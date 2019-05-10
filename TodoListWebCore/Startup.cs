using Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Net;
using System.Threading.Tasks;

namespace TodoListWebCore
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public SiteConfiguration SiteConfiguration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();

            // Initialize site and STS configuration.
            this.SiteConfiguration = new SiteConfiguration();
            this.Configuration.GetSection("App").Bind(this.SiteConfiguration);
            StsConfiguration.Initialize(this.SiteConfiguration.StsRootUrl, this.SiteConfiguration.StsPath);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            // Add authentication services.
            services.AddAuthentication(options => options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            // Add site configuration so it can be injected into controllers.
            services.AddSingleton<SiteConfiguration>(this.SiteConfiguration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            // [NOTE] Use cookies to keep the user session active after having signed in.
            // By default, the cookie lifetime will be the same as the token lifetime.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AutomaticAuthenticate = true
            });

            // [SCENARIO] OpenID Connect
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                AutomaticChallenge = true,
                Authority = StsConfiguration.Authority,
                ClientId = this.SiteConfiguration.TodoListWebCoreClientId,
                ClientSecret = this.SiteConfiguration.TodoListWebCoreClientSecret,
                ResponseType = OpenIdConnectResponseType.CodeIdToken, // We want to retrieve an ID token as well as an authorization code (hybrid flow).
                SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme,
                SaveTokens = true, // Persists the tokens in the cookie, so they can be retrieved later with e.g. "this.HttpContext.Authentication.GetTokenAsync(...)".
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = StsConfiguration.NameClaimType,
                    RoleClaimType = StsConfiguration.RoleClaimType
                },
                Events = new OpenIdConnectEvents
                {
                    OnAuthorizationCodeReceived = async (context) =>
                    {
                        // [SCENARIO] OAuth 2.0 Authorization Code Grant, Confidential Client
                        // If there is an authorization code in the OpenID Connect response, redeem it for
                        // an access token and refresh token, and store those away in the cache.
                        // Note that typically this is a "Multiple Resource Refresh Token" which means the
                        // refresh token can be used not only against the requested "resource" but also against
                        // any other resource defined in the same directory tenant the user has access to.
                        var credential = new ClientCredential(this.SiteConfiguration.TodoListWebCoreClientId, this.SiteConfiguration.TodoListWebCoreClientSecret);
                        var userId = context.Ticket.Principal.GetUniqueIdentifier();
                        var authContext = new AuthenticationContext(StsConfiguration.Authority, StsConfiguration.CanValidateAuthority, TokenCacheFactory.GetTokenCache(userId));
                        var redirectUri = new Uri(string.Format("{0}://{1}{2}{3}", context.Request.Scheme, context.Request.Host.ToUriComponent(), context.Request.PathBase.ToUriComponent(), context.Request.Path.ToUriComponent()));
                        var result = await authContext.AcquireTokenByAuthorizationCodeAsync(context.ProtocolMessage.Code, redirectUri, credential, this.SiteConfiguration.TodoListWebApiResourceId);
                        context.HandleCodeRedemption(result.AccessToken, result.IdToken);
                        // No need to do anything with the result at this time, it is stored in the cache
                        // for later use.
                    },
                    OnRemoteFailure = (context) =>
                    {
                        // Handle sign-in errors differently than generic errors.
                        context.HandleResponse();
                        context.Response.Redirect("/Home/Error?message=" + WebUtility.UrlEncode(context.Failure.Message));
                        return Task.FromResult(0);
                    }
                }
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}