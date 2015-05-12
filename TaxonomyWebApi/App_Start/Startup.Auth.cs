using Microsoft.Owin.Security.ActiveDirectory;
using Owin;
using System.IdentityModel.Tokens;

namespace TaxonomyWebApi
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = SiteConfiguration.TaxonomyWebApiResourceId,
                        NameClaimType = "name", // [NOTE] This indicates that the user's display name is defined in the "name" claim
                        RoleClaimType = "roles" // [NOTE] This indicates that the user's roles are defined in the "roles" claim
                    },
                    Tenant = SiteConfiguration.AadTenant
                });
        }
    }
}