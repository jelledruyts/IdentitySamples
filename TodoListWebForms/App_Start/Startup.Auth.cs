using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Threading.Tasks;
using System.Web;

namespace TodoListWebForms
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            // [NOTE] Use cookies to keep the user session active after having signed in.
            // By default, the cookie lifetime will be the same as the token lifetime.
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // [SCENARIO] OpenID Connect
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = SiteConfiguration.TodoListWebFormsClientId,
                    Authority = StsConfiguration.Authority,
                    PostLogoutRedirectUri = new Uri(new Uri(SiteConfiguration.TodoListWebFormsRootUrl), "SignedOut.aspx").ToString(),
                    RedirectUri = SiteConfiguration.TodoListWebFormsRootUrl,

                    TokenValidationParameters = new TokenValidationParameters
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
                            var credential = new ClientCredential(SiteConfiguration.TodoListWebFormsClientId, SiteConfiguration.TodoListWebFormsClientSecret);
                            var userId = context.AuthenticationTicket.Identity.GetUniqueIdentifier();
                            var authContext = new AuthenticationContext(StsConfiguration.Authority, StsConfiguration.CanValidateAuthority, TokenCacheFactory.GetTokenCache(userId));
                            var result = await authContext.AcquireTokenByAuthorizationCodeAsync(context.Code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, SiteConfiguration.TodoListWebApiResourceId);
                            // No need to do anything with the result at this time, it is stored in the cache
                            // for later use.
                        },
                        AuthenticationFailed = context =>
                        {
                            context.HandleResponse();
                            context.Response.Redirect("Error.aspx?message=" + HttpUtility.UrlEncode(context.Exception.Message));
                            return Task.FromResult(0);
                        }
                    }
                });

            // [NOTE] This makes any middleware defined above this line run before the Authorization rule is applied in web.config
            app.UseStageMarker(PipelineStage.Authenticate);
        }
    }
}