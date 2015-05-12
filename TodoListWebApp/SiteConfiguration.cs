using Common;
using System.Configuration;

namespace TodoListWebApp
{
    public static class SiteConfiguration
    {
        public const string ApplicationName = "Todo List (ASP.NET MVC)";

        public static readonly string TodoListWebAppRootUrl = ConfigurationManager.AppSettings["TodoListWebAppRootUrl"];
        public static readonly string TodoListWebAppResourceId = ConfigurationManager.AppSettings["TodoListWebAppResourceId"];
        public static readonly string TodoListWebAppClientId = ConfigurationManager.AppSettings["TodoListWebAppClientId"];
        public static readonly string TodoListWebAppClientSecret = ConfigurationManager.AppSettings["TodoListWebAppClientSecret"];

        public static readonly string TodoListWebApiRootUrl = ConfigurationManager.AppSettings["TodoListWebApiRootUrl"];
        public static readonly string TodoListWebApiResourceId = ConfigurationManager.AppSettings["TodoListWebApiResourceId"];
        
        public static readonly string AadTenant = ConfigurationManager.AppSettings["AadTenant"];
        public static readonly string AadAuthority = Constants.AadEndpoint + AadTenant;
    }
}