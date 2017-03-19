using System.Collections.Generic;

namespace Common
{
    /// <summary>
    /// Represents information about an identity as seen from an application.
    /// </summary>
    public class IdentityInfo
    {
        #region Properties

        /// <summary>
        /// The source of the identity information.
        /// </summary>
        public string Source { get; set; }

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
    }
}