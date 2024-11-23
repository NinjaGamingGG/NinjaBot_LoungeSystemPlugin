using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using LoungeSystemPlugin.Records.Cache;
using Newtonsoft.Json;
using NRedisStack.RedisStackCommands;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupInterfaceChannelSelection
{
    internal static async Task SelectionMade(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        //Check if User has Admin Permissions
        if (!member.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,UIMessageBuilders.NoPermissionsResponseBuilder);
            return;
        }
        
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
            UIMessageBuilders.LoungeSetupComplete);
        
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
            var parseSuccess = ulong.TryParse(eventArgs.Values[0], out var setupChannelId);

            if (!parseSuccess)
                return;
            
            await LoungeSetupUiHelper.CompleteSetup(deserializedRecord,eventArgs.Guild.Id,setupChannelId);

        }
        catch (Exception ex)
        {
            Log.Error(ex,"[{PluginName}] Unable to update LoungeSetupRecord for ui message {messageId}",LoungeSystemPlugin.GetStaticPluginName(), messageId);
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage, UIMessageBuilders.InteractionFailedResponseBuilder($"Unable to update LoungeSetupRecord for ui message {messageId}"));
        }
    }
}