using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace TodoListDaemon
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("A - Show daemon identity information as seen by the Web API, using X509 Certificate Authentication");
                    if (StsConfiguration.StsType == StsType.ActiveDirectoryFederationServices)
                    {
                        Console.WriteLine("B - Show daemon identity information as seen by the Web API, using Windows Integrated Authentication");
                    }
                    Console.Write("Type your choice and press Enter: ");
                    var choice = Console.ReadLine();
                    if (string.Equals(choice, "A", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var identity = GetIdentityInfoFromWebApiAsync(false).Result;
                        identity.WriteToConsole();
                    }
                    else if (string.Equals(choice, "B", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var identity = GetIdentityInfoFromWebApiAsync(true).Result;
                        identity.WriteToConsole();
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc.ToString());
                }
            }
        }

        private static async Task<IdentityInfo> GetIdentityInfoFromWebApiAsync(bool useWindowsIntegratedAuthentication)
        {
            // Get identity information from the Todo List Web API.
            var token = useWindowsIntegratedAuthentication ? await GetTokenUsingWiaAsync() : await GetTokenUsingClientCertificateAsync();
            var todoListWebApiClient = GetTodoListClient(token.AccessToken);
            var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, AppConfiguration.TodoListWebApiRootUrl + "api/identity");
            var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(todoListWebApiIdentityInfoRequest);
            todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
            var todoListWebApiIdentityInfo = JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);
            return await IdentityInfoFactory.FromJwt(token.IdToken, "ID Token", AppConfiguration.ApplicationName, new[] { todoListWebApiIdentityInfo });
        }

        private static HttpClient GetTodoListClient(string accessToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        private static async Task<TokenResult> GetTokenUsingClientCertificateAsync()
        {
            // [SCENARIO] OAuth 2.0 Client Credential Grant with Client Certificate
            // Get a token to authenticate against the Web API.
            var context = new AuthenticationContext(StsConfiguration.Authority, StsConfiguration.CanValidateAuthority);
            var certificate = GetCertificate(AppConfiguration.TodoListDaemonCertificateName);
            var clientCertificate = new ClientAssertionCertificate(AppConfiguration.TodoListDaemonClientId, certificate);
            var result = await context.AcquireTokenAsync(AppConfiguration.TodoListWebApiResourceId, clientCertificate);
            return new TokenResult(result.AccessToken, result.IdToken);
        }

        private static async Task<TokenResult> GetTokenUsingWiaAsync()
        {
            // [SCENARIO] OAuth 2.0 Client Credential Grant with Windows Integrated Authentication on AD FS
            // Get a token to authenticate against the Web API.
            // ADAL does not support this scenario so perform the client credentials token request manually.
            using (var handler = new HttpClientHandler { UseDefaultCredentials = true })
            using (var client = new HttpClient(handler))
            {
                var parameters = new Dictionary<string, string>();
                parameters.Add("resource", AppConfiguration.TodoListWebApiResourceId);
                parameters.Add("client_id", AppConfiguration.TodoListDaemonClientId);
                parameters.Add("grant_type", "client_credentials");
                parameters.Add("use_windows_client_authentication", "true");
                using (var content = new FormUrlEncodedContent(parameters))
                using (var response = await client.PostAsync(StsConfiguration.Authority + "/oauth2/token", content))
                {
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var responseValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseBody);
                    var accessToken = responseValues["access_token"];
                    return new TokenResult(accessToken, null);
                }
            }
        }

        private static X509Certificate2 GetCertificate(string certificateName)
        {
            var store = new X509Store(StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                var certificate = store.Certificates.Find(X509FindType.FindBySubjectName, certificateName, false).Cast<X509Certificate2>().FirstOrDefault();
                if (certificate == null)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "The certificate with subject name \"{0}\" could not be found, please create it first.", AppConfiguration.TodoListDaemonCertificateName));
                }
                return certificate;
            }
            finally
            {
                store.Close();
            }
        }

        private class TokenResult
        {
            public string AccessToken { get; set; }
            public string IdToken { get; set; }

            public TokenResult(string accessToken, string idToken)
            {
                this.AccessToken = accessToken;
                this.IdToken = idToken;
            }
        }
    }
}