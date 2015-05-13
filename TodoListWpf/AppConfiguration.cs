using Common;
using System.Configuration;

namespace TodoListWpf
{
    public static class AppConfiguration
    {
        public const string ApplicationName = "Todo List (WPF)";

        public static readonly string TodoListWpfRedirectUrl = ConfigurationManager.AppSettings["TodoListWpfRedirectUrl"];
        public static readonly string TodoListWpfClientId = ConfigurationManager.AppSettings["TodoListWpfClientId"];
        public static readonly string TodoListWebApiRootUrl = ConfigurationManager.AppSettings["TodoListWebApiRootUrl"];
        public static readonly string TodoListWebApiResourceId = ConfigurationManager.AppSettings["TodoListWebApiResourceId"];

        public static readonly string AadTenant = ConfigurationManager.AppSettings["AadTenant"];
        public static readonly string AadAuthority = Constants.AadEndpoint + AadTenant;
    }
}