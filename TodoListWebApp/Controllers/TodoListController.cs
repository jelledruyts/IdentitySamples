using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using TodoListWebApp.Infrastructure;
using TodoListWebApp.Models;

namespace TodoListWebApp.Controllers
{
    [Authorize]
    public class TodoListController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var client = await GetTodoListClient();
            var request = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.WebApiRootUrl + "api/todolist");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            var todoList = JsonConvert.DeserializeObject<List<TodoItem>>(responseString);
            return View(todoList);
        }

        [HttpPost]
        public async Task<ActionResult> Index(TodoItem model)
        {
            var client = await GetTodoListClient();
            var request = new HttpRequestMessage(HttpMethod.Post, SiteConfiguration.WebApiRootUrl + "api/todolist");
            request.Content = new JsonContent(model);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return RedirectToAction("Index");
        }

        private async Task<HttpClient> GetTodoListClient()
        {
            // Get a token to authenticate against the Web API.
            var authContext = new AuthenticationContext(SiteConfiguration.AadAuthority, TokenCacheFactory.Instance);
            var credential = new ClientCredential(SiteConfiguration.WebAppClientId, SiteConfiguration.WebAppClientSecret);
            var userIdentifier = new UserIdentifier(ClaimsPrincipal.Current.GetUniqueIdentifier(), UserIdentifierType.UniqueId);
            // We can acquire the token silently here because we have redeemed the OpenID Connect authorization code at signin
            // for an access token and stored it in the token cache.
            var result = await authContext.AcquireTokenSilentAsync(SiteConfiguration.WebApiResourceId, credential, userIdentifier);

            // Retrieve the user's To Do List.
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }
    }
}