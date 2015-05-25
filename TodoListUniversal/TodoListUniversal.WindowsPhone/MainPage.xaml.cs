using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TodoListUniversal
{
    public sealed partial class MainPage : Page, IWebAuthenticationContinuable
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.DataContext = new MainPageViewModel();
        }

        public async Task ContinueWebAuthenticationAsync(IWebAuthenticationBrokerContinuationEventArgs args)
        {
            // A web authentication continuation was requested, delegate to the view model.
            await ((MainPageViewModel)this.DataContext).ContinueWebAuthenticationAsync(args);
        }
    }
}