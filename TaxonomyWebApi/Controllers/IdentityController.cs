using Common;
using System.Web.Http;

namespace TaxonomyWebApi.Controllers
{
    [Authorize]
    public class IdentityController : ApiController
    {
        public IdentityInfo Get()
        {
            return IdentityInfo.FromCurrent("Taxonomy Web API", null);
        }
    }
}