namespace TodoListUniversalWindows10
{
    public static class AppConfiguration
    {
        public const string TodoListWindows10ClientId = "910d393b-ba70-4205-ad3e-da88d5385e9f";

        public const string AadEndpoint = "https://login.microsoftonline.com/";

        public const string TodoListWebApiResourceId = "http://identitysamples/todolistwebapi";

        public const string AadTenant = "identitysamples.onmicrosoft.com";
        public const string AadAuthority = AadEndpoint + AadTenant;
    }
}