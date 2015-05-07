using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using TodoListWebApi.Models;

namespace TodoListWebApi.Controllers
{
    [Authorize]
    public class CategoryController : ApiController
    {
        public async Task<IEnumerable<Category>> Get()
        {
            var client = await GetTaxonomyClient();
            var categoryListRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TaxonomyWebApiRootUrl + "api/category");
            var categoryListResponse = await client.SendAsync(categoryListRequest);
            categoryListResponse.EnsureSuccessStatusCode();
            var categoryListResponseString = await categoryListResponse.Content.ReadAsStringAsync();
            var categoryList = JsonConvert.DeserializeObject<List<Category>>(categoryListResponseString);
            return categoryList;
        }

        public async Task<IHttpActionResult> Post(Category value)
        {
            var client = await GetTaxonomyClient();
            var newCategoryRequest = new HttpRequestMessage(HttpMethod.Post, SiteConfiguration.TaxonomyWebApiRootUrl + "api/category");
            newCategoryRequest.Content = new JsonContent(value);
            var newCategoryResponse = await client.SendAsync(newCategoryRequest);
            newCategoryResponse.EnsureSuccessStatusCode();
            var newCategoryResponseString = await newCategoryResponse.Content.ReadAsStringAsync();
            var newCategory = JsonConvert.DeserializeObject<Category>(newCategoryResponseString);
            return Ok(newCategory);
        }

        public static async Task<HttpClient> GetTaxonomyClient()
        {
            // Get an On-Behalf-Of token to authenticate against the Taxonomy Web API.
            var authContext = new AuthenticationContext(SiteConfiguration.AadAuthority, TokenCacheFactory.GetTokenCacheForCurrentPrincipal());
            var credential = new ClientCredential(SiteConfiguration.TodoListWebApiClientId, SiteConfiguration.TodoListWebApiClientSecret);
            var userIdentity = (ClaimsIdentity)ClaimsPrincipal.Current.Identity;
            var bootstrapContext = userIdentity.BootstrapContext as System.IdentityModel.Tokens.BootstrapContext;
            var userAssertion = new UserAssertion(bootstrapContext.Token);
            var result = await authContext.AcquireTokenAsync(SiteConfiguration.TaxonomyWebApiResourceId, credential, userAssertion);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }
    }
}