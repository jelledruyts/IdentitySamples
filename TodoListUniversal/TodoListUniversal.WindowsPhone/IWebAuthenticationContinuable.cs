using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace TodoListUniversal
{
    // [NOTE] This interface is used from within App.OnActivated to continue web authentication.
    public interface IWebAuthenticationContinuable
    {
        Task ContinueWebAuthenticationAsync(IWebAuthenticationBrokerContinuationEventArgs args);
    }
}