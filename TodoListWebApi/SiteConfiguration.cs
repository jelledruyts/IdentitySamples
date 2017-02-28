using System.Configuration;

namespace TodoListWebApi
{
    public static class SiteConfiguration
    {
        public static readonly string TodoListWebApiResourceId = ConfigurationManager.AppSettings["TodoListWebApiResourceId"];
        public static readonly string TodoListWebApiClientId = ConfigurationManager.AppSettings["TodoListWebApiClientId"];
        public static readonly string TodoListWebApiClientSecret = ConfigurationManager.AppSettings["TodoListWebApiClientSecret"];

        public static readonly string TaxonomyWebApiRootUrl = ConfigurationManager.AppSettings["TaxonomyWebApiRootUrl"];
        public static readonly string TaxonomyWebApiResourceId = ConfigurationManager.AppSettings["TaxonomyWebApiResourceId"];
    }
}