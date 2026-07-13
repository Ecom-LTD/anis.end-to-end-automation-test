namespace Automation.Framework.Configuration
{
    public static class UserHelper
    {
        public static UserCredentials GetUser(string key)
            => ConfigurationManager.Settings.Users.FirstOrDefault(u => u.Key == key)
               ?? throw new Exception($"User '{key}' not found in appsettings.json");

        public static List<UserCredentials> GetUsersByRole(string role)
            => ConfigurationManager.Settings.Users.Where(u => u.Role == role).ToList();

        public static List<UserCredentials> GetAllUsers() => ConfigurationManager.Settings.Users;

        public static bool UserExists(string key)
            => ConfigurationManager.Settings.Users.Any(u => u.Key == key);
    }
}
