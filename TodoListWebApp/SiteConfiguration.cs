using System.Configuration;

namespace TodoListWebApp
{
    public static class SiteConfiguration
    {
        public const string ApplicationName = "Identity Samples";
        
        public static readonly string WebAppRootUrl = ConfigurationManager.AppSettings["WebAppRootUrl"];
        public static readonly string WebAppClientId = ConfigurationManager.AppSettings["WebAppClientId"];
        public static readonly string WebAppClientSecret = ConfigurationManager.AppSettings["WebAppClientSecret"];

        public static readonly string WebApiRootUrl = ConfigurationManager.AppSettings["WebApiRootUrl"];
        public static readonly string WebApiResourceId = ConfigurationManager.AppSettings["WebApiResourceId"];
        
        public static readonly string AadTenant = ConfigurationManager.AppSettings["AadTenant"];
        public static readonly string AadAuthority = "https://login.microsoftonline.com/" + AadTenant;
    }
}