using Common;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TodoListWebCore.Models;

namespace TodoListWebCore.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SiteConfiguration siteConfiguration;

        public AccountController(SiteConfiguration siteConfiguration)
        {
            this.siteConfiguration = siteConfiguration;
        }

        public async Task<IActionResult> Index()
        {
            // Get identity information from the Todo List Web API.
            var relatedApplicationIdentities = new List<IdentityInfo>();
            try
            {
                var todoListWebApiClient = await TodoListController.GetTodoListClient(this.siteConfiguration, this.User);
                var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, this.siteConfiguration.TodoListWebApiRootUrl + "api/identity");
                var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(todoListWebApiIdentityInfoRequest);
                todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
                var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
                var todoListWebApiIdentityInfo = JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);
                relatedApplicationIdentities.Add(todoListWebApiIdentityInfo);
            }
            catch (Exception exc)
            {
                relatedApplicationIdentities.Add(IdentityInfoFactory.FromException("Todo List Web API", exc));
            }

            // Gather identity information from the current application and aggregate it with the identity information from the Web API.
            var graphClient = default(AadGraphClient);
            if (StsConfiguration.StsType == StsType.AzureActiveDirectory)
            {
                graphClient = new AadGraphClient(StsConfiguration.Authority, StsConfiguration.AadTenant, this.siteConfiguration.TodoListWebCoreClientId, this.siteConfiguration.TodoListWebCoreClientSecret);
            }
            var identityInfo = await IdentityInfoFactory.FromPrincipal(this.User, "ID Token", SiteConfiguration.ApplicationName, relatedApplicationIdentities, graphClient);

            return View(new AccountIndexViewModel(identityInfo));
        }

        [HttpPost]
        public async Task<IActionResult> Index(IdentityUpdate model)
        {
            if (model != null && !string.IsNullOrWhiteSpace(model.DisplayName))
            {
                // Update identity information through the Todo List Web API.
                var todoListWebApiClient = await TodoListController.GetTodoListClient(this.siteConfiguration, this.User);
                var identityUpdateRequest = new HttpRequestMessage(HttpMethod.Post, this.siteConfiguration.TodoListWebApiRootUrl + "api/identity");
                identityUpdateRequest.Content = new JsonContent(model);
                var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(identityUpdateRequest);
                todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            }
            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public async Task<IActionResult> SignIn()
        {
            if (!User.Identity.IsAuthenticated)
            {
                // [NOTE] Send an OpenID Connect sign-in request.
                await HttpContext.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") });
                return new EmptyResult(); // The Challenge will take care of the response.
            }
            else
            {
                // Redirect to the home page after signing in.
                return RedirectToAction("Index", "Home");
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> SignOut()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("SignedOut");
            }
            else
            {
                // [NOTE] Remove the token cache for this user and send an OpenID Connect sign-out request.
                TokenCacheFactory.DeleteTokenCacheForPrincipal(this.User);
                await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                return new EmptyResult(); // The SignOut will take care of the response.
            }
        }

        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            return View();
        }
    }
}