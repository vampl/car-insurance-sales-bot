using CarInsuranceSalesBot.Models;
using CarInsuranceSalesBot.Options;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace CarInsuranceSalesBot.Services;

public class BotService
{
    private readonly ITelegramBotClient _botClient;

    private readonly UserSessionManager _sessionManager;
    private readonly MindeeOcrService _ocrService;
    private readonly PdfPolicyGenerationService _pdfPolicyGenerationService;

    public BotService(
        TelegramBotOptions options,
        UserSessionManager sessionManager,
        MindeeOcrService ocrService,
        PdfPolicyGenerationService pdfPolicyGenerationService,
        CancellationToken cancellationToken)
    {
        _sessionManager = sessionManager;
        _ocrService = ocrService;
        _pdfPolicyGenerationService = pdfPolicyGenerationService;

        _botClient = new TelegramBotClient(token: options.BotToken, cancellationToken: cancellationToken);
    }

    public async Task StartAsync()
    {
        // looking for initialized bot information
        User botInfo = await _botClient.GetMe();
        Console.WriteLine(value: $"Bot started as @{botInfo.Username} with {botInfo.Id}");

        // setting handlers for update and exception flows
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: ErrorHandlerAsync);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is null)
            return;

        UserSession session = _sessionManager.GetOrCreateUserSession(update.Message.Chat.Id);

        switch (session.Step)
        {
            // welcome & passport retrieve request
            case 0:
                await bot.SendMessageAsync(
                    session.UserId,
                    """
                    👋 Hello! 
                    I’ll help you purchase car insurance.
                    Please send a photo of your passport.
                    """,
                    cancellationToken: cancellationToken);

                session.Step++;
                break;
            // passport process & vehicle id retrieve request
            case 1 when update.Message.Photo is not null:

                session.PassportImageStream =
                    await bot.DownloadTelegramFileAsync(
                        update.Message.Photo[^1].FileId,
                        cancellationToken: cancellationToken);
                session.MindeeDataExtractionResponse.ExtractedPassportData =
                    await _ocrService.ExtractPassportAsync(session.PassportImageStream);

                await bot.SendMessage(
                    chatId: session.UserId,
                    text:
                    """
                    Got your passport ✅
                    Now send your vehicle ID document.
                    """,
                    cancellationToken: cancellationToken);

                session.Step++;

                break;
            case 1 when update.Message.Photo is null:
                await bot.SendMessageAsync(
                    session.UserId,
                    "Please send a photo of your passport",
                    cancellationToken: cancellationToken);

                break;
            // vehicle id process & review extracted data
            case 2 when update.Message.Photo is not null:
                session.VehicleIdImageStream =
                    await bot.DownloadTelegramFileAsync(
                        update.Message.Photo[^1].FileId,
                        cancellationToken: cancellationToken);
                session.MindeeDataExtractionResponse.ExtractedVehicleIdData =
                    await _ocrService.ExtractVehicleIdAsync(session.VehicleIdImageStream);

                await bot.SendMessage(
                    chatId: session.UserId,
                    text:
                    """
                    Got your vehicle ID ✅
                    Wait a second to process your documents
                    """,
                    cancellationToken: cancellationToken);

                await bot.SendKeyboardAsync(
                    chatId: session.UserId,
                    text:
                    $"""
                     Here’s what I found
                     📄 Passport Information:
                     👤 Full Name: {session.MindeeDataExtractionResponse.ExtractedPassportData.Surname.Value} {
                         session.MindeeDataExtractionResponse.ExtractedPassportData.Name.Value} {
                             session.MindeeDataExtractionResponse.ExtractedPassportData.Patronymic.Value}
                     🆔 Record No: {session.MindeeDataExtractionResponse.ExtractedPassportData.RecordNo.Value}
                     👫 Sex: {session.MindeeDataExtractionResponse.ExtractedPassportData.Sex.Value}
                     🎂 Date of Birth: {session.MindeeDataExtractionResponse.ExtractedPassportData.DateOfBirth.Value
                     }
                     📅 Issued On: {session.MindeeDataExtractionResponse.ExtractedPassportData.DateOfExpiry.Value}
                     🌍 Nationality: {session.MindeeDataExtractionResponse.ExtractedPassportData.Nationality.Value}

                     🚗 Vehicle Information:
                     🔢 Reg Number: {session.MindeeDataExtractionResponse.ExtractedVehicleIdData.RegistrationNumber
                         .Value}
                     📅 First Registration: {
                         session.MindeeDataExtractionResponse.ExtractedVehicleIdData.DateOfFirstRegistration.Value}
                     📅 Ukraine Registration: {session.MindeeDataExtractionResponse.ExtractedVehicleIdData
                         .DateOfFirstRegistrationInUkraine.Value}
                     🏷️ Make & Model: {session.MindeeDataExtractionResponse.ExtractedVehicleIdData.Make.Value} {
                         session.MindeeDataExtractionResponse.ExtractedVehicleIdData.CommercialDescription.Value}
                     📌 Type: {session.MindeeDataExtractionResponse.ExtractedVehicleIdData.Type.Value}
                     🎨 Color: {session.MindeeDataExtractionResponse.ExtractedVehicleIdData.ColorOfVehicle.Value}

                     Do you confirm?
                     """,
                    keyboard: [["✅ Yes", "❌ No"]],
                    cancellationToken: cancellationToken);

                session.Step++;
                break;
            case 2 when update.Message.Photo is null:
                await bot.SendMessageAsync(
                    session.UserId,
                    "Please send a photo of vehicle ID",
                    cancellationToken: cancellationToken);
                break;
            // payment agreement
            case 3 when update.Message.Text == "✅ Yes":
                await bot.SendKeyboardAsync(
                    chatId: session.UserId,
                    text: """
                          The insurance price is 100 USD.
                          Do you confirm?
                          """,
                    keyboard: [["✅ Yes", "❌ No"]],
                    cancellationToken: cancellationToken);

                session.Step++;

                break;
            case 3 when update.Message.Text == "❌ No":
                await bot.SendMessageAsync(
                    chatId: session.UserId,
                    text: "Let's try again. Send photo of a passport",
                    cancellationToken: cancellationToken);

                session.Step = 1;
                break;
            // insurance file generation
            case 4 when update.Message.Text == "✅ Yes":
                MemoryStream policyStream =
                    _pdfPolicyGenerationService.GeneratePolicy(session.MindeeDataExtractionResponse);
                await bot.SendDocumentAsync(
                    chatId: session.UserId,
                    file: policyStream,
                    filename: "insurance_policy.pdf",
                    caption: "Congratulation, there is your insurance policy",
                    cancellationToken: cancellationToken);
                break;
            case 4 when update.Message.Text == "❌ No":
                await bot.SendMessage(
                    chatId: session.UserId,
                    text: "Unfortunately, the price is fixed at 100 USD.",
                    cancellationToken: cancellationToken);

                session.Step = 0;

                break;
        }
    }

    private Task ErrorHandlerAsync(
        ITelegramBotClient bot,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Console.WriteLine(
            $"""
             [{DateTimeOffset.Now:dd.MM.yyyy HH:mm:ss}]: {exception.Message}

             {exception.StackTrace}
             """);

        return Task.CompletedTask;
    }
}
