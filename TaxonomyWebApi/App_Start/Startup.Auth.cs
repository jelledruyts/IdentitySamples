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
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = SiteConfiguration.TaxonomyWebApiResourceId, // [NOTE] This ensures the token is actually intended for the current application
                        NameClaimType = "name", // [NOTE] This indicates that the user's display name is defined in the "name" claim
                        RoleClaimType = "roles" // [NOTE] This indicates that the user's roles are defined in the "roles" claim
                    },
                    Tenant = SiteConfiguration.AadTenant
                });
        }
    }
}