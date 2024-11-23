using Dapper;
using Dapper.Contrib.Extensions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class LoungeDeleteButtonLogic
{
    public static async Task ButtonInteracted(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember owningMember)
    {
        var targetChannel = await InterfaceTargetHelper.GetTargetDiscordChannelAsync(eventArgs.Channel, owningMember);

        if (ReferenceEquals(targetChannel, null))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                UIMessageBuilders.NotInChannelResponseBuilder);
            return;
        }
        
        await eventArgs.Interaction.DeferAsync();

        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, targetChannel, eventArgs.Guild);
        
        //Only non owners can delete
        if (!existsAsOwner)
            return;

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        List<LoungeDbRecord> loungeDbRecordList;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();

            var loungeDbRecordEnumerable = await mySqlConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeIndex WHERE GuildId = @GuildId AND ChannelId= @ChannelId", new {GuildId = eventArgs.Guild.Id, ChannelId = targetChannel.Id});
            await mySqlConnection.CloseAsync();
            loungeDbRecordList = loungeDbRecordEnumerable.ToList();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Error while querying lounge-db-records in the LoungeSystem Delete Button Logic. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, targetChannel.Name, targetChannel.Id);
            return;
        }


        if (loungeDbRecordList.Count == 0)
        {
            Log.Error("No LoungeDbRecord from Lounge Index on Guild {GuildId} at Channel {ChannelId}", eventArgs.Guild.Id, targetChannel.Id);
            return;
        }

        var loungeChannel = targetChannel;

        var afkChannel = await eventArgs.Guild.GetAfkChannelAsync();

        if (!ReferenceEquals(afkChannel,null))
        {        foreach (var loungeChannelUser in loungeChannel.Users)
            {
                await loungeChannelUser.PlaceInAsync(afkChannel);
            }
            
        }
        


        await loungeChannel.DeleteAsync();
        bool deleteSuccess;
        try
        {
            await using var mySqlConnection =  new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            deleteSuccess = await mySqlConnection.DeleteAsync(loungeDbRecordList.First());
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Unable to delete lounge database record in LoungeSystem. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, targetChannel.Name,targetChannel.Id);
            return;
        }
        
        if (deleteSuccess == false)
            Log.Error("Unable to delete the Sql Record for Lounge {LoungeName} with the Id {LoungeId} in Guild {GuildId}",loungeChannel.Name, targetChannel.Id, eventArgs.Guild.Id);
        
        await eventArgs.Interaction.DeleteOriginalResponseAsync();

    }
    
}