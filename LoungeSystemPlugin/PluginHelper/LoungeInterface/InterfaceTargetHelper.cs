using System.Data;
using Dapper;
using DSharpPlus.Entities;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper.LoungeInterface;

public static class InterfaceTargetHelper
{
    public static async Task<DiscordChannel?> GetTargetDiscordChannelAsync(DiscordChannel interactedChannel, DiscordMember invokingUser)
    {        
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        if (ReferenceEquals(invokingUser.VoiceState?.Channel, null))
        {
            return null;
        }
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

        //check channel if the interaction channel is an interface channel (then user channel is to be returned)
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            var configurationRecords = await mySqlConnection.QueryAsync("SELECT * FROM LoungeSystem.LoungeSystemConfigurationIndex WHERE InterfaceChannelId = @Id", new { interactedChannel.Id }, commandType: CommandType.Text);

            if (configurationRecords.Any())
                return invokingUser.VoiceState.Channel;
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "[{PluginName}] Cant query Mysql Records in GetTargetDiscordChannelIdAsync",LoungeSystemPlugin.GetStaticPluginName());
        }
        
        //if not check is the channel the invoking user is in is a lounge channel of the same configuration as the interface (then interaction channel is returned)
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            var loungeRecords = await mySqlConnection.QueryAsync("SELECT * FROM LoungeSystem.LoungeIndex WHERE ChannelId = @Id", new { interactedChannel.Id }, commandType: CommandType.Text);
            
            if (loungeRecords.Any())
                return interactedChannel;
        }        catch (MySqlException ex)
        {
            Log.Error(ex, "[{PluginName}] Cant query Mysql Records in GetTargetDiscordChannelIdAsync",LoungeSystemPlugin.GetStaticPluginName());
            return null;

        }
        
        return null;
    }
}