using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Web;
using System.Web.Mvc;

namespace TodoListWebApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
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
        }

        [AllowAnonymous]
        public ActionResult SignedOut()
        {
            return View();
        }
    }
}