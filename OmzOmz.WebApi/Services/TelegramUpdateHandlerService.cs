using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OmzOmz.WebApi.Context;
using OmzOmz.WebApi.Entities;
using OmzOmz.WebApi.StateMachines;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Chat = OmzOmz.WebApi.StateMachines.Chat;

namespace OmzOmz.WebApi.Services;

public class TelegramUpdateHandlerService(
    ILogger<TelegramUpdateHandlerService> logger,
    AppDbContext dbContext,
    HybridCache cache,
    ITelegramBotClient bot
) : IUpdateHandler
{
    public async Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken ct
    )
    {
        logger.LogInformation("Error in update handler: {Exception}", exception);

        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        await (update switch
        {
            { Message: { } message } => OnMessage(message, ct),
            { EditedMessage: { } message } => OnMessage(message, ct),
            _ => UnknownUpdateHandlerAsync(update)
        });
    }

    private async Task OnMessage(Message msg, CancellationToken ct)
    {
        logger.LogInformation("Receive message type: {MessageType}", msg.Type);

        if (msg.Text is not { } messageText)
            return;

        var profile = await dbContext.Profiles
            .SingleOrDefaultAsync(p => p.Id == msg.Chat.Id, cancellationToken: ct);

        if (profile is null)
        {
            var entity = await dbContext.Profiles.AddAsync(new Profile
            {
                Id = msg.Chat.Id,
                Name = "",
                Description = "",
                Chat = new Chat()
            }, ct);

            profile = entity.Entity;

            await dbContext.SaveChangesAsync(ct);
        }

        switch (profile.Chat.CurrentState)
        {
            case Chat.State.Start:
            {
                await bot.SendMessage(msg.Chat, "Welcome! Let's onboard you!", cancellationToken: ct);
                await bot.SendMessage(msg.Chat, "What's your name!", cancellationToken: ct);
                await profile.Chat.EditProfileNameAsync();
                break;
            }
            case Chat.State.EditingProfileName:
            {
                profile.Name = msg.Text;
                await bot.SendMessage(msg.Chat, "What's your age!", cancellationToken: ct);
                await profile.Chat.EditProfileAgeAsync();
                break;
            }
            case Chat.State.EditingProfileAge:
            {
                profile.Name = msg.Text;
                await bot.SendMessage(msg.Chat, "Description!", cancellationToken: ct);
                await profile.Chat.EditProfileDescriptionAsync();
                break;
            }
            case Chat.State.EditingProfileDescription:
            {
                profile.Description = msg.Text;
                await bot.SendMessage(msg.Chat, "Picture!", cancellationToken: ct);
                await profile.Chat.EditProfilePictureAsync();
                break;
            }
            case Chat.State.EditingProfilePicture:
                break;
            case Chat.State.ViewingProfileSummary:
                break;
            case Chat.State.ViewingProfiles:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        await dbContext.SaveChangesAsync(ct);
        await bot.SendMessage(msg.Chat, JsonSerializer.Serialize(profile), cancellationToken: ct);
        await bot.SendMessage(msg.Chat, profile.Chat.Dot());
    }

    async Task<Message> Usage(Message msg)
    {
        const string usage = """
                             <b><u>Bot menu</u></b>:
                             /photo          - send a photo
                             /inline_buttons - send inline buttons
                             /keyboard       - send keyboard buttons
                             /remove         - remove keyboard buttons
                             /request        - request location or contact
                             /inline_mode    - send inline-mode results list
                             /poll           - send a poll
                             /poll_anonymous - send an anonymous poll
                             /throw          - what happens if handler fails
                             """;

        return await bot.SendMessage(msg.Chat, usage, parseMode: ParseMode.Html,
            replyMarkup: new ReplyKeyboardRemove());
    }

    private Task UnknownUpdateHandlerAsync(Update update)
    {
        logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}