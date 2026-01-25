namespace DevHabit.FunctionalTests.Infrastructure;

public static class Routes
{
    public static class Auth
    {
        public const string Register = "auth/register";
        public const string Login = "auth/login";
    }

    public static class Habits
    {
        public const string Create = "habits";
        public static string GetById(string id) => $"habits/{id}";
    }

    public static class GitHub
    {
        public const string StoreAccessToken = "github/personal-access-token";
        public const string GetProfile = "github/profile";
        public const string GetEvents = "github/events";
    }

    public static class Tags
    {
        public const string Create = "tags";
    }

    public static class HabitTags
    {
        public static string UpsertTags(string habitId) => $"habits/{habitId}/tags";
    }
}
