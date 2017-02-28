using Common;
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
            // [NOTE] Allow Cross-Origin Resource Sharing (CORS) from a JavaScript client (SPA web application).
            app.UseCors(CorsOptions.AllowAll);

            // [SCENARIO] OAuth 2.0 Bearer Token Authorization 
            // Use bearer authentication with tokens coming from Azure Active Directory / AD FS.
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidAudience = SiteConfiguration.TodoListWebApiResourceId, // [NOTE] This ensures the token is actually intended for the current application
                SaveSigninToken = true, // [NOTE] This places the original token on the ClaimsIdentity.BootstrapContext
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