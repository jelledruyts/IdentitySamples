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
                        ValidAudience = SiteConfiguration.TaxonomyWebApiResourceId
                    },
                    Tenant = SiteConfiguration.AadTenant
                });
        }
    }
}