using LeetPOTD.Configuration;
using LeetPOTD.Workers;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<Timezone>(
    builder.Configuration.GetSection("Bot"));

builder.Services.AddHostedService<MainWorker>();

var host = builder.Build();

await host.RunAsync();