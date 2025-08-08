using Dapper;
using DSharpPlus.Commands;
using DSharpPlus.Entities;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using LoungeSystemPlugin.Records.Cache;
using LoungeSystemPlugin.Records.MySQL;
using MySqlConnector;
using NRedisStack.RedisStackCommands;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin.CommandModules;

// ReSharper disable once StringLiteralTypo
[Command("loungesystem")]
public static class AdminLoungeSystemCommandsGroup
{
    [Command("setup")]
    public static async Task LoungeSetupCommand(CommandContext context)
    {
        if (ReferenceEquals(context.Member, null))
        {
            await context.DeferResponseAsync();
            return;
        }
        await context.DeferResponseAsync();
        if (!context.Member.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            await context.DeferResponseAsync();
            Log.Debug("User {userName} doesnt hast Permission for '/lounge setup' command", context.Member.Username);
            await context.RespondAsync(UIMessageBuilders.NoPermissionMessageBuilder);
            return;
        }
        
        await context.RespondAsync(UIMessageBuilders.InitialMessageBuilder);
        
        var responseMessage = await context.GetResponseAsync();

        if (ReferenceEquals(responseMessage, null))
        {
            Log.Error("[{PluginName}] Unable to get response from Discord!", LoungeSystemPlugin.GetStaticPluginName());
        }

        try
        {
            var redisConnection = await ConnectionMultiplexer.ConnectAsync(LoungeSystemPlugin.RedisConnectionString);
            var redisDatabase = redisConnection.GetDatabase(LoungeSystemPlugin.RedisDatabase);
            var json = redisDatabase.JSON();
            
            var newLoungeSetupRecord = new LoungeSetupRecord("", context.User.Id.ToString(), "", "");
            json.Set(responseMessage!.Id.ToString(), "$", newLoungeSetupRecord);
            redisDatabase.KeyExpire(responseMessage.Id.ToString(), TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{PluginName}] Unable to insert new configuration record!",LoungeSystemPlugin.GetStaticPluginName());
        }
    }

    [Command("config")]
    public static async Task ConfigCommand(CommandContext context)
    {
        //validation and permissions check
        if (ReferenceEquals(context.Member, null))
        {
            await context.DeferResponseAsync();
            return;
        }

        if (!context.Member.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            Log.Debug("User {userName} doesnt hast Permission for '/lounge config' command", context.Member.Username);
            await context.RespondAsync(UIMessageBuilders.NoPermissionMessageBuilder);
            return;
        }
        
        await context.DeferResponseAsync();
        
        //get a list with all lounge configurations for this guild
        var mysqlConnectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        var mysqlConnection = new MySqlConnection(mysqlConnectionString);

        if (ReferenceEquals(context.Guild, null))
            return;

        List<LoungeSystemConfigurationRecord> foundRecordsList = [];

        try
        {
            await mysqlConnection.OpenAsync();

            var configurations = await mysqlConnection.QueryAsync<LoungeSystemConfigurationRecord>("SELECT * FROM LoungeSystem.LoungeSystemConfigurationIndex WHERE GuildId = @GuildId",new {GuildId = context.Guild.Id});

            await mysqlConnection.CloseAsync();
            
            var configurationsAsList = configurations.ToList();

            if (configurationsAsList.Count == 0)
            {
                await context.RespondAsync(UIMessageBuilders.NoConfigurationsFound);
                return;
            }

            foundRecordsList = configurationsAsList;
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "[{PluginName}] Unable to query configuration records!",context.Member.Username);
        }

        List<DiscordEmbed> embedList = [];
        List<DiscordSelectComponentOption> selectComponentOptions = [];
        
        //print list as message back to user
        foreach (var record in foundRecordsList)
        {
            var targetChannel = await context.Guild.GetChannelAsync(record.TargetChannelId);

            
            string interfaceContentString;
            if (record.InterfaceChannelId == 0)
            {
                interfaceContentString = "This channel configuration has an internal interface.";
            }
            else
            {
                var interfaceChannel = await context.Guild.GetChannelAsync(record.InterfaceChannelId);
                interfaceContentString = "The set interface channel for this configuration is:\n" + interfaceChannel.Mention;
            }
            
            if (record.LoungeNamePattern == null || record.DecoratorPattern == null)
                return;
            
            var embedBuilder = new DiscordEmbedBuilder()
                .WithTitle(targetChannel.Mention)
                .AddField("Target Channel Id:", record.TargetChannelId.ToString())
                .AddField("Interface Channel Details:", interfaceContentString)
                .AddField("The set Name Pattern is: ", record.LoungeNamePattern)
                .AddField("The set Name Decorator is:", record.DecoratorPattern)
                .Build();
            
            embedList.Add(embedBuilder);
            
            selectComponentOptions.Add(new DiscordSelectComponentOption(targetChannel.Name, targetChannel.Id.ToString() ));
        }

        await context.RespondAsync(new DiscordMessageBuilder()
            .WithContent("I found the following configurations for this Guild:")
            .AddEmbeds(embedList)
            .AddComponents(new DiscordSelectComponent(CustomComponentIdHelper.LoungeConfig.EntrySelector, "Select a channel here for more options", selectComponentOptions, maxOptions: 1)));

    }
}