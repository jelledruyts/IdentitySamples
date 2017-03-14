namespace TodoListWebCore
{
    public class SiteConfiguration
    {
        public const string ApplicationName = "Todo List (ASP.NET Core)";

        public string TodoListWebCoreRootUrl { get; set; }
        public string TodoListWebCoreClientId { get; set; }
        public string TodoListWebCoreClientSecret { get; set; }

        public string TodoListWebApiRootUrl { get; set; }
        public string TodoListWebApiResourceId { get; set; }

        public string StsRootUrl { get; set; }
        public string StsPath { get; set; }
    }
}