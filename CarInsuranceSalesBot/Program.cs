using CarInsuranceSalesBot.Options;

using Microsoft.Extensions.Configuration;

// configuration handler setup
IConfiguration configuration =
    new ConfigurationBuilder()
        .SetBasePath(basePath: Directory.GetCurrentDirectory())
        .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
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
            configuration["MindeeOptions:ApiKey"] ??
            throw new InvalidOperationException(message: "Mindee API key is missing")
    };
    