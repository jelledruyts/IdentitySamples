using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TodoListConsole
{
    class Program
    {
        // The STA threading model is necessary for the ADAL web browser popup that authenticates with the authority.
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                var identity = GetIdentityInfoFromWebApiAsync().Result;
                WriteToConsole(identity, 0);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
            }
        }

        private static async Task<IdentityInfo> GetIdentityInfoFromWebApiAsync()
        {
            // Get identity information from the Todo List Web API.
            var todoListWebApiClient = GetTodoListClient();
            var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, AppConfiguration.TodoListWebApiRootUrl + "api/identity");
            var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(todoListWebApiIdentityInfoRequest);
            todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);
        }

        private static HttpClient GetTodoListClient()
        {
            // Get a token to authenticate against the Web API.
            var context = new AuthenticationContext(AppConfiguration.AadAuthority);
            var result = context.AcquireToken(AppConfiguration.TodoListWebApiResourceId, AppConfiguration.TodoListConsoleClientId, new Uri(AppConfiguration.TodoListConsoleRedirectUrl));

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }

        private static void WriteToConsole(IdentityInfo identity, int indentationLevel)
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
    }
}