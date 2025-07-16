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
    private readonly MistralAiAskingService _mistralAiAskingService;

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
        MistralAiAskingService mistralAiAskingService,
        CancellationToken cancellationToken)
    {
        _sessionManager = sessionManager;
        _ocrService = ocrService;
        _pdfPolicyGenerationService = pdfPolicyGenerationService;
        _mistralAiAskingService = mistralAiAskingService;

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

        if (!string.IsNullOrWhiteSpace(update.Message.Text))
        {
            var lower = update.Message.Text.ToLowerInvariant();

            if (!YesAnswers.Contains(lower) && !NoAnswers.Contains(lower))
            {
                if (!IsStructuredInputExpected(update))
                {
                    // Send to AI
                    await HandleGeneralQuestion(bot, update, session, cancellationToken);
                    return;
                }
            }
        }

        switch (session.Step)
        {
            case 0:
                await HandleStartStep0(bot, session, cancellationToken);
                return;
            case 1:
                await HandlePassportObtainingStep1(bot, update, session, cancellationToken);
                return;
            case 2:
                await HandlePassportConfirmationStep2(bot, update, session, cancellationToken);
                return;
            case 3:
                await HandleVehicleIdObtainingStep3(bot, update, session, cancellationToken);
                return;
            case 4:
                await HandleVehicleIdConfirmationStep4(bot, update, session, cancellationToken);
                return;
            case 5:
                await HandleSummaryConfirmationStep5(bot, update, session, cancellationToken);
                return;
            case 6:
                await HandleFinalConfirmationStep6(bot, update, session, cancellationToken);
                return;
        }
    }

    private bool IsStructuredInputExpected(Update update)
    {
        if (update.Message is null)
            return false;

        // During Step 1 or 3 we expect a photo, ignore AI response
        if (update.Message.Photo != null)
            return true;

        // Only expect structured input if message is Yes/No
        string? text = update.Message.Text;
        if (text is null)
            return false;

        // Ignore commands
        if (text.StartsWith('/'))
            return true;

        return YesAnswers.Contains(text) || NoAnswers.Contains(text);
    }

    private async Task HandleGeneralQuestion(
        ITelegramBotClient bot,
        Update update,
        UserSession session,
        CancellationToken cancellationToken)
    {
        string response =
            await _mistralAiAskingService.AskAsync(
                $"Answer this car insurance-related question in a helpful and friendly tone: {update.Message?.Text}");

        await bot.SendMessage(session.UserId, response, cancellationToken: cancellationToken);
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
            string text = SummaryBuilder.BuildPassportInfoString(passport);

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
            string text = SummaryBuilder.BuildVehicleInfoString(vehicle);

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
            await bot.SendMessageWithKeyboard(
                session.UserId,
                summary,
                [[new KeyboardButton("✅ Yes"), new KeyboardButton("❌ No")]],
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

        return string.Join(
            "\n\n",
            SummaryBuilder.BuildPassportInfoString(passport),
            SummaryBuilder.BuildVehicleInfoString(vehicleId));
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
