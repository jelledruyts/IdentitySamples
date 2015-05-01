using Microsoft.Owin.Security.ActiveDirectory;
using Owin;
using System.Configuration;
using System.IdentityModel.Tokens;

namespace TodoListWebApi
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    TokenValidationParameters = new TokenValidationParameters { ValidAudience = ConfigurationManager.AppSettings["WebApiResourceId"] },
                    Tenant = ConfigurationManager.AppSettings["AadTenant"]
                });
        }
    }
}