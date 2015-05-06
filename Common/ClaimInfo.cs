
namespace Common
{
    /// <summary>
    /// Represents information about a claim.
    /// </summary>
    public class ClaimInfo
    {
        #region Properties

        /// <summary>
        /// The issuer of the claim.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// The claim type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The claim value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// A remark about the claim (e.g. the interpretation of its value).
        /// </summary>
        public string Remark { get; set; }

        #endregion
    }
}