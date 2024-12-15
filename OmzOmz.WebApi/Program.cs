using Microsoft.Extensions.Options;
using OmzOmz.WebApi.Context;
using OmzOmz.WebApi.Options;
using OmzOmz.WebApi.Services;
using Polly;
using Polly.Retry;
using Telegram.Bot;
using Telegram.Bot.Polling;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSqlite<AppDbContext>(builder.Configuration.GetConnectionString("Sqlite"));

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

builder.Services.ConfigureTelegramBot<Microsoft.AspNetCore.Http.Json.JsonOptions>(opt => opt.SerializerOptions);

builder.Services.AddOptions<TelegramOptions>()
    .Bind(builder.Configuration.GetSection("Telegram"));

builder.Services.AddResiliencePipeline("Telegram",
    pipeline =>
    {
        pipeline.AddRetry(new RetryStrategyOptions
        {
            Delay = TimeSpan.FromSeconds(10)
        });
    });

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient(nameof(TelegramBotClient))
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        new TelegramBotClient(
            sp.GetRequiredService<IOptions<TelegramOptions>>().Value.Token,
            httpClient
        )
    );

builder.Services.AddScoped<IUpdateHandler, TelegramUpdateHandlerService>();

builder.Services.AddHostedService<TelegramPollingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

await using var scope = app.Services.CreateAsyncScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await dbContext.Database.EnsureCreatedAsync();

app.Run();