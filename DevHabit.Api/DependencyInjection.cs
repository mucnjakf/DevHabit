using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using DevHabit.Api.Constants;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Jobs;
using DevHabit.Api.Middleware;
using DevHabit.Api.Options;
using DevHabit.Api.Services;
using DevHabit.Api.Services.Sorting;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Quartz;
using Refit;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace DevHabit.Api;

public static class DependencyInjection
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddApiServices()
        {
            builder.Services.AddControllers(options =>
                {
                    options.RespectBrowserAcceptHeader = true;
                    options.ReturnHttpNotAcceptable = true;
                })
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver())
                .AddXmlSerializerFormatters();

            builder.Services.Configure<MvcOptions>(options =>
            {
                NewtonsoftJsonOutputFormatter formatter = options.OutputFormatters
                    .OfType<NewtonsoftJsonOutputFormatter>()
                    .First();

                formatter.SupportedMediaTypes.Add(VendorMediaTypeNames.Application.JsonV1);
                formatter.SupportedMediaTypes.Add(VendorMediaTypeNames.Application.JsonV2);
                formatter.SupportedMediaTypes.Add(VendorMediaTypeNames.Application.HateoasJson);
                formatter.SupportedMediaTypes.Add(VendorMediaTypeNames.Application.HateoasJsonV1);
                formatter.SupportedMediaTypes.Add(VendorMediaTypeNames.Application.HateoasJsonV2);
            });

            builder.Services.AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1.0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                    options.ApiVersionSelector = new DefaultApiVersionSelector(options);

                    options.ApiVersionReader = ApiVersionReader.Combine(
                        new MediaTypeApiVersionReader(),
                        new MediaTypeApiVersionReaderBuilder()
                            .Template("application/vnd.dev-habit.hateoas.{version}+json")
                            .Build());
                })
                .AddMvc();

            builder.Services.AddOpenApi();

            builder.Services.AddResponseCaching();

            return builder;
        }

        public WebApplicationBuilder AddErrorHandling()
        {
            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
                };
            });

            builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

            return builder;
        }

        public WebApplicationBuilder AddDatabase()
        {
            builder.Services.AddDbContext<DevHabitDbContext>(options => options
                .UseNpgsql(
                    builder.Configuration.GetConnectionString("Default"),
                    npgsqlOptions =>
                        npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application))
                .UseSnakeCaseNamingConvention());

            builder.Services.AddDbContext<DevHabitIdentityDbContext>(options => options
                .UseNpgsql(
                    builder.Configuration.GetConnectionString("Default"),
                    npgsqlOptions => npgsqlOptions
                        .MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Identity))
                .UseSnakeCaseNamingConvention());

            return builder;
        }

        public WebApplicationBuilder AddObservability()
        {
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(builder.Environment.ApplicationName))
                .WithTracing(tracing => tracing
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddNpgsql())
                .WithMetrics(metrics => metrics
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation())
                .UseOtlpExporter();

            builder.Logging.AddOpenTelemetry(options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            });

            return builder;
        }

        public WebApplicationBuilder AddApplicationServices()
        {
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();

            builder.Services.AddTransient<SortMappingProvider>();

            builder.Services.AddSingleton<ISortMappingDefinition, SortMappingDefinition<HabitDto, Habit>>(_ =>
                HabitMappings.SortMapping);

            builder.Services.AddTransient<DataShapingService>();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddTransient<LinkService>();

            builder.Services.AddTransient<TokenProvider>();

            builder.Services.AddMemoryCache();

            builder.Services.AddScoped<UserContext>();

            builder.Services.AddScoped<GitHubPatService>();

            builder.Services.AddTransient<GitHubService>();
            builder.Services.AddTransient<RefitGitHubService>();

            builder.Services
                .AddHttpClient()
                .ConfigureHttpClientDefaults(x => x.AddStandardResilienceHandler());

            builder.Services
                .AddHttpClient("github")
                .ConfigureHttpClient(httpClient =>
                {
                    httpClient.BaseAddress = new Uri("https://api.github.com");

                    httpClient.DefaultRequestHeaders.UserAgent
                        .Add(new ProductInfoHeaderValue("DevHabit", "1.0"));

                    httpClient.DefaultRequestHeaders.Accept
                        .Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                });

            builder.Services.AddRefitClient<IGitHubApi>(new RefitSettings
                {
                    ContentSerializer = new NewtonsoftJsonContentSerializer()
                })
                .ConfigureHttpClient(x => x.BaseAddress = new Uri("https://api.github.com"));

            builder.Services.Configure<EncryptionOptions>(
                builder.Configuration.GetSection(EncryptionOptions.Section));

            builder.Services.AddTransient<EncryptionService>();

            builder.Services.Configure<GitHubAutomationOptions>(
                builder.Configuration.GetSection(GitHubAutomationOptions.Section));

            builder.Services.AddSingleton<InMemoryETagStore>();

            return builder;
        }

        public WebApplicationBuilder AddAuthenticationServices()
        {
            builder.Services
                .AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<DevHabitIdentityDbContext>();

            builder.Services.Configure<JwtAuthOptions>(builder.Configuration.GetSection(JwtAuthOptions.Section));

            JwtAuthOptions jwtAuthOptions = builder.Configuration
                .GetSection(JwtAuthOptions.Section)
                .Get<JwtAuthOptions>()!;

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtAuthOptions.Issuer,
                    ValidAudience = jwtAuthOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthOptions.Key)),
                };
            });

            builder.Services.AddAuthorization();

            return builder;
        }

        public WebApplicationBuilder AddCorsPolicy()
        {
            CorsOptions corsOptions = builder.Configuration.GetSection(CorsOptions.Section).Get<CorsOptions>()!;

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsOptions.PolicyName, policy =>
                {
                    policy
                        .WithOrigins(corsOptions.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            return builder;
        }

        public WebApplicationBuilder AddBackgroundJobs()
        {
            builder.Services.AddQuartz(options =>
            {
                options.AddJob<GitHubAutomationSchedulerJob>(x =>
                    x.WithIdentity("github-automation-scheduler"));

                options
                    .AddTrigger(x => x.ForJob("github-automation-scheduler")
                        .WithIdentity("github-automation-scheduler-trigger")
                        .WithSimpleSchedule(y =>
                        {
                            GitHubAutomationOptions gitHubAutomationOptions = builder.Configuration
                                .GetSection(GitHubAutomationOptions.Section)
                                .Get<GitHubAutomationOptions>()!;

                            y.WithIntervalInMinutes(gitHubAutomationOptions.ScanIntervalInMinutes).RepeatForever();
                        }));

                options.AddJob<CleanupEntryImportJobsJob>(x => x.WithIdentity("cleanup-entry-imports"));

                options.AddTrigger(x => x
                    .ForJob("cleanup-entry-imports")
                    .WithIdentity("cleanup-entry-imports-trigger")
                    .WithCronSchedule("0 0 3 * * ?", y => y.InTimeZone(TimeZoneInfo.Utc)));
            });

            builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            return builder;
        }

        public WebApplicationBuilder AddRateLimiting()
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, token) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter = $"{retryAfter.TotalSeconds}";

                        ProblemDetailsFactory problemDetailsFactory = context.HttpContext.RequestServices
                            .GetRequiredService<ProblemDetailsFactory>();

                        ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(
                            context.HttpContext,
                            StatusCodes.Status429TooManyRequests,
                            "Too Many Requests",
                            detail: $"Too many requests. Please try again after {retryAfter.TotalSeconds} seconds.");

                        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, token);
                    }
                };

                options.AddPolicy("default", httpContext =>
                {
                    string identityId = httpContext.User.GetIdentityId() ?? string.Empty;

                    if (!string.IsNullOrEmpty(identityId))
                    {
                        return RateLimitPartition.GetTokenBucketLimiter(
                            identityId,
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 100,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 5,
                                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                                TokensPerPeriod = 25
                            });
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        "anonymous",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });
            });

            return builder;
        }
    }
}
