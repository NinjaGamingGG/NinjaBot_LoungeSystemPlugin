using Dapper;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.CommandModules;


public static class LoungeCommandGroup
{
    [Command("lounge")]
    public static async Task LoungeCommand(CommandContext context)
    {
        var mysqlConnectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        LoungeDbRecord? loungeRecord;

        await context.DeferResponseAsync();

        try
        {
            var mysqlConnection = new MySqlConnection(mysqlConnectionString);
            await mysqlConnection.OpenAsync();

            var databaseRecords = await mysqlConnection.QueryAsync<LoungeDbRecord>("SELECT * FROM LoungeSystem.LoungeIndex WHERE ChannelId=@ChannelId", 
                new { ChannelId = context.Channel.Id });
            
            await mysqlConnection.CloseAsync();

            var loungeDbRecords = databaseRecords as LoungeDbRecord?[] ?? databaseRecords.ToArray();
            if (loungeDbRecords.Length == 0)
            {
                await context.FollowupAsync("Command failed, you have to use this command in a lounge channel.");
                return;
            }
            
            loungeRecord = loungeDbRecords[0];
        }
        catch (Exception ex)
        {
            await context.FollowupAsync("Command failed, please try again later.");
            Log.Error(ex, "Unable to query lounge Records for channel {ChannelId} from database", context.Channel.Id);
            return;
        }

        if (ReferenceEquals(loungeRecord, null))
        {
            await context.FollowupAsync("Command failed, please try again later.");
            return;
        }

        var privacyStatus = "Private";
        
        if (loungeRecord.IsPublic)
            privacyStatus = "Public";

        var originalChannel = await context.Client.GetChannelAsync(loungeRecord.OriginChannel);
        var owningUser = await context.Client.GetUserAsync(loungeRecord.OwnerId);

        await context.FollowupAsync(new DiscordEmbedBuilder().WithTitle("Lounge Details")
            .AddField("You are currently in:", context.Channel.Mention)
            .AddField("The Channel belongs to:", originalChannel.Mention)
            .AddField("The Channel Owner is:", owningUser.Mention)
            .AddField("The Channel is:" , privacyStatus));
    }
}