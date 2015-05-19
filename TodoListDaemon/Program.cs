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
            try
            {
                var identity = GetIdentityInfoFromWebApiAsync().Result;
                identity.WriteToConsole();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }

        private static X509Certificate2 GetCertificate(string certificateName)
        {
            var store = new X509Store(StoreLocation.CurrentUser);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                return store.Certificates.Find(X509FindType.FindBySubjectName, certificateName, false).Cast<X509Certificate2>().FirstOrDefault();
            }
            finally
            {
                store.Close();
            }
        }

        private static async Task<IdentityInfo> GetIdentityInfoFromWebApiAsync()
        {
            // Get identity information from the Todo List Web API.
            var todoListWebApiClient = await GetTodoListClient();
            var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, AppConfiguration.TodoListWebApiRootUrl + "api/identity");
            var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(todoListWebApiIdentityInfoRequest);
            todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);
        }

        private static async Task<HttpClient> GetTodoListClient()
        {
            // Get a token to authenticate against the Web API.
            var context = new AuthenticationContext(AppConfiguration.AadAuthority);
            var certificate = GetCertificate(AppConfiguration.TodoListDaemonCertificateName);
            if (certificate == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "The certificate with subject name \"{0}\" could not be found, please create it first.", AppConfiguration.TodoListDaemonCertificateName));
            }
            var clientCertificate = new ClientAssertionCertificate(AppConfiguration.TodoListDaemonClientId, certificate);
            var result = await context.AcquireTokenAsync(AppConfiguration.TodoListWebApiResourceId, clientCertificate);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }
    }
}