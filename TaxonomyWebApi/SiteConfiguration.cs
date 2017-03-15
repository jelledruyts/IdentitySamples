namespace TaxonomyWebApi
{
    public class SiteConfiguration
    {
        public string TaxonomyWebApiResourceId { get; set; }
        public string TaxonomyWebApiClientId { get; set; }
        public string TaxonomyWebApiClientSecret { get; set; }

        public string StsRootUrl { get; set; }
        public string StsPath { get; set; }
        public string StsAccessTokenIssuer { get; set; }
    }
}