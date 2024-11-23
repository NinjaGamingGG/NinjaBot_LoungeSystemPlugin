using Dapper;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper;

/// <summary>
/// The <see cref="ChannelNameBuilder"/> class provides a static method for building channel names.
/// </summary>
public static class ChannelNameBuilder
{
    /// <summary>
    /// Asynchronously builds a channel name based on the provided guild ID, channel ID, and custom name content.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <param name="channelId">The ID of the channel.</param>
    /// <param name="customNameContent">The custom name content to include in the channel name.</param>
    /// <returns>The channel name pattern.</returns>
    public static async Task<string> BuildAsync(ulong guildId, ulong channelId, string customNameContent)
    {
        string channelNamePattern = "New Lounge";
        List<LoungeSystemConfigurationRecord> channelConfigurationList;
        ulong originChannelId;
        
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        var mySqlConnection = new MySqlConnection(connectionString);
        
        try
        {
            await mySqlConnection.OpenAsync();
        
            var channelConfigurations = await mySqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystem.LoungeSystemConfigurationIndex WHERE GuildId = @GuildId ", new { GuildId = guildId});
        
            channelConfigurationList = channelConfigurations.ToList();

            var channelRecords = await mySqlConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeSystem.LoungeIndex WHERE GuildId = @GuildId AND ChannelId = @ChannelId", new {GuildId = guildId, ChannelId = channelId});

            var channelRecordsAsList = channelRecords.ToList();
        
            originChannelId = channelRecordsAsList.First().OriginChannel;
        
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "[{PluginName}]Error while building LoungeSystem channel name", LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString());
            return channelNamePattern;
        }
        
        foreach (var channelConfig in channelConfigurationList.Where(channelConfig => originChannelId == channelConfig.TargetChannelId))
        {
            channelNamePattern = channelConfig.DecoratorPattern + customNameContent;
        }

        return channelNamePattern;
    }
}