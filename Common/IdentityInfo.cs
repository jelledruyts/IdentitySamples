using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace Common
{
    /// <summary>
    /// Represents information about an identity as seen from an application.
    /// </summary>
    public class IdentityInfo
    {
        #region Constants

        private static readonly string[] ClaimTypesToSkip = { "nonce", "at_hash", "c_hash" };

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
        public static IdentityInfo FromCurrent(string application, IList<IdentityInfo> relatedApplicationIdentities)
        {
            return FromIdentity((ClaimsIdentity)ClaimsPrincipal.Current.Identity, application, relatedApplicationIdentities);
        }

        /// <summary>
        /// Creates identity infromation about the specified identity.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <param name="application">The application from which the identity is observed.</param>
        /// <param name="relatedApplicationIdentities">The identities as seen from other applications related to the current application.</param>
        /// <returns>Identity information about the specified identity.</returns>
        public static IdentityInfo FromIdentity(ClaimsIdentity identity, string application, IList<IdentityInfo> relatedApplicationIdentities)
        {
            // [NOTE] Inspect the identity and its claims.
            return new IdentityInfo
            {
                Application = application,
                IsAuthenticated = identity.IsAuthenticated,
                Name = identity.Name,
                AuthenticationType = identity.AuthenticationType,
                Claims = identity.Claims.Where(c => !ClaimTypesToSkip.Any(s => string.Equals(s, c.Type, StringComparison.OrdinalIgnoreCase))).Select(c => new ClaimInfo { Issuer = c.Issuer, Type = c.Type, Value = c.Value, Remark = GetRemark(c) }).ToArray(),
                RelatedApplicationIdentities = relatedApplicationIdentities ?? new IdentityInfo[0]
            };
        }

        private static string GetRemark(Claim claim)
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
            return null;
        }

        private static readonly DateTimeOffset UnixTimestampEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

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