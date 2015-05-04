using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(TaxonomyWebApi.Startup))]

namespace TaxonomyWebApi
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}