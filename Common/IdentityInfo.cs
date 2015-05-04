using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Common
{
    public class IdentityInfo
    {
        #region Properties

        public string Application { get; set; }
        public bool IsAuthenticated { get; set; }
        public string AuthenticationType { get; set; }
        public string Name { get; set; }
        public IList<ClaimInfo> Claims { get; set; }
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

        public static IdentityInfo FromCurrent(string application, IList<IdentityInfo> relatedApplicationIdentities)
        {
            var identity = (ClaimsIdentity)ClaimsPrincipal.Current.Identity;
            return new IdentityInfo
            {
                Application = application,
                IsAuthenticated = identity.IsAuthenticated,
                Name = identity.Name,
                AuthenticationType = identity.AuthenticationType,
                Claims = identity.Claims.Select(c => new ClaimInfo { Issuer = c.Issuer, Type = c.Type, Value = c.Value, Remark = GetRemark(c) }).ToArray(),
                RelatedApplicationIdentities = relatedApplicationIdentities ?? new IdentityInfo[0]
            };
        }

        private static string GetRemark(Claim claim)
        {
            switch (claim.Type.ToLowerInvariant())
            {
                case "aud":
                    return "Audience URI of the targeted application";
                case "iss":
                    return "Issued by Security Token Service";
                case "iat":
                    return GetUnixTimestamp("Issued at", claim.Value).ToString();
                case "nbf":
                    return GetUnixTimestamp("Not valid before", claim.Value).ToString();
                case "exp":
                    return GetUnixTimestamp("Not valid after", claim.Value).ToString();
                case "ver":
                    return "Version";
            }
            return null;
        }

        private static readonly DateTimeOffset UnixTimestampEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private static string GetUnixTimestamp(string prefix, string timestamp)
        {
            var secondsElapsed = default(int);
            if (int.TryParse(timestamp, out secondsElapsed))
            {
                return prefix + " " + UnixTimestampEpoch.AddSeconds(secondsElapsed);
            }
            return null;
        }

        #endregion
    }
}