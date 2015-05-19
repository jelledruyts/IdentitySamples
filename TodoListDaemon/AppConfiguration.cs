using Common;
using System.Configuration;

namespace TodoListDaemon
{
    public static class AppConfiguration
    {
        public const string ApplicationName = "Todo List (Daemon)";

        public static readonly string TodoListDaemonClientId = ConfigurationManager.AppSettings["TodoListDaemonClientId"];
        public static readonly string TodoListDaemonCertificateName = ConfigurationManager.AppSettings["TodoListDaemonCertificateName"];
        public static readonly string TodoListWebApiRootUrl = ConfigurationManager.AppSettings["TodoListWebApiRootUrl"];
        public static readonly string TodoListWebApiResourceId = ConfigurationManager.AppSettings["TodoListWebApiResourceId"];

        public static readonly string AadTenant = ConfigurationManager.AppSettings["AadTenant"];
        public static readonly string AadAuthority = Constants.AadEndpoint + AadTenant;
    }
}