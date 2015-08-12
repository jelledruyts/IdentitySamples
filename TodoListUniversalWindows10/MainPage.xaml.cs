using Windows.UI.Xaml.Controls;

namespace TodoListUniversalWindows10
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = new MainPageViewModel();
        }
    }
}