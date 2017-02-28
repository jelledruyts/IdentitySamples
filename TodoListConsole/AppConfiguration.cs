using System.Configuration;

namespace TodoListConsole
{
    public static class AppConfiguration
    {
        public const string ApplicationName = "Todo List (Console)";

        public static readonly string TodoListConsoleRedirectUrl = ConfigurationManager.AppSettings["TodoListConsoleRedirectUrl"];
        public static readonly string TodoListConsoleClientId = ConfigurationManager.AppSettings["TodoListConsoleClientId"];
        public static readonly string TodoListWebApiRootUrl = ConfigurationManager.AppSettings["TodoListWebApiRootUrl"];
        public static readonly string TodoListWebApiResourceId = ConfigurationManager.AppSettings["TodoListWebApiResourceId"];
    }
}