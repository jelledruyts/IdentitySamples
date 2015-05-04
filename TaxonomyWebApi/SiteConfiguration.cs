using System.Configuration;

namespace TaxonomyWebApi
{
    public static class SiteConfiguration
    {
        public static readonly string TaxonomyWebApiResourceId = ConfigurationManager.AppSettings["TaxonomyWebApiResourceId"];
        public static readonly string AadTenant = ConfigurationManager.AppSettings["AadTenant"];
    }
}