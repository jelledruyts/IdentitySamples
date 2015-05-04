using Common;

namespace TodoListWebApp.Models
{
    public class AccountIndexViewModel
    {
        public IdentityInfo IdentityInfo { get; private set; }

        public AccountIndexViewModel(IdentityInfo identityInfo)
        {
            this.IdentityInfo = identityInfo;
        }
    }
}