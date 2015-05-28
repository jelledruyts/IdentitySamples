using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace TodoListUniversal
{
    // [NOTE] This interface is used from within App.OnActivated to continue web authentication
    // when the Web Authentication Broker on Windows Phone has finished the authentication process.
    public interface IWebAuthenticationContinuable
    {
        Task ContinueWebAuthenticationAsync(IWebAuthenticationBrokerContinuationEventArgs args);
    }
}