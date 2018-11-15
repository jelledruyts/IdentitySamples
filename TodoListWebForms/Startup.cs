using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Owin;
using System.Configuration;

[assembly: OwinStartup(typeof(TodoListWebForms.Startup))]

namespace TodoListWebForms
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}