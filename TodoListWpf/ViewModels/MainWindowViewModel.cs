using Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Windows;
using TodoListWpf.Models;

namespace TodoListWpf.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Properties

        public string ApplicationName { get { return AppConfiguration.ApplicationName; } }

        private string statusText;
        public string StatusText { get { return this.statusText; } set { if (this.statusText != value) { this.statusText = value; OnPropertyChanged(); } } }

        public AsyncRelayCommand SignInCommand { get; private set; }

        private ClaimsPrincipal currentUser;
        public ClaimsPrincipal CurrentUser { get { return this.currentUser; } set { if (this.currentUser != value) { this.currentUser = value; OnPropertyChanged(); } } }

        private IdentityInfo identityInfo;
        public IdentityInfo IdentityInfo { get { return this.identityInfo; } set { if (this.identityInfo != value) { this.identityInfo = value; OnPropertyChanged(); } } }

        public bool isUserSignedIn;
        public bool IsUserSignedIn { get { return this.isUserSignedIn; } set { if (this.isUserSignedIn != value) { this.isUserSignedIn = value; OnPropertyChanged(); } } }

        public AsyncRelayCommand CreateTodoItemCommand { get; private set; }

        public AsyncRelayCommand RefreshTodoListCommand { get; private set; }

        private IList<TodoItemViewModel> todoList;
        public IList<TodoItemViewModel> TodoList { get { return this.todoList; } set { if (this.todoList != value) { this.todoList = value; OnPropertyChanged(); } } }

        private IList<CategoryViewModel> categories;
        public IList<CategoryViewModel> Categories { get { return this.categories; } set { if (this.categories != value) { this.categories = value; OnPropertyChanged(); } } }

        private TodoItemCreate todoItemCreate;
        public TodoItemCreate TodoItemCreate { get { return this.todoItemCreate; } set { if (this.todoItemCreate != value) { this.todoItemCreate = value; OnPropertyChanged(); } } }

        #endregion

        #region Constructors

        public MainWindowViewModel()
        {
            this.SignInCommand = new AsyncRelayCommand(SignIn);
            this.RefreshTodoListCommand = new AsyncRelayCommand(RefreshTodoList, CanRefreshTodoList);
            this.CreateTodoItemCommand = new AsyncRelayCommand(CreateTodoItem, CanCreateTodoItem);
            this.TodoItemCreate = new TodoItemCreate();
            this.StatusText = "Ready";
        }

        #endregion

        #region SignIn Command

        private async Task SignIn(object argument)
        {
            try
            {
                this.StatusText = "Signing in...";

                // Retrieve identity information.
                this.IdentityInfo = await GetIdentityInfoAsync(true);

                // Sign in as the user that the Web API sees.
                var todoListWebApiIdentityClaims = this.IdentityInfo.Claims.Select(c => new Claim(c.Type, c.Value));
                this.CurrentUser = new ClaimsPrincipal(new ClaimsIdentity(todoListWebApiIdentityClaims, this.IdentityInfo.AuthenticationType, StsConfiguration.NameClaimType, StsConfiguration.RoleClaimType));
                this.IsUserSignedIn = true;

                // Immediately get the Todo List data as well.
                await RefreshTodoList(argument);

                this.StatusText = "Signed in as " + this.CurrentUser.Identity.Name;
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        #endregion

        #region RefreshTodoList Command

        private bool CanRefreshTodoList(object argument)
        {
            return this.CurrentUser != null;
        }

        private async Task RefreshTodoList(object argument)
        {
            try
            {
                this.StatusText = "Refreshing Todo List...";

                var data = await GetTodoListDataAsync();
                var categories = data.Item2;
                this.TodoList = data.Item1.Select(t => new TodoItemViewModel(t, categories.FirstOrDefault(c => string.Equals(c.Id, t.CategoryId, StringComparison.OrdinalIgnoreCase)))).ToList();
                this.Categories = categories.Select(c => new CategoryViewModel(c.Id, c.Name, c.IsPrivate)).ToList();
                this.TodoItemCreate = new TodoItemCreate { CategoryId = categories.Any() ? categories.First().Id : null };

                this.StatusText = string.Format(CultureInfo.CurrentCulture, "Retrieved {0} todo item(s)", this.TodoList.Count);
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        #endregion

        #region CreateTodoItem Command

        private bool CanCreateTodoItem(object argument)
        {
            return this.CurrentUser != null && this.TodoItemCreate != null && !string.IsNullOrWhiteSpace(this.TodoItemCreate.Title);
        }

        private async Task CreateTodoItem(object argument)
        {
            try
            {
                this.StatusText = "Submitting Todo Item...";

                await CreateTodoItem(this.TodoItemCreate);

                // Immediately refresh the data.
                await RefreshTodoList(argument);
            }
            catch (Exception exc)
            {
                ShowException(exc);
            }
        }

        #endregion

        #region ShowException

        private void ShowException(Exception exc)
        {
            MessageBox.Show(exc.ToString(), "An error occurred...", MessageBoxButton.OK, MessageBoxImage.Error);
            this.StatusText = "An error occurred: " + exc.Message;
        }

        #endregion

        #region Web API Communication

        private static async Task<IdentityInfo> GetIdentityInfoAsync(bool forceLogin)
        {
            // Get identity information from the Todo List Web API.
            var todoListWebApiClient = GetTodoListClient(forceLogin);
            var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, AppConfiguration.TodoListWebApiRootUrl + "api/identity");
            var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendAsync(todoListWebApiIdentityInfoRequest);
            todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);
        }

        private static async Task<Tuple<IList<TodoItem>, IList<Category>>> GetTodoListDataAsync()
        {
            var todoListWebApiClient = GetTodoListClient(false);

            // Get the todo list.
            var todoListRequest = new HttpRequestMessage(HttpMethod.Get, AppConfiguration.TodoListWebApiRootUrl + "api/todolist");
            var todoListResponse = await todoListWebApiClient.SendAsync(todoListRequest);
            todoListResponse.EnsureSuccessStatusCode();
            var todoListResponseString = await todoListResponse.Content.ReadAsStringAsync();
            var todoList = JsonConvert.DeserializeObject<List<TodoItem>>(todoListResponseString);

            // Get the categories.
            var categoriesRequest = new HttpRequestMessage(HttpMethod.Get, AppConfiguration.TodoListWebApiRootUrl + "api/category");
            var categoriesResponse = await todoListWebApiClient.SendAsync(categoriesRequest);
            categoriesResponse.EnsureSuccessStatusCode();
            var categoriesResponseString = await categoriesResponse.Content.ReadAsStringAsync();
            var categories = JsonConvert.DeserializeObject<List<Category>>(categoriesResponseString);

            return new Tuple<IList<TodoItem>, IList<Category>>(todoList, categories);
        }

        private static async Task CreateTodoItem(TodoItemCreate item)
        {
            var client = GetTodoListClient(false);
            var newTodoItemRequest = new HttpRequestMessage(HttpMethod.Post, AppConfiguration.TodoListWebApiRootUrl + "api/todolist");
            newTodoItemRequest.Content = new JsonContent(item);
            var newTodoItemResponse = await client.SendAsync(newTodoItemRequest);
            newTodoItemResponse.EnsureSuccessStatusCode();
        }

        private static HttpClient GetTodoListClient(bool forceLogin)
        {
            // [SCENARIO] OAuth 2.0 Authorization Code Grant, Public Client
            // Get a token to authenticate against the Web API.
            var promptBehavior = forceLogin ? PromptBehavior.Always : PromptBehavior.Auto;
            var context = new AuthenticationContext(StsConfiguration.Authority, StsConfiguration.CanValidateAuthority);
            var result = context.AcquireToken(AppConfiguration.TodoListWebApiResourceId, AppConfiguration.TodoListWpfClientId, new Uri(AppConfiguration.TodoListWpfRedirectUrl), promptBehavior);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            return client;
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}