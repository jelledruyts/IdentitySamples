using Common;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace TodoListWebApi.Controllers
{
    [Authorize]
    public class IdentityController : ApiController
    {
        public async Task<IdentityInfo> Get()
        {
            var taxonomyWebApiClient = await CategoryController.GetTaxonomyClient();
            var taxonomyWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TaxonomyWebApiRootUrl + "api/identity");
            var taxonomyWebApiIdentityInfoResponse = await taxonomyWebApiClient.SendAsync(taxonomyWebApiIdentityInfoRequest);
            taxonomyWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            var taxonomyWebApiIdentityInfoResponseString = await taxonomyWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
            var taxonomyWebApiIdentityInfo = JsonConvert.DeserializeObject<IdentityInfo>(taxonomyWebApiIdentityInfoResponseString);

            return IdentityInfo.FromCurrent("Todo List Web API", new IdentityInfo[] { taxonomyWebApiIdentityInfo });
        }
    }
}