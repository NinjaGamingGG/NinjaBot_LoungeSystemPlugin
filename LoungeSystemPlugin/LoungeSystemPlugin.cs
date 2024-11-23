using LoungeSystemPlugin.Events;
using LoungeSystemPlugin.PluginHelper;
using NinjaBot_DC;
using CommonPluginHelpers;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using NRedisStack;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using PluginBase;
using Serilog;
using StackExchange.Redis;

namespace LoungeSystemPlugin;

// ReSharper disable once ClassNeverInstantiated.Global
public class LoungeSystemPlugin : DefaultPlugin
{
    public static MySqlConnectionHelper MySqlConnectionHelper { get; private set; } = null!;
    
    public static string RedisConnectionString { get; private set; } = "127.0.0.1";
    public static int RedisDatabase { get; private set; }

    private static string? _staticPluginName;
    public static string GetStaticPluginName()
    {
        return _staticPluginName ?? "Not Initialized";
    }

    private static void SetStaticPluginName(string pluginName)
    {
        _staticPluginName = pluginName;
    }
    
    public override void OnLoad()
    {
        if (ReferenceEquals(PluginDirectory, null))
        {
            OnUnload();
            return;
        }
        
        SetStaticPluginName(Name);
        
        Directory.CreateDirectory(PluginDirectory);

        var config = Worker.LoadAssemblyConfig(Path.Combine(PluginDirectory,"config.json"), GetType().Assembly, EnvironmentVariablePrefix);

        try
        {
            RedisConnectionString = config.GetValue<string>(EnvironmentVariablePrefix+":redis-connection-string") ?? "127.0.0.1";
            RedisDatabase = config.GetValue<int>(EnvironmentVariablePrefix + ":redis-database");
        }
        catch (Exception exception)
        {
            Log.Error(exception, "[{PluginName}] Failed to load redis connection string", Name);
            return;
        }

        try
        {
            var redisConnection = ConnectionMultiplexer.Connect(RedisConnectionString);
            var database = redisConnection.GetDatabase(RedisDatabase);
            var searchCommands = database.FT();
            
            CreateConfigurationsIndexJsonSchema(searchCommands);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[{PluginName}] Failed to initialize the Redis Connection", _staticPluginName);
        }
        
        var tableStrings = new[]
        {
            "CREATE TABLE IF NOT EXISTS LoungeSystemConfigurationIndex (Id INTEGER PRIMARY KEY AUTO_INCREMENT, GuildId BIGINT, TargetChannelId BIGINT, InterfaceChannelId BIGINT, LoungeNamePattern TEXT, DecoratorPattern TEXT)",
            "CREATE TABLE IF NOT EXISTS LoungeIndex (ChannelId BIGINT, GuildId BIGINT, OwnerId BIGINT, IsPublic BOOLEAN, OriginChannel BIGINT)",
            "CREATE TABLE IF NOT EXISTS RequiredRoleIndex (Id INTEGER PRIMARY KEY AUTO_INCREMENT, GuildId INTEGER, ChannelId BIGINT, RoleId INTEGER)"};
        
        MySqlConnectionHelper = new MySqlConnectionHelper(EnvironmentVariablePrefix, config, Name);
        
        try
        {
            var connectionString = MySqlConnectionHelper.GetMySqlConnectionString();
            using var connection = new MySqlConnection(connectionString);
            connection.Open();
            MySqlConnectionHelper.InitializeTables(tableStrings,connection);
            connection.Close();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex,"[{PluginName}] Canceling the Startup of Plugin!", Name);
            return;
        }
        
        var clientBuilder = Worker.GetDiscordClientBuilder();

        clientBuilder.ConfigureEventHandlers(
            builder => builder.HandleModalSubmitted(ModalSubmitted.ModalSubmittedHandler)
                .HandleVoiceStateUpdated(VoiceStateUpdated.ChannelEnter)
                .HandleVoiceStateUpdated(VoiceStateUpdated.ChannelLeave)
                .HandleComponentInteractionCreated(ComponentInteractionCreated.InterfaceButtonPressed));

        Task.Run(async () =>
        {
            await StartupCleanup.Execute();
        });

        Log.Information("[{PluginName}] Plugin Loaded", Name);
    }


    public override void OnUnload()
    {
        Log.Information("[{PluginName}] Plugin Unloaded", Name);
    }
    
    private static void CreateConfigurationsIndexJsonSchema(SearchCommands searchCommands)
    {
        const string indexName = "idx:setupInterfaces";
        try
        {
            var schema = new Schema()
                .AddTextField(new FieldName("$.channelId", "channelId"))
                .AddTextField(new FieldName("$.userId", "userId"))
                .AddTextField(new FieldName("$.namePattern", "namePattern"))
                .AddTextField(new FieldName("$.nameDecorator", "nameDecorator"));

            searchCommands.Create(
                indexName,
                new FTCreateParams().On(IndexDataType.JSON).Prefix("setupInterface:"), schema);
        }
        catch (RedisServerException ex)
        {
            if (ex.Message == "Index already exists")
            {
                Log.Debug("[{PluginName}] Index {indexName} already exists!",_staticPluginName, indexName);
                return;
            }

            Log.Error(ex, "[{PluginName}] Failed to create json schema", _staticPluginName);
        }
    }
}