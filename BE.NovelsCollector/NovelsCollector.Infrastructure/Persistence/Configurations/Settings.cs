namespace NovelsCollector.Infrastructure.Persistence.Configurations
{
    public class Settings
    {
        public const string SettingsName = "DatabaseSettings";
        public string ConnectionString { get; set; } = String.Empty;
        public string DatabaseName { get; set; } = String.Empty;
        public string JwtKey { get; set; } = String.Empty;
    }
}
