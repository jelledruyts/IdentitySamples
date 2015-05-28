using Common;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TodoListWebApp.Models;

namespace TodoListWebApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public async Task<ActionResult> Index()
        {
            // Get identity information from the Todo List Web API.
            var todoListWebApiClient = await TodoListController.GetTodoListClient();
            var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TodoListWebApiRootUrl + "api/identity");
            var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(todoListWebApiIdentityInfoRequest);
            todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
            var todoListWebApiIdentityInfo = JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);

            // Gather identity information from the current application and aggregate it with the identity information from the Web API.
            var graphClient = new AadGraphClient(SiteConfiguration.AadTenant, SiteConfiguration.TodoListWebAppClientId, SiteConfiguration.TodoListWebAppClientSecret);
            var identityInfo = await IdentityInfo.FromCurrent(SiteConfiguration.ApplicationName, new IdentityInfo[] { todoListWebApiIdentityInfo }, graphClient);

            return View(new AccountIndexViewModel(identityInfo));
        }

        [HttpPost]
        public async Task<ActionResult> Index(IdentityUpdate model)
        {
            if (model != null && !string.IsNullOrWhiteSpace(model.DisplayName))
            {
                // Update identity information through the Todo List Web API.
                var todoListWebApiClient = await TodoListController.GetTodoListClient();
                var identityUpdateRequest = new HttpRequestMessage(HttpMethod.Post, SiteConfiguration.TodoListWebApiRootUrl + "api/identity");
                identityUpdateRequest.Content = new JsonContent(model);
                var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(identityUpdateRequest);
                todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            }
            return RedirectToAction("Index");
        }

        public ActionResult SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                // [NOTE] Send an OpenID Connect sign-in request.
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
                return new EmptyResult(); // The challenge will take care of the response.
            }
            else
            {
                // Redirect to the home page after signing in.
                return RedirectToAction("Index", "Home");
            }
        }

        public void SignOut()
        {
            // [NOTE] Remove the token cache for this user and send an OpenID Connect sign-out request.
            TokenCacheFactory.DeleteTokenCacheForCurrentPrincipal();
            HttpContext.GetOwinContext().Authentication.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
        }

        [AllowAnonymous]
        public ActionResult SignedOut()
        {
            return View();
        }
    }
}