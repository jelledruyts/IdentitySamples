using Common;
using System.Threading.Tasks;
using System.Web.Http;

namespace TaxonomyWebApi.Controllers
{
    [Authorize]
    public class IdentityController : ApiController
    {
        /// <summary>
        /// Gets identity information about the currently authenticated user.
        /// </summary>
        public async Task<IdentityInfo> Get()
        {
            var graphClient = default(AadGraphClient);
            if (StsConfiguration.StsType == StsType.AzureActiveDirectory)
            {
                graphClient = new AadGraphClient(StsConfiguration.Authority, StsConfiguration.AadTenant, SiteConfiguration.TaxonomyWebApiClientId, SiteConfiguration.TaxonomyWebApiClientSecret);
            }
            return await IdentityInfo.FromCurrent("Taxonomy Web API", null, graphClient);
        }
    }
}