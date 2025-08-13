using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using LoungeSystemPlugin.Records.Cache;
using Newtonsoft.Json;
using NRedisStack.RedisStackCommands;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin.Events.ModalsSubmitted;

public static class LoungeSetupUiModal
{
    internal static async Task ModalSubmitted(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        //Check if Guild is null
        if (ReferenceEquals(eventArgs.Interaction.Guild, null))
        {
            await eventArgs.Interaction.DeferAsync();
            return;
        }
        
        //Get the DiscordMember that Submitted the Modal
        var member = await eventArgs.Interaction.Guild.GetMemberAsync(eventArgs.Interaction.User.Id);
            
        //Check if User has Admin Permissions
        if (!member.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,UIMessageBuilders.NoPermissionsResponseBuilder);
            return;
        }
        
        if (ReferenceEquals(eventArgs.Interaction.Message, null))
            return;
        
        var messageId = eventArgs.Interaction.Message.Id.ToString();

        try
        {
            var redisConnection = await ConnectionMultiplexer.ConnectAsync(LoungeSystemPlugin.RedisConnectionString);
            var redisDatabase = redisConnection.GetDatabase(LoungeSystemPlugin.RedisDatabase);
            var entryKey = new RedisKey(messageId);

            var json = redisDatabase.JSON();
            var redisResult = json.Get(entryKey, path:"$").ToString().TrimEnd(']').TrimStart('[');
            var deserializedRecord = JsonConvert.DeserializeObject<LoungeSetupRecord>(redisResult);
            if (deserializedRecord is null)
                return;

            eventArgs.Values.TryGetValue(PluginHelper.CustomComponentIdHelper.LoungeSetup.NamePatternModalNameComponent, out var namePatternString);
            eventArgs.Values.TryGetValue(PluginHelper.CustomComponentIdHelper.LoungeSetup.NamePatternModalDecoratorComponent, out var nameDecoratorString);
            
            if (namePatternString is null || nameDecoratorString is null)
                return;
        
            var newLoungeSetupRecord = deserializedRecord with { NamePattern = namePatternString, NameDecorator = nameDecoratorString };
        
            var remainingTimeToLive = redisDatabase.KeyTimeToLive(entryKey);

             
            json.Set(messageId, "$", newLoungeSetupRecord);
            
            redisDatabase.KeyExpire(messageId, remainingTimeToLive);

        }
        catch (Exception ex)
        {
            Log.Error(ex,"[{PluginName}] Unable to update LoungeSetupRecord for ui message {messageId}",LoungeSystemPlugin.GetStaticPluginName(), messageId);
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, UIMessageBuilders.InteractionFailedResponseBuilder($"Unable to update LoungeSetupRecord for ui message {messageId}"));
            return;
        }
        
        //Update the Message this Modal was attached to
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
            UIMessageBuilders.ModalSubmittedResponseBuilder);

    }
}