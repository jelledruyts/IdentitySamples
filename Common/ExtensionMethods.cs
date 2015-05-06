using System;
using System.Security.Claims;

namespace Common
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets a unique identifier for the specified principal.
        /// </summary>
        /// <param name="principal">The principal.</param>
        /// <returns>A unique identifier for the specified principal.</returns>
        public static string GetUniqueIdentifier(this ClaimsPrincipal principal)
        {
            return ((ClaimsIdentity)principal.Identity).GetUniqueIdentifier();
        }

        /// <summary>
        /// Gets a unique identifier for the specified identity.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns>A unique identifier for the specified identity.</returns>
        public static string GetUniqueIdentifier(this ClaimsIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }

            // [NOTE] The "Object Identifier" claim is ensured to be unique, non-changeable and non-reusable across multiple identities.
            var objectIdentifierClaim = identity.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (objectIdentifierClaim == null || string.IsNullOrWhiteSpace(objectIdentifierClaim.Value))
            {
                throw new ArgumentException("The specified identity does not contain an object identifier claim.");
            }
            return objectIdentifierClaim.Value;
        }
    }
}