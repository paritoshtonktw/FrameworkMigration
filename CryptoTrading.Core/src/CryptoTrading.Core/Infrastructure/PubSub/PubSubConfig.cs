using System;
using Microsoft.Extensions.Configuration;

namespace CryptoTrading.Infrastructure.PubSub;

public class PubSubConfig
{
    public bool Enabled { get; set; }
    public string ProjectId { get; set; } = null!;
    public string EmulatorHost { get; set; } = null!;
    public string OrdersIncomingTopic { get; set; } = null!;
    public string OrdersExecutedTopic { get; set; } = null!;
    public string MarketTicksTopic { get; set; } = null!;
    public string OrderPushSubscription { get; set; } = null!;
    public string TickSubscription { get; set; } = null!;
    public string? PushEndpointSecret { get; set; }
    public bool TickSubscriberEnabled { get; set; }

    public static PubSubConfig FromConfiguration(IConfiguration configuration)
    {
        var config = new PubSubConfig();
        var section = configuration.GetSection("PubSub");

        config.Enabled = section.GetValue<bool>("Enabled", false);
        config.ProjectId = section.GetValue<string>("ProjectId") ?? "cryptotrading-gcp-dev";
        config.EmulatorHost = section.GetValue<string>("EmulatorHost") ?? "localhost:8085";
        config.OrdersIncomingTopic = section.GetValue<string>("OrdersIncomingTopic") ?? "crypto-orders-incoming";
        config.OrdersExecutedTopic = section.GetValue<string>("OrdersExecutedTopic") ?? "crypto-orders-executed";
        config.MarketTicksTopic = section.GetValue<string>("MarketTicksTopic") ?? "crypto-market-ticks";
        config.OrderPushSubscription = section.GetValue<string>("OrderPushSubscription") ?? "crypto-orders-incoming-sub";
        config.TickSubscription = section.GetValue<string>("TickSubscription") ?? "crypto-market-ticks-sub";
        config.PushEndpointSecret = section.GetValue<string>("PushEndpointSecret");
        config.TickSubscriberEnabled = section.GetValue<bool>("TickSubscriberEnabled", true);

        return config;
    }
}
