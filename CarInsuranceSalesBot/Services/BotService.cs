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

    private static readonly HashSet<string> YesAnswers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "yes", "✅ yes", "y", "ok", "sure", "confirm", "confirmed", "👍", "да", "oui"
        };

    private static readonly HashSet<string> NoAnswers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "no", "❌ no", "n", "cancel", "not sure", "nope", "✖", "нет", "non"
        };


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

        string? messageText = update.Message.Text?.ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(messageText) && IsGeneralQuestion(messageText))
        {
            await HandleGeneralQuestion(bot, update.Message.Chat.Id, messageText, cancellationToken);
            return;
        }

        UserSession session = _sessionManager.GetOrCreateUserSession(update.Message.Chat.Id);

        switch (session.Step)
        {
            case 0:
                await HandleStartStep0(bot, session, cancellationToken);
                break;
            case 1:
                await HandlePassportObtainingStep1(bot, update, session, cancellationToken);
                break;
            case 2:
                await HandlePassportConfirmationStep2(bot, update, session, cancellationToken);
                break;
            case 3:
                await HandleVehicleIdObtainingStep3(bot, update, session, cancellationToken);
                break;
            case 4:
                await HandleVehicleIdConfirmationStep4(bot, update, session, cancellationToken);
                break;
            case 5:
                await HandleSummaryConfirmationStep5(bot, update, session, cancellationToken);
                break;
            case 6:
                await HandleFinalConfirmationStep6(bot, update, session, cancellationToken);
                break;
        }
    }

    private static bool IsGeneralQuestion(string text)
    {
        return text.Contains("your purpose") ||
               text.Contains("what can i do") ||
               text.Contains("my data") ||
               text.Contains("who are you") ||
               text.Contains("are you safe") ||
               text.Contains("data safe") ||
               text.Contains("privacy") ||
               text.Contains("help");
    }

    private async Task HandleGeneralQuestion(
        ITelegramBotClient bot,
        long chatId,
        string question,
        CancellationToken cancellationToken)
    {
        string response;

        if (question.Contains("your purpose") || question.Contains("who are you"))
        {
            response =
                "🤖 I'm an assistant that helps you get car insurance by processing your passport and vehicle documents.";
        }
        else if (question.Contains("my data") || question.Contains("privacy") || question.Contains("data safe"))
        {
            response =
                "🔒 Your data is processed only to generate the insurance policy. It is not shared or stored permanently.";
        }
        else if (question.Contains("what can i do") || question.Contains("help"))
        {
            response =
                "📝 You can use this bot to generate a car insurance policy. Just send your passport photo and vehicle ID.";
        }
        else
        {
            response = "ℹ️ I'm here to help you get car insurance. Please send your passport to get started.";
        }

        await bot.SendMessage(chatId, response, cancellationToken: cancellationToken);
    }

    private async Task HandleStartStep0(
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

    private async Task HandlePassportObtainingStep1(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        if (update.Message?.Photo is null)
        {
            await bot.SendMessage(
                session.UserId,
                "📸 Please send a photo of your passport.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            session.PassportImageStream =
                await bot.DownloadFile(
                    update.Message.Photo[^1].FileId,
                    cancellationToken: cancellationToken);

            await bot.SendMessage(
                session.UserId,
                "✅ Got your passport! Reading data, wait a minute",
                cancellationToken: cancellationToken);
            session.MindeeDataExtractionResponse.ExtractedPassportData =
                await _ocrService.ExtractPassportAsync(session.PassportImageStream);

            MindeeDataExtractionResponse.Passport passport = session.MindeeDataExtractionResponse.ExtractedPassportData;
            string text =
                $"""
                 📄 Passport Information:
                 👤 Name: {passport.Surname.Value} {passport.Name.Value} {passport.Patronymic.Value}
                 🆔 Record No: {passport.RecordNo.Value}
                 🎂 DOB: {passport.DateOfBirth.Value}
                 👫 Sex: {passport.Sex.Value}
                 📅 Issued: {passport.DateOfExpiry.Value}
                 🌍 Nationality: {passport.Nationality.Value}

                 Is this correct?
                 """;

            await bot.SendMessageWithKeyboard(
                session.UserId,
                text,
                [[new KeyboardButton("✅ Yes"), new KeyboardButton("❌ No")]],
                cancellationToken);
            session.Step++;
        }
        catch
        {
            await bot.SendMessage(
                session.UserId,
                "⚠️ I had trouble reading the passport. Try again.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandlePassportConfirmationStep2(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        string? text = update.Message?.Text?.Trim();

        if (text == null)
            return;

        if (YesAnswers.Contains(text))
        {
            await bot.SendMessage(
                session.UserId,
                "Now please send a photo of your vehicle ID document.",
                cancellationToken: cancellationToken);
            session.Step++;
        }
        else if (NoAnswers.Contains(text))
        {
            await bot.SendMessage(
                session.UserId,
                "Please send your passport photo again.",
                cancellationToken: cancellationToken);
            session.Step = 1;
        }
        else
        {
            await bot.SendMessage(
                session.UserId,
                "Please confirm passport data by replying ✅ Yes or ❌ No.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleVehicleIdObtainingStep3(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        if (update.Message?.Photo is null)
        {
            await bot.SendMessage(
                session.UserId,
                "📸 Please send a photo of your vehicle ID.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            session.VehicleIdImageStream = await bot.DownloadFile(update.Message.Photo[^1].FileId, cancellationToken);
            await bot.SendMessage(
                session.UserId,
                "✅ Got your vehicle ID! Reading data, wait a minute",
                cancellationToken: cancellationToken);

            session.MindeeDataExtractionResponse.ExtractedVehicleIdData =
                await _ocrService.ExtractVehicleIdAsync(session.VehicleIdImageStream);

            MindeeDataExtractionResponse.VehicleId
                vehicle = session.MindeeDataExtractionResponse.ExtractedVehicleIdData;
            string text =
                $"""
                 🚗 Vehicle Information:
                 🔢 Reg Number: {vehicle.RegistrationNumber.Value}
                 📅 First Registration: {vehicle.DateOfFirstRegistration.Value}
                 📅 Ukraine Registration: {vehicle.DateOfFirstRegistrationInUkraine.Value}
                 🏷️ Make & Model: {vehicle.Make.Value} {vehicle.CommercialDescription.Value}
                 📌 Type: {vehicle.Type.Value}
                 🎨 Color: {vehicle.ColorOfVehicle.Value}

                 Is this correct?
                 """;

            await bot.SendMessageWithKeyboard(
                session.UserId,
                text,
                [[new KeyboardButton("✅ Yes"), new KeyboardButton("❌ No")]],
                cancellationToken: cancellationToken);
            session.Step++;
        }
        catch
        {
            await bot.SendMessage(
                session.UserId,
                "⚠️ Couldn't read vehicle ID. Try again.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleVehicleIdConfirmationStep4(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        string? text = update.Message?.Text?.Trim();

        if (text == null)
            return;

        if (YesAnswers.Contains(text))
        {
            await bot.SendMessage(
                session.UserId,
                "Great! Here is a summary of your data:",
                cancellationToken: cancellationToken);

            string summary = BuildSummaryText(session.MindeeDataExtractionResponse);
            await bot.SendMessage(
                session.UserId,
                summary,
                cancellationToken: cancellationToken);

            session.Step++;
        }
        else if (NoAnswers.Contains(text))
        {
            await bot.SendMessage(
                session.UserId,
                "Please resend your vehicle ID photo.",
                cancellationToken: cancellationToken);
            session.Step = 3;
        }
        else
        {
            await bot.SendMessage(
                session.UserId,
                "Please confirm with ✅ Yes or ❌ No.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleSummaryConfirmationStep5(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        string? text = update.Message?.Text?.Trim();

        if (text == null)
            return;

        if (YesAnswers.Contains(text))
        {
            await bot.SendMessageWithKeyboard(
                session.UserId,
                """
                In fact of all data correctness please pay 100$ to obtain your insurance document.

                Do you agreed?
                """,
                [[new KeyboardButton("✅ Yes"), new KeyboardButton("❌ No")]],
                cancellationToken: cancellationToken);
            session.Step++;
        }
        else if (NoAnswers.Contains(text))
        {
            await bot.SendMessage(
                session.UserId,
                "Let's try again. Send photo of a passport",
                cancellationToken: cancellationToken);
            session.Step = 1;
        }
        else
        {
            await bot.SendMessage(
                session.UserId,
                "Please confirm with ✅ Yes or ❌ No.",
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleFinalConfirmationStep6(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        string? text = update.Message?.Text?.Trim();

        if (text == null)
            return;

        if (YesAnswers.Contains(text))
        {
            MemoryStream policyStream =
                _pdfPolicyGenerationService.GeneratePolicy(session.MindeeDataExtractionResponse);
            await bot.SendDocument(
                session.UserId,
                policyStream,
                "insurance_policy.pdf",
                "Congratulation, there is your insurance policy",
                cancellationToken: cancellationToken);
            session.Step = 0;
        }
        else if (NoAnswers.Contains(text))
        {
            await bot.SendMessage(
                session.UserId,
                "Unfortunately, the price is fixed at 100 USD.",
                cancellationToken: cancellationToken);
            session.Step = 0;
        }
        else
        {
            await bot.SendMessage(
                session.UserId,
                "Please confirm with ✅ Yes or ❌ No.",
                cancellationToken: cancellationToken);
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
