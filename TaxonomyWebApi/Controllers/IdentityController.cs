using Common;
using System.Threading.Tasks;
using System.Web.Http;

namespace TaxonomyWebApi.Controllers
{
    [Authorize]
    public class IdentityController : ApiController
    {
        public async Task<IdentityInfo> Get()
        {
            var graphClient = new AadGraphClient(SiteConfiguration.AadTenant, SiteConfiguration.TaxonomyWebApiClientId, SiteConfiguration.TaxonomyWebApiClientSecret);
            return await IdentityInfo.FromCurrent("Taxonomy Web API", null, graphClient);
        }
    }
}