using System;
using System.Web;
using System.Web.Http;
using log4net.Config;

namespace CryptoTrading.Web
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            XmlConfigurator.Configure();
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // Initialize and start AWS SQS streaming workers
            try
            {
                DependencyConfig.SqsManager.Start();
            }
            catch (Exception ex)
            {
                DependencyConfig.Logger?.Error($"Failed to start AWS SQS services: {ex.Message}", ex);
            }
        }

        protected void Application_End()
        {
            try
            {
                DependencyConfig.SqsManager.Stop();
            }
            catch (Exception ex)
            {
                DependencyConfig.Logger?.Error($"Failed to stop AWS SQS services: {ex.Message}", ex);
            }
        }
    }
}

