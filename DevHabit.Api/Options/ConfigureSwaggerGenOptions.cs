using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DevHabit.Api.Options;

public sealed class ConfigureSwaggerGenOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
    : IConfigureNamedOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (ApiVersionDescription apiVersionDescription in
                 apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            var openApiInfo = new OpenApiInfo
            {
                Title = $"DevHabit.Api {apiVersionDescription.ApiVersion}",
                Version = apiVersionDescription.ApiVersion.ToString()
            };

            options.SwaggerDoc(apiVersionDescription.GroupName, openApiInfo);
        }

        options.ResolveConflictingActions(descriptions => descriptions.First());

        string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        options.IncludeXmlComments(xmlPath);

        options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        options.DescribeAllParametersInCamelCase();

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description =
                "JWT authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = JwtBearerDefaults.AuthenticationScheme
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme,
                    }
                },
                []
            }
        });
    }

    public void Configure(string? name, SwaggerGenOptions options)
    {
        Configure(options);
    }
}
