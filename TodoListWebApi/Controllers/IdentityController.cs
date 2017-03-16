using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
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
                var taxonomyWebApiClient = await CategoryController.GetTaxonomyClient(this.User);
                var taxonomyWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, SiteConfiguration.TaxonomyWebApiRootUrl + "api/identity");
                var taxonomyWebApiIdentityInfoResponse = await taxonomyWebApiClient.SendAsync(taxonomyWebApiIdentityInfoRequest);
                taxonomyWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
                var taxonomyWebApiIdentityInfoResponseString = await taxonomyWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
                var taxonomyWebApiIdentityInfo = JsonConvert.DeserializeObject<IdentityInfo>(taxonomyWebApiIdentityInfoResponseString);
                relatedApplicationIdentities.Add(taxonomyWebApiIdentityInfo);
            }
            catch (Exception exc)
            {
                relatedApplicationIdentities.Add(IdentityInfoFactory.FromException("Taxonomy Web API", exc));
            }

            // Aggregate the current identity information with the downstream identities.
            var graphClient = default(AadGraphClient);
            if (StsConfiguration.StsType == StsType.AzureActiveDirectory)
            {
                graphClient = new AadGraphClient(StsConfiguration.Authority, StsConfiguration.AadTenant, SiteConfiguration.TodoListWebApiClientId, SiteConfiguration.TodoListWebApiClientSecret);
            }
            return await IdentityInfoFactory.FromPrincipal(this.User, "Todo List Web API", relatedApplicationIdentities, graphClient);
        }

        /// <summary>
        /// Updates information about a user in Azure Active Directory.
        /// </summary>
        public async Task<IHttpActionResult> Post(IdentityUpdate identity)
        {
            if (StsConfiguration.StsType != StsType.AzureActiveDirectory)
            {
                return this.BadRequest("Updating user information is only supported when using Azure Active Directory.");
            }
            if (identity != null && !string.IsNullOrWhiteSpace(identity.DisplayName))
            {
                var userId = this.User.GetUniqueIdentifier();
                var graphClient = new AadGraphClient(StsConfiguration.Authority, StsConfiguration.AadTenant, SiteConfiguration.TodoListWebApiClientId, SiteConfiguration.TodoListWebApiClientSecret);
                await graphClient.UpdateUserAsync(userId, identity.DisplayName);
            }
            return Ok();
        }
    }
}