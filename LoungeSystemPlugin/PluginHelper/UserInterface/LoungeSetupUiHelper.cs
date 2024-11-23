using Dapper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.Records.Cache;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using NinjaBot_DC;
using Serilog;

namespace LoungeSystemPlugin.PluginHelper.UserInterface;

public static class LoungeSetupUiHelper
{
    public static async Task CompleteSetup(LoungeSetupRecord setupRecord,ulong guildId, ulong interfaceChannelId = 0)
    {
        var parseSuccess = ulong.TryParse(setupRecord.ChannelId, out var targetChannelId);

        if (!parseSuccess)
        {
            Log.Error("[{PluginName}] Unable to parse channelId for {targetChannelId}", LoungeSystemPlugin.GetStaticPluginName(), setupRecord.ChannelId);    
        }
        
        var newConfigRecord = new LoungeSystemConfigurationRecord()
        {
            GuildId = guildId,
            TargetChannelId = targetChannelId,
            InterfaceChannelId = interfaceChannelId,
            LoungeNamePattern = setupRecord.NamePattern,
            DecoratorPattern = setupRecord.NameDecorator
        };
        
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
        
            if (ReferenceEquals(mySqlConnection, null))
            {
                Log.Error("[{PluginName}] Unable to connect to database!",LoungeSystemPlugin.GetStaticPluginName());
                return;
            }
        
            var alreadyExists = await mySqlConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM LoungeSystemConfigurationIndex WHERE GuildId = @GuildId AND TargetChannelId = @TargetChannelId",
                new { GuildId = guildId, TargetChannelId = targetChannelId });
        
            if (alreadyExists != 0)
            {
                Log.Error("[{PluginName}] Error while Executing Setup Completion in LoungeSetupUiHelper. Configuration already exists!", LoungeSystemPlugin.GetStaticPluginName());
                return;
            }
        
            await mySqlConnection.ExecuteAsync(
                "INSERT INTO LoungeSystemConfigurationIndex (GuildId, TargetChannelId, InterfaceChannelId, LoungeNamePattern, DecoratorPattern) VALUES (@GuildId, @TargetChannelId, @InterfaceChannelId, @LoungeNamePattern, @DecoratorPattern)",
                newConfigRecord);
        
            await mySqlConnection.CloseAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, "[{PluginName}] Error while Executing Mysql Operations on LoungeSetupUiHelper Setup Completion", LoungeSystemPlugin.GetStaticPluginName());
        }
        
        if (interfaceChannelId != 0)
            await PrintExternalLoungeInterface(interfaceChannelId, targetChannelId);
        
    }

    private static async Task PrintExternalLoungeInterface(ulong interfaceChannelId, ulong targetChannelId)
    {
        var discordClient = Worker.GetServiceDiscordClient();
        var interfaceChannel = await discordClient.GetChannelAsync(interfaceChannelId);
        var targetChannel = await discordClient.GetChannelAsync(targetChannelId);

        var builder = InterfaceMessageBuilder.GetBuilder(discordClient,
            "This ist the Interface for all Lounges of "+ targetChannel.Mention+" channels");
        
        await interfaceChannel.SendMessageAsync(builder);
    }


}