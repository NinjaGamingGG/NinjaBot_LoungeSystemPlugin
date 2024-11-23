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
        if (ReferenceEquals(eventArgs.Channel, null))
            return;

        await NewLoungeHelper.CreateNewLounge(eventArgs.Guild, eventArgs.Channel, eventArgs.User);
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
            if (ReferenceEquals(eventArgs.Before.Channel,null))
                return;
            
            if (loungeDbModel.ChannelId != eventArgs.Before.Channel.Id)
                continue;
            
            if (eventArgs.Before.Channel.Users.Count != 0)
                return;
            
            await CleanupLounge.Execute(loungeDbModel);

        }
    }
    
}