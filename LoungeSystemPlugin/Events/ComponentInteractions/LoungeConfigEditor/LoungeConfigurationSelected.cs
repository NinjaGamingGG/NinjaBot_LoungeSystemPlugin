using Dapper;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using NinjaBot_DC;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeConfigEditor;

public static class LoungeConfigurationSelected
{
    internal static async Task ChannelSelectionMade(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember invokingMember)
    {
        if (!invokingMember.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
            return;
        }
        
        var parseSuccess = ulong.TryParse(eventArgs.Interaction.Data.Values[0], out var selectedChannelId);

        if (parseSuccess == false)
        {
            await eventArgs.Interaction.DeferAsync();
            return;
        }

        var selectedChannel = await eventArgs.Guild.GetChannelAsync(selectedChannelId);

        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            UIMessageBuilders.LoungeConfigSelectedResponseBuilder(selectedChannel.Mention, true));

        var responseMessage = await eventArgs.Interaction.GetOriginalResponseAsync();
        
        try
        {
            var redisConnection = await ConnectionMultiplexer.ConnectAsync(LoungeSystemPlugin.RedisConnectionString);
            var db = redisConnection.GetDatabase();
            
            var hash = new HashEntry[]
            {
                new("TargetChannelId", selectedChannel.Id),
            };
            var redisKey = $"InteractionMessageId:{responseMessage.Id}";

            db.HashSet(redisKey, hash);
            db.KeyExpire(redisKey, TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{PluginName}] Error while adding Interaction Message to Cache", LoungeSystemPlugin.RedisConnectionString);
        }
    }

    internal static async Task DeleteButton(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember invokingMember)
    {
        if (!invokingMember.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
            return;
        }
        
        if (ReferenceEquals(eventArgs.Interaction.Message, null))
        {
            return;
        }
        
        var hashFieldValue = RedisValue.Null;
        try
        {
            var redisConnection = await ConnectionMultiplexer.ConnectAsync(LoungeSystemPlugin.RedisConnectionString);
            var db = redisConnection.GetDatabase();
            
            var redisKey = $"InteractionMessageId:{eventArgs.Interaction.Message.Id}";
            
            var hashFields = db.HashGetAll(redisKey);
            
            await redisConnection.CloseAsync();
            
            if (hashFields.Length == 0)
            {
                await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Interaction Failed, please try again later."));
                return;
            }
            hashFieldValue = hashFields[0].Value;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{PluginName}] Error while querying Interaction Message from Cache", LoungeSystemPlugin.RedisConnectionString);
        }
        
        var parseIdSuccess = ulong.TryParse(hashFieldValue, out var targetChannelId);
        
        if (!parseIdSuccess)
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Interaction Failed, please try again later."));
            return;
        }

        var deleteSuccess = 0;
        
        
        try
        {
            var mysqlConnection = new MySqlConnection(LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString());
            await mysqlConnection.OpenAsync();

            deleteSuccess = await mysqlConnection.ExecuteAsync("DELETE FROM LoungeSystemConfigurationIndex WHERE TargetChannelId=@TargetChannelId", 
                new { TargetChannelId = targetChannelId });
                
            await mysqlConnection.CloseAsync();

        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"[{PluginName}] Error while querying Configurations from DB", LoungeSystemPlugin.GetStaticPluginName());
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Interaction Failed, please try again later."));
        }
        
        if (deleteSuccess == 0)
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Unable to delete Config"));
            
            return;
        }
        
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent("Config deleted successfully"));
        
        List<LoungeDbRecord> loungeDbRecordList = [];
        
        try
        {
            var mysqlConnection = new MySqlConnection(LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString());
            await mysqlConnection.OpenAsync();

             var loungeDbRecords = await mysqlConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeIndex WHERE OriginChannel=@TargetChannelId", 
                new { TargetChannelId = targetChannelId });
                
            await mysqlConnection.CloseAsync();
            
            loungeDbRecordList = loungeDbRecords.ToList();

        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"[{PluginName}] Error while querying LoungeRecords from Database Index", LoungeSystemPlugin.GetStaticPluginName());
        }
        
        if (loungeDbRecordList.Count == 0)
            return;

        foreach (var record in loungeDbRecordList)
        {
            var deleteRecordSuccess = 0;
            
            try
            {
                var mysqlConnection = new MySqlConnection(LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString());
                await mysqlConnection.OpenAsync();

                deleteRecordSuccess = await mysqlConnection.ExecuteAsync("DELETE FROM LoungeIndex WHERE ChannelId=@TargetChannelId", 
                    new { TargetChannelId = record.ChannelId });
                
                await mysqlConnection.CloseAsync();

            }
            catch (MySqlException ex)
            {
                Log.Error(ex,"[{PluginName}] Error while deleting Lounge Records from Database Index", LoungeSystemPlugin.GetStaticPluginName());
            }

            if (deleteRecordSuccess == 0)
            {
                Log.Error("[{PluginName}] Unable to delete Records from Database Index", LoungeSystemPlugin.GetStaticPluginName());
                return;
            }

            var client = Worker.GetServiceDiscordClient();
            var channel = await client.GetChannelAsync(record.ChannelId);

            await channel.DeleteAsync();
        }
    }
    
    internal static async Task ResetNamePatternButton(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember invokingMember)
    {
        if (!invokingMember.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
            return;
        }
        
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal,
            UIMessageBuilders.ChannelNamePatternRenameModalBuilder);
    }
    
    internal static async Task ResetInterfaceButton(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember invokingMember)
    {
        if (!invokingMember.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
            return;
        }
        
        if (ReferenceEquals(eventArgs.Interaction.Message, null))
        {
            return;
        }

        await eventArgs.Interaction.DeferAsync();
        
        var hashFieldValue = RedisValue.Null;
        try
        {
            var redisConnection = await ConnectionMultiplexer.ConnectAsync(LoungeSystemPlugin.RedisConnectionString);
            var db = redisConnection.GetDatabase();
            
            var redisKey = $"InteractionMessageId:{eventArgs.Interaction.Message.Id}";
            
            var hashFields = db.HashGetAll(redisKey);
            
            await redisConnection.CloseAsync();
            
            if (hashFields.Length == 0)
            {
                await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Interaction Failed, please try again later."));
                return;
            }
            hashFieldValue = hashFields[0].Value;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{PluginName}] Error while querying Interaction Message from Cache", LoungeSystemPlugin.RedisConnectionString);
        }
        
        var parseIdSuccess = ulong.TryParse(hashFieldValue, out var targetChannelId);
        
        if (!parseIdSuccess)
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Interaction Failed, please try again later."));
            return;
        }

        List<LoungeSystemConfigurationRecord> configRecordsAsList = [];
        
        try
        {
            var mysqlConnection = new MySqlConnection(LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString());
            await mysqlConnection.OpenAsync();

            var configRecords = await mysqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystemConfigurationIndex WHERE TargetChannelId=@TargetChannelId", 
                new { TargetChannelId = targetChannelId });
                
            await mysqlConnection.CloseAsync();
            
            configRecordsAsList = configRecords.ToList();

        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"[{PluginName}] Error while querying LoungeRecords from Database Index", LoungeSystemPlugin.GetStaticPluginName());
        }


        if (configRecordsAsList.Count == 0)
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Interaction Failed, please try again later."));
            return;
        }

        var configRecord = configRecordsAsList[0];
        
        if (configRecord.TargetChannelId == 0)
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Error: Configuration has Interfaces set to Internal"));
            return;
        }

        var discordClient = Worker.GetServiceDiscordClient();
        var targetChannel = await discordClient.GetChannelAsync(targetChannelId);
        var interfaceChannel = await discordClient.GetChannelAsync(configRecord.InterfaceChannelId);
        
        var builder = InterfaceMessageBuilder.GetBuilder(discordClient,
            "This ist the Interface for all Lounges of "+ targetChannel.Mention+" channels");

        await interfaceChannel.SendMessageAsync(builder);

    }
}