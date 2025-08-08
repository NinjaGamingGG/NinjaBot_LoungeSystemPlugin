using Dapper.Contrib.Extensions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.Events;

public static class VoiceStateUpdated
{
    public static async Task ChannelEnter(DiscordClient client, VoiceStateUpdatedEventArgs eventArgs)
    {
        var enteredChannel = await eventArgs.After.GetChannelAsync();
        if (ReferenceEquals(enteredChannel, null))
            return;

        var eventGuild = await eventArgs.GetGuildAsync();
        if (ReferenceEquals(eventGuild, null))
            return;
        
        var eventUser = await eventArgs.GetUserAsync();
        if (ReferenceEquals(eventUser, null))
            return;
        
        await NewLoungeHelper.CreateNewLounge(eventGuild, enteredChannel, eventUser);
    }

    public static async Task ChannelLeave(DiscordClient client, VoiceStateUpdatedEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.Before, null))
            return;

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        List<LoungeDbRecord> loungeList;

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();

            var loungeRecords = await mySqlConnection.GetAllAsync<LoungeDbRecord>();
            await mySqlConnection.CloseAsync();

            loungeList = loungeRecords.ToList();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"[{PluginName}] Error while Operating on the MySql Database in ChannelLeave Task on VoiceStateUpdated Event in LoungeSystem",LoungeSystemPlugin.GetStaticPluginName());
            return;
        }
        
        foreach (var loungeDbModel in loungeList)
        {
            var leftChannel = await eventArgs.Before.GetChannelAsync();
            if (ReferenceEquals(leftChannel,null))
                return;
            
            if (loungeDbModel.ChannelId != leftChannel.Id)
                continue;
            
            if (leftChannel.Users.Count != 0)
                return;
            
            await CleanupLounge.Execute(loungeDbModel);

        }
    }
    
}