using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.Security.Cryptography.Certificates;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;

namespace TodoListUniversalWindows10
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        #region Properties

        private WebAccount account;
        public WebAccount Account { get { return this.account; } set { if (this.account != value) { this.account = value; OnPropertyChanged(); } } }

        private IdentityInfo identityInfo;
        public IdentityInfo IdentityInfo { get { return this.identityInfo; } set { if (this.identityInfo != value) { this.identityInfo = value; OnPropertyChanged(); } } }

        private Visibility identityInfoVisibility;
        public Visibility IdentityInfoVisibility { get { return this.identityInfoVisibility; } set { if (this.identityInfoVisibility != value) { this.identityInfoVisibility = value; OnPropertyChanged(); } } }

        private string statusText;
        public string StatusText { get { return this.statusText; } set { if (this.statusText != value) { this.statusText = value; OnPropertyChanged(); } } }

        public AsyncRelayCommand SignInCommand { get; private set; }
        public AsyncRelayCommand SignOutCommand { get; private set; }

        #endregion

        #region Constructors

        public MainPageViewModel()
        {
            // [NOTE] Use the line below to retrieve the Redirect URL for the application
            // that needs to be registered in Azure Active Directory.
            var redirectUri = string.Format("ms-appx-web://microsoft.aad.brokerplugin/{0}", WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpperInvariant());

            this.SignInCommand = new AsyncRelayCommand(SignIn, CanSignIn);
            this.SignOutCommand = new AsyncRelayCommand(SignOut, CanSignOut);
            this.IdentityInfoVisibility = Visibility.Collapsed;
        }

        #endregion

        #region SignIn Command

        private bool CanSignIn(object argument)
        {
            return this.Account == null;
        }

        private async Task SignIn(object argument)
        {
            try
            {
                this.StatusText = "Signing in...";
                ResetCommandStatus();

                var tokenResponse = await RequestTokenAsync(true);
                if (tokenResponse != null)
                {
                    this.Account = tokenResponse.WebAccount;

                    // Immediately retrieve identity information from the Web API.
                    this.StatusText = "Getting identity info...";
                    ResetCommandStatus();
                    this.IdentityInfo = await GetIdentityInfoFromWebApiAsync();
                    this.IdentityInfoVisibility = Visibility.Visible;
                    ResetCommandStatus();
                    this.StatusText = "Retrieved identity info from " + this.IdentityInfo.Application;
                }
            }
            catch (Exception exc)
            {
                await ShowExceptionAsync(exc);
            }
        }

        #endregion

        #region SignOut Command

        private bool CanSignOut(object argument)
        {
            return this.Account != null;
        }

        private async Task SignOut(object argument)
        {
            try
            {
                this.StatusText = "Signing out...";
                ResetCommandStatus();

                await this.Account.SignOutAsync();
                this.Account = null;
                this.IdentityInfo = null;
                this.IdentityInfoVisibility = Visibility.Collapsed;

                ResetCommandStatus();
                this.StatusText = "Signed out";
            }
            catch (Exception exc)
            {
                await ShowExceptionAsync(exc);
            }
        }

        #endregion

        #region Dialogs

        private async Task ShowExceptionAsync(Exception exc)
        {
            if (exc != null)
            {
                await ShowDialogAsync(exc.ToString(), "An error occurred...", "An error occurred: " + exc.Message);
            }
        }

        private async Task ShowDialogAsync(string content, string title, string statusText)
        {
            this.StatusText = statusText;
            var dialog = new MessageDialog(content, title);
            await dialog.ShowAsync();
        }

        #endregion

        #region Helper Methods

        private static async Task<WebTokenResponse> RequestTokenAsync(bool forceLogin)
        {
            // [SCENARIO] OAuth 2.0 Authorization Code Grant, Public Client
            // Get a token to authenticate against the Web API.
            var promptType = forceLogin ? WebTokenRequestPromptType.ForceAuthentication : WebTokenRequestPromptType.Default;
            var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com/", AppConfiguration.AccountProviderAuthority);
            var request = new WebTokenRequest(provider, string.Empty, AppConfiguration.TodoListWindows10ClientId, promptType);
            request.Properties.Add("resource", AppConfiguration.TodoListWebApiResourceId);
            request.Properties.Add("authority", AppConfiguration.StsAuthority);
            // Skip authority validation for AD FS, otherwise you get the following error:
            // ERROR: The value specified for 'authority' is invalid. It is not in the valid authority list or not discovered. (3399548934)
            request.Properties.Add("validateAuthority", AppConfiguration.CanValidateAuthority.ToString());

            var result = await WebAuthenticationCoreManager.RequestTokenAsync(request);
            if (result.ResponseStatus == WebTokenRequestStatus.Success)
            {
                // [NOTE] At this point we have the authentication result, including the user information.
                // From this point on we can use the regular OAuth 2.0 Bearer Token to call the Web API.
                return result.ResponseData.Single();
            }
            else if (result.ResponseStatus != WebTokenRequestStatus.UserCancel)
            {
                var errorMessage = result.ResponseError == null ? "Unknown error" : result.ResponseError.ErrorMessage;
                throw new Exception(errorMessage);
            }
            else
            {
                return null;
            }
        }

        private static async Task<IdentityInfo> GetIdentityInfoFromWebApiAsync()
        {
            // Get identity information from the Todo List Web API.
            var tokenResponse = await RequestTokenAsync(false);
            var todoListWebApiClient = GetTodoListClient(tokenResponse.Token);
            var todoListWebApiIdentityInfoRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(AppConfiguration.TodoListWebApiRootUrl + "api/identity"));
            var todoListWebApiIdentityInfoResponse = await todoListWebApiClient.SendRequestAsync(todoListWebApiIdentityInfoRequest);
            todoListWebApiIdentityInfoResponse.EnsureSuccessStatusCode();
            var todoListWebApiIdentityInfoResponseString = await todoListWebApiIdentityInfoResponse.Content.ReadAsStringAsync();
            var todoListWebApiIdentityInfo = JsonConvert.DeserializeObject<IdentityInfo>(todoListWebApiIdentityInfoResponseString);

            // Translate the authentication properties into claims.
            var displayName = default(string);
            var claims = new List<ClaimInfo>();
            foreach (var property in tokenResponse.Properties)
            {
                claims.Add(new ClaimInfo { Type = property.Key, Value = property.Value });
                if (string.Equals(property.Key, "DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    displayName = property.Value;
                }
            }
            return new IdentityInfo
            {
                Source = "WebAuthenticationCoreManager",
                Application = AppConfiguration.ApplicationName,
                IsAuthenticated = true,
                Name = displayName,
                AuthenticationType = "JWT",
                Claims = claims,
                RelatedApplicationIdentities = new[] { todoListWebApiIdentityInfo }
            };
        }

        private static HttpClient GetTodoListClient(string accessToken)
        {
            // Ignore certificate errors from the local IIS Express SSL certificate.
            // NOTE: Do not do this for production services!
            var filter = new HttpBaseProtocolFilter();
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.RevocationFailure);

            var client = new HttpClient(filter);
            client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", accessToken);
            return client;
        }

        private void ResetCommandStatus()
        {
            this.SignInCommand.RaiseCanExecuteChanged();
            this.SignOutCommand.RaiseCanExecuteChanged();
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