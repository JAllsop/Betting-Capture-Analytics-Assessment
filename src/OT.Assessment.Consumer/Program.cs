
using MassTransit;
using Microsoft.Extensions.Configuration;
using OT.Assessment.Consumer;
using OT.Assessment.Shared;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<WagerConsumer>(cfg =>
    {
        cfg.Options<BatchOptions>(options => options
            .SetMessageLimit(200)
            .SetTimeLimit(TimeSpan.FromSeconds(5))
        );
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("messaging");
        cfg.Host(connectionString);


        cfg.ReceiveEndpoint("wager-handler", e =>
        {
            e.PrefetchCount = 400;

            e.Batch<CasinoWager>(b =>
            {
                b.MessageLimit = 200;
                b.TimeLimit = TimeSpan.FromSeconds(5);
                b.ConcurrencyLimit = 1;
            });

            e.ConfigureConsumer<WagerConsumer>(context);
        });
    });
});

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Consumer Application started {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);

// DB Initialization
try
{
    await OT.Assessment.Consumer.Infrastructure.DatabaseInitializer.InitializeDatabase(builder.Configuration, logger);
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Failed to initialize database - worker will not start");
    logger.LogInformation("Consumer Application ended {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);
    return;
}

await host.RunAsync();

logger.LogInformation("Consumer Application ended {time:yyyy-MM-dd HH:mm:ss}", DateTime.Now);