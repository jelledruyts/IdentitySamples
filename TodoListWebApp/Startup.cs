using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(TodoListWebApp.Startup))]

namespace TodoListWebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}