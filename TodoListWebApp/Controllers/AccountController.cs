using Common;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            var relatedApplicationIdentities = new List<IdentityInfo>();
            try
            {
                var todoListWebApiClient = await TodoListController.GetTodoListClient(this.User);
                var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TodoListWebApiRootUrl + "api/identity");
                var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(todoListWebApiIdentityInfoRequest);
                todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
                var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
                var todoListWebApiIdentityInfo = JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);
                relatedApplicationIdentities.Add(todoListWebApiIdentityInfo);
            }
            catch (Exception exc)
            {
                relatedApplicationIdentities.Add(IdentityInfo.FromException("Todo List Web API", exc));
            }

            // Gather identity information from the current application and aggregate it with the identity information from the Web API.
            var graphClient = default(AadGraphClient);
            if (StsConfiguration.StsType == StsType.AzureActiveDirectory)
            {
                graphClient = new AadGraphClient(StsConfiguration.Authority, StsConfiguration.AadTenant, SiteConfiguration.TodoListWebAppClientId, SiteConfiguration.TodoListWebAppClientSecret);
            }
            var identityInfo = await IdentityInfo.FromPrincipal(this.User, SiteConfiguration.ApplicationName, relatedApplicationIdentities, graphClient);

            return View(new AccountIndexViewModel(identityInfo));
        }

        [HttpPost]
        public async Task<ActionResult> Index(IdentityUpdate model)
        {
            if (model != null && !string.IsNullOrWhiteSpace(model.DisplayName))
            {
                // Update identity information through the Todo List Web API.
                var todoListWebApiClient = await TodoListController.GetTodoListClient(this.User);
                var identityUpdateRequest = new HttpRequestMessage(HttpMethod.Post, SiteConfiguration.TodoListWebApiRootUrl + "api/identity");
                identityUpdateRequest.Content = new JsonContent(model);
                var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(identityUpdateRequest);
                todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            }
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public ActionResult SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                // [NOTE] Send an OpenID Connect sign-in request.
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
                return new EmptyResult(); // The Challenge will take care of the response.
            }
            else
            {
                // Redirect to the home page after signing in.
                return RedirectToAction("Index", "Home");
            }
        }

        [AllowAnonymous]
        public ActionResult SignOut()
        {
            if (!Request.IsAuthenticated)
            {
                return RedirectToAction("SignedOut");
            }
            else
            {
                // [NOTE] Remove the token cache for this user and send an OpenID Connect sign-out request.
                TokenCacheFactory.DeleteTokenCacheForPrincipal(this.User);
                HttpContext.GetOwinContext().Authentication.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
                return new EmptyResult(); // The SignOut will take care of the response.
            }
        }

        [AllowAnonymous]
        public ActionResult SignedOut()
        {
            return View();
        }
    }
}