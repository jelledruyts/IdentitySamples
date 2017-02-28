using System;
using System.Configuration;

namespace Common
{
    public static class StsConfiguration
    {
        public static readonly StsType StsType;
        public static readonly string AadTenant;
        public static readonly string Authority;
        public static readonly string FederationMetadataUrl;
        public static readonly string NameClaimType; // [NOTE] This indicates the claim type where the user's display name is defined
        public static readonly string RoleClaimType; // [NOTE] This indicates the claim type where the user's roles are defined
        public static readonly bool CanValidateAuthority;
        private const string FederationMetadataPath = "/federationmetadata/2007-06/federationmetadata.xml";

        static StsConfiguration()
        {
            var stsRootUrl = ConfigurationManager.AppSettings["StsRootUrl"].TrimEnd('/');
            var stsPath = ConfigurationManager.AppSettings["StsPath"].Trim('/');
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