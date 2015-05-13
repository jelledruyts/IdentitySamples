using System.Windows;
using TodoListWpf.ViewModels;

namespace TodoListWpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainWindowViewModel();
        }
    }
}