using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace TaxonomyWebApi.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class IdentityController : Controller
    {
        private readonly SiteConfiguration siteConfiguration;

        public IdentityController(SiteConfiguration siteConfiguration)
        {
            this.siteConfiguration = siteConfiguration;
        }

        /// <summary>
        /// Gets identity information about the currently authenticated user.
        /// </summary>
        [HttpGet]
        public async Task<IdentityInfo> Get()
        {
            var graphClient = default(AadGraphClient);
            if (StsConfiguration.StsType == StsType.AzureActiveDirectory)
            {
                graphClient = new AadGraphClient(StsConfiguration.Authority, StsConfiguration.AadTenant, this.siteConfiguration.TaxonomyWebApiClientId, this.siteConfiguration.TaxonomyWebApiClientSecret);
            }
            return await IdentityInfoFactory.FromPrincipal(this.User, "Access Token", "Taxonomy Web API", null, graphClient);
        }
    }
}