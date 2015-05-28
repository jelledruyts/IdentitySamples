using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// <summary>
        /// Gets identity information about the currently authenticated user.
        /// </summary>
        public async Task<IdentityInfo> Get()
        {
            // Retrieve identity information from the downstream Taxonomy Web API.
            var relatedApplicationIdentities = new List<IdentityInfo>();
            try
            {
                var taxonomyWebApiClient = await CategoryController.GetTaxonomyClient();
                var taxonomyWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TaxonomyWebApiRootUrl + "api/identity");
                var taxonomyWebApiIdentityInfoResponse = await taxonomyWebApiClient.SendAsync(taxonomyWebApiIdentityInfoRequest);
                taxonomyWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
                var taxonomyWebApiIdentityInfoResponseString = await taxonomyWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
                var taxonomyWebApiIdentityInfo = JsonConvert.DeserializeObject<IdentityInfo>(taxonomyWebApiIdentityInfoResponseString);
                relatedApplicationIdentities.Add(taxonomyWebApiIdentityInfo);
            }
            catch (AdalException exc)
            {
                // Ignore exceptions when attempting to retrieve identity information from down-stream applications.
                // This will fail e.g. for a daemon client which cannot perform an On-Behalf-Of token request because
                // it is using the application's service principal identity, not an actual directory user, which
                // results in the following error:
                // AADSTS50034: To sign into this application the account must be added to the <tenant>.onmicrosoft.com directory
                Trace.WriteLine("Failed to retrieve related application identity information: " + exc.ToString());
            }

            // Aggregate the current identity information with the downstream identities.
            var graphClient = new AadGraphClient(SiteConfiguration.AadTenant, SiteConfiguration.TodoListWebApiClientId, SiteConfiguration.TodoListWebApiClientSecret);
            return await IdentityInfo.FromCurrent("Todo List Web API", relatedApplicationIdentities, graphClient);
        }

        /// <summary>
        /// Updates information about a user in Azure Active Directory.
        /// </summary>
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