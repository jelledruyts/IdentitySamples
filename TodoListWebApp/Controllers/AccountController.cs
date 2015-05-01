using Common;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using TodoListWebApp.Infrastructure;

namespace TodoListWebApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                // Send an OpenID Connect sign-in request.
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
                return new EmptyResult(); // The challenge will take care of the response.
            }
            else
            {
                // Redirect to the home page after signing in.
                return RedirectToAction("Index", "Home");
            }
        }

        public void SignOut()
        {
            // Remove all cache entries for this user and send an OpenID Connect sign-out request.
            HttpContext.GetOwinContext().Authentication.SignOut(OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);

            var userId = ClaimsPrincipal.Current.GetUniqueIdentifier();
            var tokenCache = TokenCacheFactory.Instance;
            foreach (var userToken in tokenCache.ReadItems().Where(t => t.UniqueId == userId))
            {
                tokenCache.DeleteItem(userToken);
            }

            Session.Abandon();
        }

        [AllowAnonymous]
        public ActionResult SignedOut()
        {
            return View();
        }
    }
}