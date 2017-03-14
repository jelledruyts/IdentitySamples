using System;
using System.Configuration;

namespace Common
{
    public static class StsConfiguration
    {
        public static StsType StsType { get; private set; }
        public static string AadTenant { get; private set; }
        public static string Authority { get; private set; }
        public static string FederationMetadataUrl { get; private set; }
        public static string NameClaimType { get; private set; } // [NOTE] This indicates the claim type where the user's display name is defined
        public static string RoleClaimType { get; private set; } // [NOTE] This indicates the claim type where the user's roles are defined
        public static bool CanValidateAuthority { get; private set; }
        private const string FederationMetadataPath = "/federationmetadata/2007-06/federationmetadata.xml";

        static StsConfiguration()
        {
            var stsRootUrl = ConfigurationManager.AppSettings["StsRootUrl"];
            var stsPath = ConfigurationManager.AppSettings["StsPath"];
            if (!string.IsNullOrWhiteSpace(stsRootUrl))
            {
                Initialize(stsRootUrl, stsPath);
            }
        }

        public static void Initialize(string stsRootUrl, string stsPath)
        {
            stsRootUrl = stsRootUrl.TrimEnd('/');
            stsPath = stsPath.Trim('/');
            Authority = stsRootUrl + "/" + stsPath;

            // Determine if it's AD FS or Azure AD and act accordingly.
            if (stsPath.Equals("adfs", StringComparison.InvariantCultureIgnoreCase))
            {
                StsType = StsType.ActiveDirectoryFederationServices;
                FederationMetadataUrl = stsRootUrl + FederationMetadataPath;
                NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
                RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
                CanValidateAuthority = false; // ADAL does not support authority validation for AD FS
            }
            else
            {
                StsType = StsType.AzureActiveDirectory;
                AadTenant = stsPath;
                FederationMetadataUrl = Authority + FederationMetadataPath;
                NameClaimType = "name";
                RoleClaimType = "roles";
                CanValidateAuthority = true;
            }
        }
    }
}