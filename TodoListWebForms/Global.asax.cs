using System;
using System.Web;
using System.Web.Optimization;

namespace TodoListWebForms
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}