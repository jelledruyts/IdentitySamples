using System;
using System.Linq;
using System.Security.Claims;

namespace Common
{
    public static class ExtensionMethods
    {
        #region ClaimsPrincipal.GetUniqueIdentifier

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

        #endregion

        #region IdentityInfo.WriteToConsole

        public static void WriteToConsole(this IdentityInfo identity)
        {
            identity.WriteToConsole(0);
        }

        private static void WriteToConsole(this IdentityInfo identity, int indentationLevel)
        {
            // Identity.
            WritePropertyToConsole("Application\t", identity.Application, indentationLevel);
            WritePropertyToConsole("Is Authenticated", identity.IsAuthenticated.ToString(), indentationLevel);
            WritePropertyToConsole("Name\t\t", identity.Name, indentationLevel);
            WritePropertyToConsole("Authentication Type", identity.AuthenticationType, indentationLevel);
            if (identity.RoleNames != null && identity.RoleNames.Any())
            {
                WritePropertyToConsole("Application Roles", string.Join(", ", identity.RoleNames), indentationLevel);
            }
            if (identity.GroupNames != null && identity.GroupNames.Any())
            {
                WritePropertyToConsole("Groups\t\t", string.Join(", ", identity.GroupNames), indentationLevel);
            }

            // Claims.
            Console.WriteLine();
            Console.WriteLine(GetIndentationPrefix(indentationLevel) + "Claims:");
            foreach (var claim in identity.Claims)
            {
                WritePropertyToConsole("   Type", claim.Type, indentationLevel, ConsoleColor.Magenta);
                WritePropertyToConsole("  Value", claim.Value, indentationLevel);
                if (!string.IsNullOrWhiteSpace(claim.Remark))
                {
                    WritePropertyToConsole(" Remark", claim.Remark, indentationLevel, ConsoleColor.Gray);
                }
                Console.WriteLine();
            }

            // Related Application Identities.
            if (identity.RelatedApplicationIdentities != null && identity.RelatedApplicationIdentities.Any())
            {
                Console.WriteLine();
                Console.WriteLine(GetIndentationPrefix(indentationLevel) + "Related Application Identities:");
                foreach (var relatedApplicationIdentity in identity.RelatedApplicationIdentities)
                {
                    Console.WriteLine();
                    WriteToConsole(relatedApplicationIdentity, indentationLevel + 1);
                }
            }
        }

        private static void WritePropertyToConsole(string name, string value, int indentationLevel, ConsoleColor valueColor = ConsoleColor.Yellow)
        {
            var originalForegroundColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(GetIndentationPrefix(indentationLevel) + name + "\t: ");
                Console.ForegroundColor = valueColor;
                Console.WriteLine(value);
            }
            finally
            {
                Console.ForegroundColor = originalForegroundColor;
            }
        }

        private static string GetIndentationPrefix(int indentationLevel)
        {
            return new string('\t', indentationLevel);
        }

        #endregion
    }
}