using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
using Windows.UI.Popups;

namespace TodoListUniversalWindows10
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        #region Fields

        private WebAccount account;

        #endregion

        #region Properties

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
            var callbackUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().ToString();
            var applicationSid = callbackUri.Replace("ms-app://", string.Empty).TrimEnd('/').ToUpperInvariant();
            var redirectUri = "ms-appx-web://Microsoft.AAD.BrokerPlugin/" + applicationSid;

            this.SignInCommand = new AsyncRelayCommand(SignIn, CanSignIn);
            this.SignOutCommand = new AsyncRelayCommand(SignOut, CanSignOut);
        }

        #endregion

        #region SignIn Command

        private bool CanSignIn(object argument)
        {
            return this.account == null;
        }

        private async Task SignIn(object argument)
        {
            try
            {
                this.StatusText = "Signing in...";

                var authority = AppConfiguration.AadAuthority; // [NOTE] Use "organizations" for *any* Azure AD tenant, or "consumers" for a Microsoft Account.
                var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", authority);
                var request = new WebTokenRequest(provider, string.Empty, AppConfiguration.TodoListWindows10ClientId);
                request.Properties.Add("resource", AppConfiguration.TodoListWebApiResourceId);

                var result = await WebAuthenticationCoreManager.RequestTokenAsync(request);
                if (result.ResponseStatus == WebTokenRequestStatus.Success)
                {
                    // [NOTE] At this point we have the authentication result, including the user information.
                    // From this point on we could use the regular OAuth 2.0 Bearer Token to call the Web API.
                    var responseData = result.ResponseData.Single();
                    this.account = responseData.WebAccount;

                    // The responseData.Token contains the access token.
                    // The responseData.Properties contains the following keys:
                    // - UPN => User Principal Name
                    // - DisplayName => User's actual display name
                    // - TenantId => Tenant ID (GUID)
                    // - OID => Unique Object ID (GUID)
                    // - Authority => AAD tenant URL
                    // - SignInName => same as UPN
                    // - UID => Unique ID (string)

                    this.StatusText = "Signed in as " + this.account.UserName;
                    this.SignInCommand.RaiseCanExecuteChanged();
                    this.SignOutCommand.RaiseCanExecuteChanged();
                }
                else if (result.ResponseStatus != WebTokenRequestStatus.UserCancel)
                {
                    var errorMessage = result.ResponseError == null ? "Unknown error" : result.ResponseError.ErrorMessage;
                    await ShowDialog(errorMessage, "An error occurred...", "An error occurred: " + errorMessage);
                }
            }
            catch (Exception exc)
            {
                await ShowException(exc);
            }
        }

        #endregion

        #region SignOut Command

        private bool CanSignOut(object argument)
        {
            return this.account != null;
        }

        private async Task SignOut(object argument)
        {
            try
            {
                this.StatusText = "Signing out...";
                this.SignInCommand.RaiseCanExecuteChanged();
                this.SignOutCommand.RaiseCanExecuteChanged();

                await this.account.SignOutAsync();
                this.account = null;

                this.StatusText = "Signed out.";
                this.SignInCommand.RaiseCanExecuteChanged();
                this.SignOutCommand.RaiseCanExecuteChanged();
            }
            catch (Exception exc)
            {
                await ShowException(exc);
            }
        }

        #endregion

        #region Dialogs

        private async Task ShowException(Exception exc)
        {
            if (exc != null)
            {
                await ShowDialog(exc.ToString(), "An error occurred...", "An error occurred: " + exc.Message);
            }
        }

        private async Task ShowDialog(string content, string title, string statusText)
        {
            this.StatusText = statusText;
            var dialog = new MessageDialog(content, title);
            await dialog.ShowAsync();
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