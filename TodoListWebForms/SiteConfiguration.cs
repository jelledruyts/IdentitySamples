using System.Configuration;

namespace TodoListWebForms
{
    public static class SiteConfiguration
    {
        public const string ApplicationName = "Todo List (ASP.NET WebForms)";

        public static readonly string TodoListWebFormsRootUrl = ConfigurationManager.AppSettings["TodoListWebFormsRootUrl"];
        public static readonly string TodoListWebFormsClientId = ConfigurationManager.AppSettings["TodoListWebFormsClientId"];
        public static readonly string TodoListWebFormsClientSecret = ConfigurationManager.AppSettings["TodoListWebFormsClientSecret"];

        public static readonly string TodoListWebApiRootUrl = ConfigurationManager.AppSettings["TodoListWebApiRootUrl"];
        public static readonly string TodoListWebApiResourceId = ConfigurationManager.AppSettings["TodoListWebApiResourceId"];
    }
}