using System.Configuration;

namespace TodoListWebApp
{
    public static class Config
    {
        public const string ApplicationName = "Identity Samples";
        public static readonly string WebAppClientId = ConfigurationManager.AppSettings["WebAppClientId"];
        public static readonly string AadTenant = ConfigurationManager.AppSettings["AadTenant"];
        public static readonly string AadAuthority = "https://login.microsoftonline.com/" + AadTenant;
        public static readonly string RootUrl = ConfigurationManager.AppSettings["RootUrl"];
    }
}