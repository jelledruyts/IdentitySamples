using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Web;
using System.Web.Mvc;

namespace TodoListWebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = Config.WebAppClientId,
                    Authority = Config.AadAuthority,
                    PostLogoutRedirectUri = new Uri(new Uri(Config.RootUrl), urlHelper.Action("SignedOut", "Account")).ToString(),
                });
        }
    }
}