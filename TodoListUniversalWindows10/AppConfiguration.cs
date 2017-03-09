﻿namespace TodoListUniversalWindows10
{
    public static class AppConfiguration
    {
        public const string TodoListWindows10ClientId = "910d393b-ba70-4205-ad3e-da88d5385e9f";
        public const string TodoListWebApiResourceId = "http://identitysamples/todolistwebapi";

        public const string StsRootUrl = "https://login.microsoftonline.com/";
        public const string StsPath = "identitysamples.onmicrosoft.com";
        public const string StsAuthority = StsRootUrl + StsPath;
        // [NOTE] Use the following account provider authority value:
        //   - The full tenant name for one specific Azure AD tenant
        //   - "organizations" for *any* Azure AD tenant, or when using AD FS
        //   - "consumers" for a Microsoft Account
        public const string AccountProviderAuthority = "https://login.microsoftonline.com/identitysamples.onmicrosoft.com";
        // Authority validation is not supported for AD FS, only for Azure AD.
        public const bool CanValidateAuthority = true;
    }
}
