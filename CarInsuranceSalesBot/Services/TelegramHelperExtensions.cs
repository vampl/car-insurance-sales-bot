﻿using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CarInsuranceSalesBot.Services;

public static class TelegramHelperExtensions
{
    public static async Task SendMessageWithKeyboard(
        this ITelegramBotClient bot,
        long chatId,
        string text,
        IEnumerable<IEnumerable<KeyboardButton>> keyboard,
        CancellationToken cancellationToken)
    {
        ReplyKeyboardMarkup markup = new(keyboard) { ResizeKeyboard = true, OneTimeKeyboard = true };
        await bot.SendMessage(chatId, text, replyMarkup: markup, cancellationToken: cancellationToken);
    }

    public static async Task<MemoryStream> DownloadFile(
        this ITelegramBotClient bot,
        string fileId,
        CancellationToken cancellationToken)
    {
        TGFile file = await bot.GetFile(fileId, cancellationToken);
        MemoryStream memoryStream = new();

        await bot.DownloadFile(file.FilePath!, memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return memoryStream;
    }

    public static async Task SendDocument(
        this ITelegramBotClient bot,
        long chatId,
        Stream file,
        string filename,
        string caption,
        CancellationToken cancellationToken)
    {
        var inputFile = new InputFileStream(file, filename);
        await bot.SendDocument(chatId, inputFile, caption, cancellationToken: cancellationToken);
    }
}
