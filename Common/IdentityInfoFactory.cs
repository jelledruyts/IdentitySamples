using Microsoft.Azure.ActiveDirectory.GraphClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Common
{
    public static class IdentityInfoFactory
    {
        #region Constants

        private static readonly string[] GroupClaimTypes = { "groups", "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups" };
        private static readonly string[] RoleClaimTypes = { "roles", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" };
        private static readonly DateTimeOffset UnixTimestampEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates identity infromation about the specified principal's identity.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <param name="application">The application from which the identity is observed.</param>
        /// <param name="relatedApplicationIdentities">The identities as seen from other applications related to the current application.</param>
        /// <returns>Identity information about the current principal's identity.</returns>
        public static async Task<IdentityInfo> FromPrincipal(IPrincipal principal, string source, string application, IList<IdentityInfo> relatedApplicationIdentities, AadGraphClient graphClient)
        {
            return await FromIdentity(principal.Identity as ClaimsIdentity, source, application, relatedApplicationIdentities, graphClient);
        }

        /// <summary>
        /// Creates identity infromation about the specified identity.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <param name="application">The application from which the identity is observed.</param>
        /// <param name="relatedApplicationIdentities">The identities as seen from other applications related to the current application.</param>
        /// <param name="graphClient">The graph client used to look up group claim details.</param>
        /// <returns>Identity information about the specified identity.</returns>
        public static async Task<IdentityInfo> FromIdentity(ClaimsIdentity identity, string source, string application, IList<IdentityInfo> relatedApplicationIdentities, AadGraphClient graphClient)
        {
            if (identity == null)
            {
                return new IdentityInfo
                {
                    Source = source,
                    Application = application,
                    IsAuthenticated = false,
                    RelatedApplicationIdentities = relatedApplicationIdentities
                };
            }

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
                Source = source,
                Application = application,
                IsAuthenticated = identity.IsAuthenticated,
                Name = identity.Name,
                AuthenticationType = identity.AuthenticationType,
                GroupNames = (groups == null ? new string[0] : groups.Select(g => g.DisplayName).ToArray()),
                RoleNames = identity.Claims.Where(claim => RoleClaimTypes.Any(roleClaimType => string.Equals(claim.Type, roleClaimType, StringComparison.OrdinalIgnoreCase))).Select(claim => claim.Value).ToArray(),
                Claims = identity.Claims.Select(claim => new ClaimInfo { Issuer = claim.Issuer, Type = claim.Type, Value = claim.Value, Remark = GetRemark(claim, groups) }).ToArray(),
                RelatedApplicationIdentities = relatedApplicationIdentities ?? new IdentityInfo[0]
            };
        }

        /// <summary>
        /// Creates identity infromation about the claims represented in the specified JWT token.
        /// </summary>
        /// <param name="jwt">The JWT token.</param>
        /// <param name="application">The application from which the identity is observed.</param>
        /// <param name="relatedApplicationIdentities">The identities as seen from other applications related to the current application.</param>
        /// <returns>Identity information about the specified JWT token.</returns>
        public static async Task<IdentityInfo> FromJwt(string jwt, string source, string application, IList<IdentityInfo> relatedApplicationIdentities)
        {
            return await FromJwt(jwt, source, application, relatedApplicationIdentities, null);
        }

        /// <summary>
        /// Creates identity infromation about the claims represented in the specified JWT token.
        /// </summary>
        /// <param name="jwt">The JWT token.</param>
        /// <param name="application">The application from which the identity is observed.</param>
        /// <param name="relatedApplicationIdentities">The identities as seen from other applications related to the current application.</param>
        /// <param name="graphClient">The graph client used to look up group claim details.</param>
        /// <returns>Identity information about the specified JWT token.</returns>
        public static async Task<IdentityInfo> FromJwt(string jwt, string source, string application, IList<IdentityInfo> relatedApplicationIdentities, AadGraphClient graphClient)
        {
            try
            {
                var token = new JwtSecurityToken(jwt);
                var identity = new ClaimsIdentity(token.Claims, "JWT", StsConfiguration.NameClaimType, StsConfiguration.RoleClaimType);
                return await FromIdentity(identity, source, application, relatedApplicationIdentities, graphClient);
            }
            catch (Exception)
            {
                // The JWT string is not a valid token, return an unauthenticated identity.
                return await FromIdentity(null, source, application, relatedApplicationIdentities, graphClient);
            }
        }

        /// <summary>
        /// Creates identity information based on an exception.
        /// </summary>
        /// <param name="application">The application from which the identity is observed.</param>
        /// <param name="exc">The exception.</param>
        /// <returns>Identity information representing the exception.</returns>
        public static IdentityInfo FromException(string application, Exception exc)
        {
            return new IdentityInfo
            {
                Source = "Exception",
                Application = application,
                IsAuthenticated = false,
                Claims = new[] {
                    new ClaimInfo { Type = "ExceptionMessage", Value = exc.Message },
                    new ClaimInfo { Type = "ExceptionDetail", Value = exc.ToString() }
                }
            };
        }

        private static string GetRemark(Claim claim, IList<IGroup> groups)
        {
            // [NOTE] Certain claims can be interpreted to more meaningful information.
            // See https://msdn.microsoft.com/en-us/library/azure/dn195587.aspx and
            // https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-token-and-claims among others.
            switch (claim.Type.ToLowerInvariant())
            {
                case "aud":
                    return "Audience URI of the targeted application, i.e. the intended recipient of the token";
                case "iss":
                    return "Issued by Security Token Service";
                case "idp":
                    return "Identity Provider";
                case "scp":
                case "http://schemas.microsoft.com/identity/claims/scope":
                    return "Scope, i.e. the impersonation permissions granted to the client application";
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
                case "appid":
                    return "Application id of the client that is using the token to access a resource";
                case "appidacr":
                    return "Application Authentication Context Class Reference" + (claim.Value == "0" ? ": Public Client" : (claim.Value == "1" ? ": Confidential Client (Client ID + Secret)" : (claim.Value == "2" ? ": Confidential Client (X509 Certificate)" : null)));
                case "auth_time":
                    return GetTimestampDescription("Authentication time", claim.Value, true);
                case "http://schemas.microsoft.com/ws/2008/06/identity/claims/authenticationinstant":
                    return GetTimestampDescription("Authentication instant", claim.Value, true);
                case "oid":
                    return "Object identifier";
                case "sub":
                    return "Subject, i.e. the principal about which the token asserts information, such as the user of an application";
                case "tid":
                    return "Tenant identifier";
                case "http://schemas.microsoft.com/claims/authnmethodsreferences":
                case "amr":
                    return "Authentication method";
                case "http://schemas.microsoft.com/claims/authnclassreference":
                case "acr":
                    return "Authentication Context Class Reference" + (claim.Value == "0" ? ": End-user authentication did not meet the requirements of ISO/IEC 29115" : null);
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