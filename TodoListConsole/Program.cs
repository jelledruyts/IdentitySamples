﻿using Common;
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
            // [SCENARIO] OAuth 2.0 Authorization Code Grant, Public Client
            // Get a token to authenticate against the Web API.
            var context = new AuthenticationContext(AppConfiguration.AadAuthority);
            var result = context.AcquireToken(AppConfiguration.TodoListWebApiResourceId, AppConfiguration.TodoListConsoleClientId, new Uri(AppConfiguration.TodoListConsoleRedirectUrl));

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }
    }
}