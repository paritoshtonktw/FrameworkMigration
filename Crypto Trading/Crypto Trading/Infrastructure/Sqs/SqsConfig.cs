using System;
using System.Configuration;

namespace CryptoTrading.Infrastructure.Sqs
{
    /// <summary>
    /// Configuration for AWS SQS messaging.
    /// Follows the 12-Factor App pattern: environment variables override Web.config settings.
    /// </summary>
    public class SqsConfig
    {
        public bool Enabled { get; set; }
        public string AwsRegion { get; set; }
        public string ServiceUrl { get; set; }
        public string OrdersIncomingQueueUrl { get; set; }
        public string OrdersExecutedQueueUrl { get; set; }
        public string MarketTicksQueueUrl { get; set; }
        public string WebhookSecret { get; set; }
        public bool TickSubscriberEnabled { get; set; }

        public static SqsConfig FromConfiguration()
        {
            var config = new SqsConfig();

            var enabledEnv = Environment.GetEnvironmentVariable("SQS_ENABLED");
            var enabledApp = ConfigurationManager.AppSettings["Sqs:Enabled"];
            config.Enabled = !string.IsNullOrEmpty(enabledEnv)
                ? (bool.TryParse(enabledEnv, out var b) ? b : true)
                : (!string.IsNullOrEmpty(enabledApp) && (bool.TryParse(enabledApp, out var b2) ? b2 : true));

            config.AwsRegion = Environment.GetEnvironmentVariable("SQS_AWS_REGION")
                ?? Environment.GetEnvironmentVariable("AWS_REGION")
                ?? ConfigurationManager.AppSettings["Sqs:AwsRegion"]
                ?? "us-east-1";

            config.ServiceUrl = Environment.GetEnvironmentVariable("SQS_SERVICE_URL")
                ?? ConfigurationManager.AppSettings["Sqs:ServiceUrl"]
                ?? "http://localhost:4566";

            config.OrdersIncomingQueueUrl = Environment.GetEnvironmentVariable("SQS_ORDERS_INCOMING_QUEUE_URL")
                ?? ConfigurationManager.AppSettings["Sqs:OrdersIncomingQueueUrl"]
                ?? "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-orders-incoming.fifo";

            config.OrdersExecutedQueueUrl = Environment.GetEnvironmentVariable("SQS_ORDERS_EXECUTED_QUEUE_URL")
                ?? ConfigurationManager.AppSettings["Sqs:OrdersExecutedQueueUrl"]
                ?? "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-orders-executed.fifo";

            config.MarketTicksQueueUrl = Environment.GetEnvironmentVariable("SQS_MARKET_TICKS_QUEUE_URL")
                ?? ConfigurationManager.AppSettings["Sqs:MarketTicksQueueUrl"]
                ?? "https://sqs.us-east-1.amazonaws.com/123456789012/crypto-market-ticks.fifo";

            config.WebhookSecret = Environment.GetEnvironmentVariable("SQS_WEBHOOK_SECRET")
                ?? ConfigurationManager.AppSettings["Sqs:WebhookSecret"];

            var tickSubEnv = Environment.GetEnvironmentVariable("SQS_TICK_SUBSCRIBER_ENABLED");
            var tickSubApp = ConfigurationManager.AppSettings["Sqs:TickSubscriberEnabled"];
            config.TickSubscriberEnabled = !string.IsNullOrEmpty(tickSubEnv)
                ? (bool.TryParse(tickSubEnv, out var ts) ? ts : true)
                : (!string.IsNullOrEmpty(tickSubApp) ? (bool.TryParse(tickSubApp, out var ts2) ? ts2 : true) : true);

            return config;
        }
    }
}
