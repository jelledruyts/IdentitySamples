using Common;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using TodoListWebApi.Models;

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

            var graphClient = new AadGraphClient(SiteConfiguration.AadTenant, SiteConfiguration.TodoListWebApiClientId, SiteConfiguration.TodoListWebApiClientSecret);
            return await IdentityInfo.FromCurrent("Todo List Web API", new IdentityInfo[] { taxonomyWebApiIdentityInfo }, graphClient);
        }

        public async Task Post(IdentityUpdate identity)
        {
            if (identity != null && !string.IsNullOrWhiteSpace(identity.DisplayName))
            {
                var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
                var graphClient = new AadGraphClient(SiteConfiguration.AadTenant, SiteConfiguration.TodoListWebApiClientId, SiteConfiguration.TodoListWebApiClientSecret);
                await graphClient.UpdateUserAsync(userId, identity.DisplayName);
            }
        }
    }
}