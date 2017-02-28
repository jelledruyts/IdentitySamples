using Common;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;
using System.IdentityModel.Tokens;

namespace TaxonomyWebApi
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            // [SCENARIO] OAuth 2.0 Bearer Token Authorization 
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = SiteConfiguration.TaxonomyWebApiResourceId, // [NOTE] This ensures the token is actually intended for the current application
                NameClaimType = StsConfiguration.NameClaimType,
                RoleClaimType = StsConfiguration.RoleClaimType
            };
            if (StsConfiguration.StsType == StsType.AzureActiveDirectory)
            {
                app.UseWindowsAzureActiveDirectoryBearerAuthentication(new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    TokenValidationParameters = tokenValidationParameters,
                    Tenant = StsConfiguration.AadTenant
                });
            }
            else
            {
                app.UseActiveDirectoryFederationServicesBearerAuthentication(new ActiveDirectoryFederationServicesBearerAuthenticationOptions
                {
                    TokenValidationParameters = tokenValidationParameters,
                    MetadataEndpoint = StsConfiguration.FederationMetadataUrl
                });
            }
        }
    }
}