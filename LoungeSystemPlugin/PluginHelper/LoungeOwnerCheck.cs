using Dapper;
using DSharpPlus.Entities;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper;

public static class LoungeOwnerCheck
{
    /// <summary>
    /// Checks if a member is the owner of a lounge channel.
    /// </summary>
    /// <param name="member">The member to check.</param>
    /// <param name="channel">The channel to check.</param>
    /// <param name="guild">The guild to check.</param>
    /// <returns>True if the member is the owner of the lounge channel, false otherwise.</returns>
    public static async Task<bool> IsLoungeOwnerAsync(DiscordMember member, DiscordChannel channel, DiscordGuild guild)
    {
        if (ReferenceEquals(member, null))
            return false;
        
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        int existsAsOwner;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            existsAsOwner = await mySqlConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM LoungeIndex WHERE ChannelId = @ChannelId AND OwnerId = @OwnerId AND GuildId = @GuildId",
                new {ChannelId = channel.Id ,OwnerId = member.Id, GuildId = guild.Id});

            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"[{PluginName}] Unable to retrieve database lounge ownership on Guild {GuildId} for {LoungeName}/{LoungeId} of user {MemberName}/{MemberId}",LoungeSystemPlugin.GetStaticPluginName(),guild.Id,channel.Name,channel.Id,member.Username,member.Id);
            return false;
        }
        

        
        return existsAsOwner != 0;
    }
    
    public static async Task<ulong> GetOwnerIdAsync(DiscordChannel channel)
    {
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        ulong ownerId;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
        
            ownerId = await mySqlConnection.ExecuteScalarAsync<ulong>(
                "SELECT OwnerId FROM LoungeIndex WHERE ChannelId = @ChannelId",
                new {ChannelId = channel.Id});

            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"[{PluginName}] Error while Fetching owner Id on LoungeSystem. Channel: {ChannelName}/{ChannelId}",LoungeSystemPlugin.GetStaticPluginName(), channel.Name,channel.Id);
            return 0;
        }

        
        return ownerId;
    }
}