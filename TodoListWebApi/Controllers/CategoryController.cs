using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using TodoListWebApi.Infrastructure;
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
            var authContext = new AuthenticationContext(SiteConfiguration.AadAuthority, TokenCacheFactory.Instance);
            var credential = new ClientCredential(SiteConfiguration.TodoListWebApiClientId, SiteConfiguration.TodoListWebApiClientSecret);
            var userIdentity = (ClaimsIdentity)ClaimsPrincipal.Current.Identity;
            var bootstrapContext = userIdentity.BootstrapContext as System.IdentityModel.Tokens.BootstrapContext;
            // When using a UserAssertion containing only the token and not the user name, the token cache lookup
            // will only match against the requested resource, so a token from another user could be returned!
            // In this case, the user's "Name" is also provided which will make the token cache lookup match on the "DisplayableId"
            // property; this won't match the provided "Name" either (so it will not reuse a cached token) but at least it won't
            // use another user's token.
            var userAssertion = new UserAssertion(bootstrapContext.Token, userIdentity.AuthenticationType, userIdentity.Name);
            var result = await authContext.AcquireTokenAsync(SiteConfiguration.TaxonomyWebApiResourceId, credential, userAssertion);

            // Retrieve the user's To Do List.
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }
    }
}