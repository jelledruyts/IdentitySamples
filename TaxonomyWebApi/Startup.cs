using Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;

namespace TaxonomyWebApi
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
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    // Output JSON data with camel case property names to follow JavaScript conventions.
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                });

            // Add authentication services.
            services.AddAuthentication();

            // Add site configuration so it can be injected into controllers.
            services.AddSingleton<SiteConfiguration>(this.SiteConfiguration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            // Require authentication using OAuth 2.0 bearer tokens.
            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                Authority = StsConfiguration.Authority,
                Audience = this.SiteConfiguration.TaxonomyWebApiResourceId,
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = StsConfiguration.NameClaimType,
                    RoleClaimType = StsConfiguration.RoleClaimType,
                },
                SaveToken = true // This makes the JWT token available through "this.HttpContext.Authentication.GetTokenAsync(...)".
            });

            app.UseMvc();
        }
    }
}