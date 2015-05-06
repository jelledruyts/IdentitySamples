using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.ActiveDirectory;
using Owin;
using System.IdentityModel.Tokens;

namespace TodoListWebApi
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            // Allow Cross-Origin Resource Sharing (CORS) from a Javascript client (SPA web application).
            app.UseCors(CorsOptions.AllowAll);

            // Use bearer authentication with tokens coming from Azure Active Directory.
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidAudience = SiteConfiguration.TodoListWebApiResourceId,
                        SaveSigninToken = true // This places the original token on the ClaimsIdentity.BootstrapContext.
                    },
                    Tenant = SiteConfiguration.AadTenant
                });
        }
    }
}