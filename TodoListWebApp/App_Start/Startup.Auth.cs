using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace TodoListWebApp
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            var urlHelper = new UrlHelper(HttpContext.Current.Request.RequestContext);
            var postLogoutRedirectUri = new Uri(new Uri(SiteConfiguration.TodoListWebAppRootUrl), urlHelper.Action("SignedOut", "Account")).ToString();

            // [NOTE] Use cookies to keep the user session active after having signed in.
            // By default, the cookie lifetime will be the same as the token lifetime.
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // [SCENARIO] OpenID Connect
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = SiteConfiguration.TodoListWebAppClientId,
                    Authority = StsConfiguration.Authority,
                    PostLogoutRedirectUri = postLogoutRedirectUri,
                    RedirectUri = SiteConfiguration.TodoListWebAppRootUrl,

                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        NameClaimType = StsConfiguration.NameClaimType,
                        RoleClaimType = StsConfiguration.RoleClaimType
                    },

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        AuthorizationCodeReceived = async (context) =>
                        {
                            // [SCENARIO] OAuth 2.0 Authorization Code Grant, Confidential Client
                            // If there is an authorization code in the OpenID Connect response, redeem it for
                            // an access token and refresh token, and store those away in the cache.
                            // Note that typically this is a "Multiple Resource Refresh Token" which means the
                            // refresh token can be used not only against the requested "resource" but also against
                            // any other resource defined in the same directory tenant the user has access to.
                            var credential = new ClientCredential(SiteConfiguration.TodoListWebAppClientId, SiteConfiguration.TodoListWebAppClientSecret);
                            var userId = context.AuthenticationTicket.Identity.GetUniqueIdentifier();
                            var authContext = new AuthenticationContext(StsConfiguration.Authority, StsConfiguration.CanValidateAuthority, TokenCacheFactory.GetTokenCache(userId));
                            var result = await authContext.AcquireTokenByAuthorizationCodeAsync(context.Code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, SiteConfiguration.TodoListWebApiResourceId);
                            // No need to do anything with the result at this time, it is stored in the cache
                            // for later use.
                        },
                        AuthenticationFailed = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect(urlHelper.Action("Error", "Home", new { message = context.Exception.Message }));
                            return Task.FromResult(0);
                        }
                    }
                });
        }
    }
}