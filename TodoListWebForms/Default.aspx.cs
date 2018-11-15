using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web.UI;

namespace TodoListWebForms
{
    public partial class _Default : Page
    {
        protected IdentityInfo identity;

        protected async void Page_Load(object sender, EventArgs e)
        {
            // Get identity information from the Todo List Web API.
            var relatedApplicationIdentities = new List<IdentityInfo>();
            try
            {
                var todoListWebApiClient = await GetTodoListClient(this.User);
                var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TodoListWebApiRootUrl + "api/identity");
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
                graphClient = new AadGraphClient(StsConfiguration.Authority, StsConfiguration.AadTenant, SiteConfiguration.TodoListWebFormsClientId, SiteConfiguration.TodoListWebFormsClientSecret);
            }
            this.identity = await IdentityInfoFactory.FromPrincipal(this.User, "ID Token", SiteConfiguration.ApplicationName, relatedApplicationIdentities, graphClient);
        }

        private static async Task<HttpClient> GetTodoListClient(IPrincipal user)
        {
            // [SCENARIO] OAuth 2.0 Authorization Code Grant, Confidential Client
            // Get a token to authenticate against the Web API.
            var authContext = new AuthenticationContext(StsConfiguration.Authority, StsConfiguration.CanValidateAuthority, TokenCacheFactory.GetTokenCacheForPrincipal(user));
            var credential = new ClientCredential(SiteConfiguration.TodoListWebFormsClientId, SiteConfiguration.TodoListWebFormsClientSecret);
            var userIdentifier = new UserIdentifier(user.GetUniqueIdentifier(), UserIdentifierType.UniqueId);

            // We can acquire the token silently here because we have redeemed the OpenID Connect authorization code at signin
            // for an access token and stored it in the token cache.
            var result = await authContext.AcquireTokenSilentAsync(SiteConfiguration.TodoListWebApiResourceId, credential, userIdentifier);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }
    }
}