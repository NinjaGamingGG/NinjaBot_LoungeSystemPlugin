using Dapper;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using MySqlConnector;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin.Events.ModalsSubmitted;

public static class LoungeConfigResetNameModal
{
    public static async Task ModalSubmitted(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        
        if (ReferenceEquals(eventArgs.Interaction.Message, null) || ReferenceEquals(eventArgs.Interaction.Guild, null))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Interaction Failed, please try again later."));
            return;
        }
        
        var invokingMember = await eventArgs.Interaction.Guild.GetMemberAsync(eventArgs.Interaction.User.Id);
        if (!invokingMember.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,new DiscordInteractionResponseBuilder().WithContent("You do not have permission to do this."));
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
        var parseNameSuccess =eventArgs.Values.TryGetValue(CustomComponentIdHelper.LoungeConfig.ResetPatternModalNameComponent, out var namePatternString);
        var parseDecoratorSuccess =eventArgs.Values.TryGetValue(CustomComponentIdHelper.LoungeConfig.ResetPatternModalDecoratorComponent, out var nameDecoratorString);
            
        if (!parseIdSuccess || !parseNameSuccess || !parseDecoratorSuccess)
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Interaction Failed, please try again later."));
            return;
        }

        bool containsProfanity;
        
        //Check for profanity
        try
        {
            containsProfanity  = await ProfanityCheck.CheckString(nameDecoratorString + " " + namePatternString);
        }
        catch (Exception ex)
        {
            Log.Error(ex,"[{PluginName}] Error while checking for Profanity", LoungeSystemPlugin.GetStaticPluginName());
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Interaction Failed, please try again later."));
            return;
        }

        if (containsProfanity)
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Unable to set that as new Name Pattern"));
            return;
        }
        
        var updateSuccess = 0;
        
        try
        {
            var mysqlConnection = new MySqlConnection(LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString());
            await mysqlConnection.OpenAsync();

            updateSuccess = await mysqlConnection.ExecuteAsync("UPDATE LoungeSystemConfigurationIndex SET LoungeNamePattern=@NameReplacement, DecoratorPattern=@DecoratorReplacement WHERE TargetChannelId=@TargetChannelId", 
                new { NameReplacement = namePatternString, DecoratorReplacement = nameDecoratorString , TargetChannelId = targetChannelId });
                
            await mysqlConnection.CloseAsync();

        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"[{PluginName}] Error while querying Interaction Message from Cache", LoungeSystemPlugin.GetStaticPluginName());
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Interaction Failed, please try again later."));
        }

        if (updateSuccess == 1)
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent("Name Pattern updated successfully"));
            return;
        }
        
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder()
                .WithContent("Unable to update Name Pattern"));

    }
    
}