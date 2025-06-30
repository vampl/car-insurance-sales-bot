using CarInsuranceSalesBot.Options;
using CarInsuranceSalesBot.Services;

using Microsoft.Extensions.Configuration;

// configuration handler setup
IConfiguration configuration =
    new ConfigurationBuilder()
        .SetBasePath(basePath: Directory.GetCurrentDirectory())
        .AddEnvironmentVariables()
        .Build();

// setup services options
TelegramBotOptions telegramBotOptions =
    new()
    {
        BotToken =
            configuration["TelegramBotToken"] ??
            throw new InvalidOperationException(message: "Telegram bot token is missing")
    };
MindeeOptions mindeeOptions =
    new()
    {
        ApiKey =
            configuration["Mindee:ApiKey"] ??
            throw new InvalidOperationException(message: "Mindee API key is missing")
    };

// service registration
CancellationTokenSource cts = new();

UserSessionManager sessionManager = new();
MindeeOcrService ocrService = new(mindeeOptions);
PdfPolicyGenerationService pdfPolicyGenerationService = new();

BotService service =
    new(
        telegramBotOptions,
        sessionManager,
        ocrService,
        pdfPolicyGenerationService,
        cancellationToken: cts.Token);

// start application core service
await service.StartAsync();

await Task.Delay(Timeout.Infinite);
