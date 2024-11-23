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

public static class LoungeSetupChannelSelect
{
    internal static async Task ChannelSelected(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        //Check if User has Admin Permissions
        if (!member.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,UIMessageBuilders.NoPermissionsResponseBuilder);
            return;
        }

        var messageId = eventArgs.Message.Id.ToString();

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

            var targetChannelId = eventArgs.Values[0];
        
            var newLoungeSetupRecord = new LoungeSetupRecord(targetChannelId, deserializedRecord.UserId, "", "");
        
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



        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,UIMessageBuilders.ChannelSelectedMessageBuilder);
    }
}