using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TodoListConsole
{
    class Program
    {
        // [NOTE] The STA threading model is necessary for the ADAL web browser popup that authenticates with the authority.
        [STAThread]
        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("A - Sign in and show identity information as seen by the Web API");
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
            var todoListWebApiClient = await GetTodoListClientAsync(true);
            var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, AppConfiguration.TodoListWebApiRootUrl + "api/identity");
            var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(todoListWebApiIdentityInfoRequest);
            todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);
        }

        private static async Task<HttpClient> GetTodoListClientAsync(bool forceLogin)
        {
            // [SCENARIO] OAuth 2.0 Authorization Code Grant, Public Client
            // Get a token to authenticate against the Web API.
            var promptBehavior = forceLogin ? PromptBehavior.Always : PromptBehavior.Auto;
            var context = new AuthenticationContext(StsConfiguration.Authority, StsConfiguration.CanValidateAuthority);
            var result = await context.AcquireTokenAsync(AppConfiguration.TodoListWebApiResourceId, AppConfiguration.TodoListConsoleClientId, new Uri(AppConfiguration.TodoListConsoleRedirectUrl), new PlatformParameters(promptBehavior));

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }
    }
}