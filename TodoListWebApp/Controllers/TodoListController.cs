using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using TodoListWebApp.Models;

namespace TodoListWebApp.Controllers
{
    [Authorize]
    public class TodoListController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var client = await GetTodoListClient();

            // Get the todo list.
            var todoListRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TodoListWebApiRootUrl + "api/todolist");
            var todoListResponse = await client.SendAsync(todoListRequest);
            todoListResponse.EnsureSuccessStatusCode();
            var todoListResponseString = await todoListResponse.Content.ReadAsStringAsync();
            var todoList = JsonConvert.DeserializeObject<List<TodoItem>>(todoListResponseString);

            // Get the categories.
            var categoriesRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TodoListWebApiRootUrl + "api/category");
            var categoriesResponse = await client.SendAsync(categoriesRequest);
            categoriesResponse.EnsureSuccessStatusCode();
            var categoriesResponseString = await categoriesResponse.Content.ReadAsStringAsync();
            var categories = JsonConvert.DeserializeObject<List<Category>>(categoriesResponseString);

            return View(new TodoListIndexViewModel(todoList, categories));
        }

        [HttpPost]
        public async Task<ActionResult> Index(TodoItemCreate model)
        {
            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                var client = await GetTodoListClient();
                var newTodoItemRequest = new HttpRequestMessage(HttpMethod.Post, SiteConfiguration.TodoListWebApiRootUrl + "api/todolist");
                newTodoItemRequest.Content = new JsonContent(model);
                var newTodoItemResponse = await client.SendAsync(newTodoItemRequest);
                newTodoItemResponse.EnsureSuccessStatusCode();
            }
            return RedirectToAction("Index");
        }

        public static async Task<HttpClient> GetTodoListClient()
        {
            // [SCENARIO] OAuth 2.0 Authorization Code Grant, Confidential Client
            // Get a token to authenticate against the Web API.
            var authContext = new AuthenticationContext(SiteConfiguration.AadAuthority, TokenCacheFactory.GetTokenCacheForCurrentPrincipal());
            var credential = new ClientCredential(SiteConfiguration.TodoListWebAppClientId, SiteConfiguration.TodoListWebAppClientSecret);
            var userIdentifier = new UserIdentifier(ClaimsPrincipal.Current.GetUniqueIdentifier(), UserIdentifierType.UniqueId);

            // We can acquire the token silently here because we have redeemed the OpenID Connect authorization code at signin
            // for an access token and stored it in the token cache.
            var result = await authContext.AcquireTokenSilentAsync(SiteConfiguration.TodoListWebApiResourceId, credential, userIdentifier);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }
    }
}