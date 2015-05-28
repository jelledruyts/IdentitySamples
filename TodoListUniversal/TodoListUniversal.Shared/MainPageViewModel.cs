using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Popups;

namespace TodoListUniversal
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        #region Fields

        private AuthenticationContext context;
        private UserInfo userInfo;

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
            var redirectUri = Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

            this.SignInCommand = new AsyncRelayCommand(SignIn, CanSignIn);
            this.SignOutCommand = new AsyncRelayCommand(SignOut, CanSignOut);
#if WINDOWS_PHONE_APP
            // [NOTE] Windows Phone uses an async factory pattern to create the AuthenticationContext.
            this.context = AuthenticationContext.CreateAsync(AppConfiguration.AadAuthority).GetResults();
#else
            this.context = new AuthenticationContext(AppConfiguration.AadAuthority);
#endif
        }

        #endregion

        #region SignIn Command

        private bool CanSignIn(object argument)
        {
            return this.userInfo == null;
        }

        private async Task SignIn(object argument)
        {
            var exception = default(Exception);
            try
            {
                this.StatusText = "Signing in...";
                await SignInAsync();
            }
            catch (Exception exc)
            {
                exception = exc;
            }
            await ShowException(exception);
        }

        #endregion

        #region SignOut Command

        private bool CanSignOut(object argument)
        {
            return this.userInfo != null;
        }

        private async Task SignOut(object argument)
        {
            var exception = default(Exception);
            try
            {
                // [NOTE] Clear the authentication token cache to sign a user out.
                // This ensures the persistent tokens are cleared from the device.
                this.context.TokenCache.Clear();
                
                this.userInfo = null;

                this.StatusText = "Signed out.";
                this.SignInCommand.RaiseCanExecuteChanged();
                this.SignOutCommand.RaiseCanExecuteChanged();
            }
            catch (Exception exc)
            {
                exception = exc;
            }
            await ShowException(exception);
        }

        #endregion

        #region ShowException

        private async Task ShowException(Exception exc)
        {
            if (exc != null)
            {
                var dialog = new MessageDialog(exc.ToString(), "An error occurred...");
                await dialog.ShowAsync();
                this.StatusText = "An error occurred: " + exc.Message;
            }
        }

        #endregion

        #region Web API Communication

        private async Task SignInAsync()
        {
            // [SCENARIO] OAuth 2.0 Authorization Code Grant, Public Client
            // Get a token to authenticate against the Web API.
#if WINDOWS_PHONE_APP
            // Check if there is a cached token first.
            var result = await context.AcquireTokenSilentAsync(AppConfiguration.TodoListWebApiResourceId, AppConfiguration.TodoListWindowsPhoneClientId);
            if (result != null && result.Status == AuthenticationStatus.Success)
            {
                // There is a cached token, complete authentication immediately.
                FinalizeAuthentication(result);
            }
            else
            {
                // There is no cached token, call the web authentication broker to start the authentication process.
                context.AcquireTokenAndContinue(AppConfiguration.TodoListWebApiResourceId, AppConfiguration.TodoListWindowsPhoneClientId, new Uri(AppConfiguration.TodoListWindowsPhoneRedirectUrl), FinalizeAuthentication);
            }
#else
            var result = await this.context.AcquireTokenAsync(AppConfiguration.TodoListWebApiResourceId, AppConfiguration.TodoListWindowsClientId, new Uri(AppConfiguration.TodoListWindowsRedirectUrl));
            FinalizeAuthentication(result);
#endif
        }

#if WINDOWS_PHONE_APP
        public async Task ContinueWebAuthenticationAsync(IWebAuthenticationBrokerContinuationEventArgs args)
        {
            // A web authentication continuation was requested, signal ADAL to continue and call the FinalizeAuthentication method.
            await context.ContinueAcquireTokenAsync(args);
        }
#endif

        private void FinalizeAuthentication(AuthenticationResult result)
        {
            // [NOTE] At this point we have the authentication result, including the user information.
            // From this point on we could use the regular OAuth 2.0 Bearer Token to call the Web API.
            this.userInfo = result.UserInfo;
            this.StatusText = "Signed in as " + this.userInfo.DisplayableId;
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