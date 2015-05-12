using Microsoft.Azure.ActiveDirectory.GraphClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Represents information about an identity as seen from an application.
    /// </summary>
    public class IdentityInfo
    {
        #region Constants

        private static readonly string[] ClaimTypesToSkip = { "nonce", "at_hash", "c_hash" };
        private static readonly string[] GroupClaimTypes = { "groups", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups" };
        private static readonly string[] RoleClaimTypes = { "roles", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" };
        private static readonly DateTimeOffset UnixTimestampEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        #endregion

        #region Properties

        /// <summary>
        /// The application from which the identity is observed.
        /// </summary>
        public string Application { get; set; }

        /// <summary>
        /// Determines if the identity is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; set; }

        /// <summary>
        /// The authentication type.
        /// </summary>
        public string AuthenticationType { get; set; }

        /// <summary>
        /// The identity name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The names of the groups the user is a member of.
        /// </summary>
        public IList<string> GroupNames { get; set; }

        /// <summary>
        /// The names of the roles the user has.
        /// </summary>
        public IList<string> RoleNames { get; set; }

        /// <summary>
        /// The claims.
        /// </summary>
        public IList<ClaimInfo> Claims { get; set; }

        /// <summary>
        /// The identities as seen from other applications related to the current application.
        /// </summary>
        public IList<IdentityInfo> RelatedApplicationIdentities { get; set; }

        #endregion

        #region Constructors

        public IdentityInfo()
        {
            this.Claims = new List<ClaimInfo>();
            this.RelatedApplicationIdentities = new List<IdentityInfo>();
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates identity infromation about the current principal's identity.
        /// </summary>
        /// <param name="application">The application from which the identity is observed.</param>
        /// <param name="relatedApplicationIdentities">The identities as seen from other applications related to the current application.</param>
        /// <returns>Identity information about the current principal's identity</returns>
        public static async Task<IdentityInfo> FromCurrent(string application, IList<IdentityInfo> relatedApplicationIdentities, AadGraphClient graphClient)
        {
            return await FromIdentity((ClaimsIdentity)ClaimsPrincipal.Current.Identity, application, relatedApplicationIdentities, graphClient);
        }

        /// <summary>
        /// Creates identity infromation about the specified identity.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <param name="application">The application from which the identity is observed.</param>
        /// <param name="relatedApplicationIdentities">The identities as seen from other applications related to the current application.</param>
        /// <param name="graphClient">The graph client used to look up group claim details.</param>
        /// <returns>Identity information about the specified identity.</returns>
        public static async Task<IdentityInfo> FromIdentity(ClaimsIdentity identity, string application, IList<IdentityInfo> relatedApplicationIdentities, AadGraphClient graphClient)
        {
            var groups = default(IList<IGroup>);
            if (graphClient != null)
            {
                // Look up all the Azure AD groups for which there are group claims.
                // [NOTE] To get group claims in the token, ensure to update the Azure AD application manifest.
                // Change "groupMembershipClaims" from null to "SecurityGroup" (or "All" to include distribution groups).
                // See http://www.dushyantgill.com/blog/2014/12/10/authorization-cloud-applications-using-ad-groups/ for more information.
                var groupIds = identity.Claims.Where(claim => GroupClaimTypes.Any(groupClaimType => string.Equals(claim.Type, groupClaimType, StringComparison.OrdinalIgnoreCase))).Select(claim => claim.Value).ToArray();
                groups = await graphClient.GetGroupsAsync(groupIds);
            }

            // [NOTE] Inspect the identity and its claims.
            return new IdentityInfo
            {
                Application = application,
                IsAuthenticated = identity.IsAuthenticated,
                Name = identity.Name,
                AuthenticationType = identity.AuthenticationType,
                GroupNames = (groups == null ? new string[0] : groups.Select(g => g.DisplayName).ToArray()),
                RoleNames = identity.Claims.Where(claim => RoleClaimTypes.Any(roleClaimType => string.Equals(claim.Type, roleClaimType, StringComparison.OrdinalIgnoreCase))).Select(claim => claim.Value).ToArray(),
                Claims = identity.Claims.Where(claim => !ClaimTypesToSkip.Any(claimTypeToSkip => string.Equals(claimTypeToSkip, claim.Type, StringComparison.OrdinalIgnoreCase))).Select(claim => new ClaimInfo { Issuer = claim.Issuer, Type = claim.Type, Value = claim.Value, Remark = GetRemark(claim, groups) }).ToArray(),
                RelatedApplicationIdentities = relatedApplicationIdentities ?? new IdentityInfo[0]
            };
        }

        private static string GetRemark(Claim claim, IList<IGroup> groups)
        {
            // [NOTE] Certain claims can be interpreted to more meaningful information.
            switch (claim.Type.ToLowerInvariant())
            {
                case "aud":
                    return "Audience URI of the targeted application";
                case "iss":
                    return "Issued by Security Token Service";
                case "iat":
                    return GetTimestampDescription("Issued at", claim.Value, true);
                case "nbf":
                    return GetTimestampDescription("Not valid before", claim.Value, true);
                case "exp":
                    return GetTimestampDescription("Not valid after", claim.Value, true);
                case "ver":
                    return "Version";
                case "pwd_exp":
                    return GetTimestampDescription("Password expires", claim.Value, false);
            }
            if (groups != null && GroupClaimTypes.Any(groupClaimType => string.Equals(claim.Type, groupClaimType, StringComparison.OrdinalIgnoreCase)))
            {
                // This is a group claim, look up the group details.
                var group = groups.FirstOrDefault(g => string.Equals(g.ObjectId, claim.Value, StringComparison.OrdinalIgnoreCase));
                if (group != null)
                {
                    return group.DisplayName;
                }
            }
            return null;
        }

        private static string GetTimestampDescription(string prefix, string timestamp, bool secondsSinceEpoch)
        {
            var timestampValue = default(int);
            if (int.TryParse(timestamp, out timestampValue))
            {
                var utcTimestamp = default(DateTimeOffset);
                if (secondsSinceEpoch)
                {
                    utcTimestamp = UnixTimestampEpoch.AddSeconds(timestampValue);
                }
                else
                {
                    utcTimestamp = DateTimeOffset.UtcNow.AddSeconds(timestampValue);
                }
                return string.Format(CultureInfo.CurrentCulture, "{0} {1} UTC ({2} Local Time)", prefix, utcTimestamp.ToString(), utcTimestamp.LocalDateTime.ToString());
            }
            return null;
        }

        #endregion
    }
}