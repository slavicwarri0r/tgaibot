﻿
using Telegram.Bot;
using CatDogTelegramBot;
using Newtonsoft.Json;
using Telegram.Bot.Polling;
using Telegram.Bot.Exceptions;
using System.Globalization;

internal class Program
{
    private static void Main(string[] args)
    {
        var client = new TelegramBotClient("6770852830:AAETZF6rqb1DQSg84SQtc635UOd2VkH8-PA");
        Console.WriteLine($"The bot {client.GetMeAsync().Result.FirstName} has started!");
        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { }, // receive all update types
        };
        client.StartReceiving(
            UpdateAsync,
            ErrorAsync,
            receiverOptions,
            cancellationToken
        );
        Console.ReadLine();
        cts.Cancel();
        cts.Dispose();
    }

    private static Task ErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    private static async Task UpdateAsync(ITelegramBotClient client, Telegram.Bot.Types.Update update, CancellationToken token)
    {
        DateTime localTime = DateTime.Now;
        if (update.Message is not { } message) return;
        if (message.Date.ToUniversalTime() >= localTime.AddMinutes(-1).ToUniversalTime() &&
        message.Date.ToUniversalTime() <= localTime.AddMinutes(1).ToUniversalTime())
        {
            Console.WriteLine($"SendTime (UTF): {message.Date}\nLocalTime: {localTime}\nMessage: {JsonConvert.SerializeObject(update)}");
            if (message.Text != null)
            {
                if (message.Text == @"/start")
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "Welcome!\nThis bot detects whether it is a cat or dog on a picture.\n" +
                        "Supported formats are: .jpeg, .jpg, .png, .gif.\nPlease, send picture as documents/files." );
                    return;
                }
                return;
            }
            if (message.Photo != null) 
            {
                await client.SendTextMessageAsync(message.Chat.Id, "Please, send picture as document.");
                return;
            }
            if (message.Document != null)
            {
                if (message.Document.FileName == null) return;
                if (!IsPhotoByExtension(message.Document.FileName))
                {
                    await client.SendTextMessageAsync(message.Chat.Id, "Wrong file format!\n" +
                            "Supported formats are: .jpeg, .jpg, .png, .gif.\nPlease, send picture as documents/files.");
                }
                var fileID = message.Document.FileId;
                var fileInfo = await client.GetFileAsync(fileID);
                var filePath = fileInfo.FilePath;
                if (!Directory.Exists(@$"{Environment.CurrentDirectory}\DownloadedFiles\)"))
                {
                    Directory.CreateDirectory(@$"{Environment.CurrentDirectory}\DownloadedFiles\");
                    Console.WriteLine("Directory created.");
                }
                string destinationFilePath = @$"{Environment.CurrentDirectory}\DownloadedFiles\{message.Document.FileName}";
                await using Stream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                await client.DownloadFileAsync(filePath!, fileStream);
                fileStream.Close();
                var sortedScoresWithLabel = await Task.Run(() => CatRecognizerModel.processPictureWithModel(destinationFilePath));
                System.IO.File.Delete(destinationFilePath);
                foreach (var score in sortedScoresWithLabel)
                {
                    string formattedValue = score.Value.ToString("F2", CultureInfo.InvariantCulture);
                    await client.SendTextMessageAsync(message.Chat.Id, $"{score.Key}: {formattedValue}%");
                }
                return;
            }
        }
        return;
    }

    static bool IsPhotoByExtension(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        string[] photoExtensions = { ".jpg", ".jpeg", ".png", ".gif"};
        return Array.Exists(photoExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }
}