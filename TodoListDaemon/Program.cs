using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
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
                    Console.WriteLine("A - Show daemon identity information as seen by the Web API");
                    Console.Write("Type your choice and press Enter: ");
                    var choice = Console.ReadLine();
                    if (string.Equals(choice, "A", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var identity = GetIdentityInfoFromWebApiAsync().Result;
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

        private static async Task<IdentityInfo> GetIdentityInfoFromWebApiAsync()
        {
            // Get identity information from the Todo List Web API.
            var token = await GetTokenAsync();
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

        private static async Task<AuthenticationResult> GetTokenAsync()
        {
            // [SCENARIO] OAuth 2.0 Client Credential Grant with Client Certificate
            // Get a token to authenticate against the Web API.
            var context = new AuthenticationContext(StsConfiguration.Authority, StsConfiguration.CanValidateAuthority);
            var certificate = GetCertificate(AppConfiguration.TodoListDaemonCertificateName);
            var clientCertificate = new ClientAssertionCertificate(AppConfiguration.TodoListDaemonClientId, certificate);
            return await context.AcquireTokenAsync(AppConfiguration.TodoListWebApiResourceId, clientCertificate);
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
    }
}