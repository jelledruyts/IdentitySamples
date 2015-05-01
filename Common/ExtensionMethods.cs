using System;
using System.Security.Claims;

namespace Common
{
    public static class ExtensionMethods
    {
        public static string GetUniqueIdentifier(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException("principal");
            }

            // The "Object Identifier" is ensured to be unique, non-changeable and non-reusable across multiple identities.
            var objectIdentifierClaim = principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (objectIdentifierClaim == null || string.IsNullOrWhiteSpace(objectIdentifierClaim.Value))
            {
                throw new ArgumentException("The specified principal does not contain an object identifier claim.");
            }
            return objectIdentifierClaim.Value;
        }
    }
}