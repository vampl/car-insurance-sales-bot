using CarInsuranceSalesBot.Models;
using CarInsuranceSalesBot.Options;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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
        if (update.Message is null) return;

        UserSession session = _sessionManager.GetOrCreateUserSession(update.Message.Chat.Id);

        switch (session.Step)
        {
            case 0:
                await HandleStep0(bot, session, cancellationToken);
                break;
            case 1:
                await HandleStep1(bot, update, session, cancellationToken);
                break;
            case 2:
                await HandleStep2(bot, update, session, cancellationToken);
                break;
            case 3:
                await HandleStep3(bot, update, session, cancellationToken);
                break;
            case 4:
                await HandleStep4(bot, update, session, cancellationToken);
                break;
        }
    }

    private async Task HandleStep0(
        ITelegramBotClient bot,
        UserSession session,
        CancellationToken cancellationToken)
    {
        await bot.SendMessage(
            session.UserId,
            """
            👋 Hello! 
            I’ll help you purchase car insurance.
            Please send a photo of your passport.
            """,
            cancellationToken: cancellationToken);
        session.Step++;
    }

    private async Task HandleStep1(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        if (update.Message?.Photo is null)
        {
            await bot.SendMessage(
                session.UserId,
                "📸 Please send a photo of your passport to continue.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            session.PassportImageStream =
                await bot.DownloadFile(
                    update.Message.Photo[^1].FileId,
                    cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            await bot.SendMessage(
                session.UserId,
                "⚠️ Hmm, I couldn't download the passport image. Please try sending it again.",
                cancellationToken: cancellationToken);

            return;
        }

        try
        {
            await bot.SendMessage(
                session.UserId,
                "✅ Got your passport! Scrapping required data from photo...\n",
                cancellationToken: cancellationToken);
            session.MindeeDataExtractionResponse.ExtractedPassportData =
                await _ocrService.ExtractPassportAsync(session.PassportImageStream);
        }
        catch (Exception)
        {
            await bot.SendMessage(
                session.UserId,
                "⚠️ I had trouble reading your passport. Make sure the photo is clear and all text is visible.",
                cancellationToken: cancellationToken);

            return;
        }

        await bot.SendMessage(
            session.UserId,
            "Done! Now please send a photo of your vehicle ID document.",
            cancellationToken: cancellationToken);

        session.Step++;
    }

    private async Task HandleStep2(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        if (update.Message?.Photo is null)
        {
            await bot.SendMessage(
                session.UserId,
                "📸 Please send a photo of your vehicle ID to proceed.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            session.VehicleIdImageStream =
                await bot.DownloadFile(
                    update.Message.Photo[^1].FileId,
                    cancellationToken: cancellationToken);
        }
        catch (Exception)
        {
            await bot.SendMessage(
                session.UserId,
                "⚠️ I couldn't download the vehicle ID image. Could you try sending it again?",
                cancellationToken: cancellationToken);

            return;
        }

        try
        {
            await bot.SendMessage(
                session.UserId,
                "✅ Got your vehicle ID! Scrapping required data from photo... ",
                cancellationToken: cancellationToken);
            session.MindeeDataExtractionResponse.ExtractedVehicleIdData =
                await _ocrService.ExtractVehicleIdAsync(session.VehicleIdImageStream);
        }
        catch (Exception)
        {
            await bot.SendMessage(
                session.UserId,
                "⚠️ I couldn't read the vehicle ID. Please ensure it's well-lit and all text is clear.",
                cancellationToken: cancellationToken);

            return;
        }

        await bot.SendMessage(
            session.UserId,
            "Done! There is your documents summary: ",
            cancellationToken: cancellationToken);

        string summaryText = BuildSummaryText(session.MindeeDataExtractionResponse);

        await bot.SendMessageWithKeyboard(
            session.UserId,
            summaryText,
            new KeyboardButton[][] { ["✅ Yes", "❌ No"] },
            cancellationToken: cancellationToken);

        session.Step++;
    }

    private async Task HandleStep3(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        switch (update.Message?.Text)
        {
            case "✅ Yes":
                await bot.SendMessageWithKeyboard(
                    session.UserId,
                    "The insurance price is 100 USD.\nDo you confirm?",
                    new KeyboardButton[][] { ["✅ Yes", "❌ No"] },
                    cancellationToken: cancellationToken);
                session.Step++;
                break;

            case "❌ No":
                await bot.SendMessage(
                    session.UserId,
                    "Let's try again. Send photo of a passport",
                    cancellationToken: cancellationToken);
                session.Step = 1;
                break;
        }
    }

    private async Task HandleStep4(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        switch (update.Message?.Text)
        {
            case "✅ Yes":
                MemoryStream policyStream =
                    _pdfPolicyGenerationService.GeneratePolicy(session.MindeeDataExtractionResponse);
                await bot.SendDocument(
                    session.UserId,
                    policyStream,
                    "insurance_policy.pdf",
                    "Congratulation, there is your insurance policy",
                    cancellationToken: cancellationToken);
                session.Step = 0;
                break;

            case "❌ No":
                await bot.SendMessage(
                    session.UserId,
                    "Unfortunately, the price is fixed at 100 USD.",
                    cancellationToken: cancellationToken);
                session.Step = 0;
                break;
        }
    }

    private static string BuildSummaryText(MindeeDataExtractionResponse data)
    {
        MindeeDataExtractionResponse.Passport passport = data.ExtractedPassportData;
        MindeeDataExtractionResponse.VehicleId vehicleId = data.ExtractedVehicleIdData;

        return $"""
                Here’s what I found
                📄 Passport Information:
                👤 Full Name: {passport.Surname.Value} {passport.Name.Value} {passport.Patronymic.Value}
                🆔 Record No: {passport.RecordNo.Value}
                👫 Sex: {passport.Sex.Value}
                🎂 Date of Birth: {passport.DateOfBirth.Value}
                📅 Issued On: {passport.DateOfExpiry.Value}
                🌍 Nationality: {passport.Nationality.Value}

                🚗 Vehicle Information:
                🔢 Reg Number: {vehicleId.RegistrationNumber.Value}
                📅 First Registration: {vehicleId.DateOfFirstRegistration.Value}
                📅 Ukraine Registration: {vehicleId.DateOfFirstRegistrationInUkraine.Value}
                🏷️ Make & Model: {vehicleId.Make.Value} {vehicleId.CommercialDescription.Value}
                📌 Type: {vehicleId.Type.Value}
                🎨 Color: {vehicleId.ColorOfVehicle.Value}

                Do you confirm?
                """;
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
