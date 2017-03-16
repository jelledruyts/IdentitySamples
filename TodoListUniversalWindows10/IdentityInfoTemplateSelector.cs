using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace TodoListUniversalWindows10
{
    // This allows recursive template bindings.
    public class IdentityInfoTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return (DataTemplate)App.Current.Resources["IdentityInfoTemplate"];
        }
    }
}