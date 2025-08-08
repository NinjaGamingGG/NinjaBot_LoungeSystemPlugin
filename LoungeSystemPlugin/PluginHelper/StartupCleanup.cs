using Dapper;
using Dapper.Contrib.Extensions;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using NinjaBot_DC;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper;

public static class StartupCleanup
{
    public static async Task Execute()
    {
        var startupTasks = new List<Task>([EmptyChannelCheck(), NewUserCheck()]);
        await Task.WhenAll(startupTasks);
    }

    /// <summary>
    /// Checks for and performs cleanup operations on empty lounges in the guild's target channel during bot startup.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private static async Task EmptyChannelCheck()
    {
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        
        List<LoungeDbRecord> loungeDbRecordList;

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            var loungeDbModels = await mySqlConnection.GetAllAsync<LoungeDbRecord>();
            loungeDbRecordList = loungeDbModels.ToList();
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "[{PluginName}] Error while reading lounge db records on StartupCleanup", LoungeSystemPlugin.GetStaticPluginName());
            return;
        }
        
        var client = Worker.GetServiceDiscordClient();
        
        while (!client.AllShardsConnected)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
        
        foreach (var loungeDbModel in loungeDbRecordList)
        {
            await CleanupLounge.Execute(loungeDbModel);
        }
    }

    /// <summary>
    /// Checks for new users in the guild's target channel on bot startup and creates new lounges for them.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private static async Task NewUserCheck()
    {
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();

        List<LoungeSystemConfigurationRecord> guildConfigRecords;
        
        try
        {
            var mysqlConnection = new MySqlConnection(connectionString);
            var results = await mysqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystem.LoungeSystemConfigurationIndex");
            guildConfigRecords = results.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return;
        }

        var client = Worker.GetServiceDiscordClient();
        
        //Loop through all guild configurations, then get the users and check if someone is in the target channels and create new lounge
        foreach (var guildConfig in guildConfigRecords)
        {
            var guild = await client.GetGuildAsync(guildConfig.GuildId);

            var allGuildUsers = guild.GetAllMembersAsync();

            await foreach (var member in allGuildUsers)
            {
                var memberChannel = await member.VoiceState.GetChannelAsync();
                if (ReferenceEquals(memberChannel, null))
                    continue;

                if (memberChannel.Id != guildConfig.TargetChannelId)
                    continue;

                await NewLoungeHelper.CreateNewLounge(guild, memberChannel, member);

            }
        }
    }
    
} 