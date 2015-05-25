
namespace TodoListUniversal
{
    public static class AppConfiguration
    {
#if WINDOWS_PHONE_APP
        public const string TodoListWindowsPhoneRedirectUrl = "ms-app://s-1-15-2-3475013502-1635785671-2604573005-3618826432-4246637727-2481625662-377198457/";
        public const string TodoListWindowsPhoneClientId = "835e56f5-9285-4578-b55b-f0207218ff1d";
#else
        public const string TodoListWindowsRedirectUrl = "ms-app://s-1-15-2-1560001648-135754094-621000606-2169641607-3755769251-1805572070-2548304156/";
        public const string TodoListWindowsClientId = "67d3ed0e-325c-455c-b012-0fcf6409826c";
#endif 
        public const string AadEndpoint = "https://login.microsoftonline.com/";

        public const string TodoListWebApiRootUrl = "https://localhost:44307/";
        public const string TodoListWebApiResourceId = "http://identitysamples/todolistwebapi";

        public const string AadTenant = "identitysamples.onmicrosoft.com";
        public const string AadAuthority = AadEndpoint + AadTenant;
    }
}