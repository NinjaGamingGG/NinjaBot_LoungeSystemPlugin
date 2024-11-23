using Dapper;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class LoungeLockButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember owningMember)
    {
        var targetChannel = await InterfaceTargetHelper.GetTargetDiscordChannelAsync(eventArgs.Channel, owningMember);

        if (ReferenceEquals(targetChannel, null))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                UIMessageBuilders.NotInChannelResponseBuilder);
            return;
        }
        
        await eventArgs.Interaction.DeferAsync();
        
        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(owningMember, targetChannel, eventArgs.Guild);
        
        //Only non owners can kick
        if (!existsAsOwner)
            return;

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        bool[] isPublicAsArray;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();

            var isPublic =
                await mySqlConnection.QueryAsync<bool>(
                    "SELECT isPublic FROM LoungeIndex where GuildId=@GuildId and ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id, ChannelId= targetChannel.Id});

            await mySqlConnection.CloseAsync();

            isPublicAsArray = isPublic as bool[] ?? isPublic.ToArray();
        }
        catch (Exception ex)
        {
            Log.Error(ex,"Unable to retrieve lounge privacy state in LoungeSystem Lock Button Logic. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, targetChannel.Name, targetChannel.Id);
            throw;
        }
       
        if (isPublicAsArray.Length != 0 == false)
            Log.Error("[RankSystem] Unable to load isPublic variable for Channel {ChannelId} on Guild {GuildId}", targetChannel.Id, eventArgs.Guild.Id);

        if (isPublicAsArray[0] == false)
        {
            await UnLockLoungeLogic(eventArgs);
            return;
        }

        await LockLoungeLogic(eventArgs);

        await eventArgs.Interaction.DeleteOriginalResponseAsync();
    }

    private static async Task LockLoungeLogic(ComponentInteractionCreatedEventArgs eventArgs)
    {
        var discordMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        var targetChannel = await InterfaceTargetHelper.GetTargetDiscordChannelAsync(eventArgs.Channel, discordMember);

        if (ReferenceEquals(targetChannel, null))
        {
            return;
        }
        
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        List<ulong> requiresRolesList;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            var requiredRolesQueryResult =
                await mySqlConnection.QueryAsync<ulong>(
                    "SELECT RoleId FROM RequiredRoleIndex WHERE GuildId=@GuildId AND ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id,ChannelId = targetChannel.Id});

            requiresRolesList = requiredRolesQueryResult.ToList();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Error while retrieving required roles from database on Lounge System Lock Logic. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, targetChannel.Name,targetChannel.Id);
            return;
        }

        var lounge = targetChannel;

        var existingOverwrites = lounge.PermissionOverwrites;
        var overwriteBuilderList = new List<DiscordOverwriteBuilder>();
        
        foreach (var overwrite in existingOverwrites)
        {
            if (overwrite.Type == DiscordOverwriteType.Role)
                continue;
            
            var overwriteMember = await overwrite.GetMemberAsync();
            overwriteBuilderList.Add(await new DiscordOverwriteBuilder(overwriteMember).FromAsync(overwrite));
        }
        
        if (requiresRolesList.Count == 0)
            requiresRolesList.Add(eventArgs.Guild.EveryoneRole.Id);

        overwriteBuilderList.AddRange(requiresRolesList.Select(requiredRole => eventArgs.Guild.GetRole(requiredRole))
            .Select(role => new DiscordOverwriteBuilder(role!).Allow(DiscordPermissions.AccessChannels)
                .Deny(DiscordPermissions.SendMessages)
                .Deny(DiscordPermissions.UseVoice)
                .Deny(DiscordPermissions.Speak)
                .Deny(DiscordPermissions.Stream)));
        
        await targetChannel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            await mySqlConnection.ExecuteAsync("UPDATE LoungeIndex SET isPublic = FALSE WHERE GuildId=@GuildId AND ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id,ChannelId = targetChannel.Id});

            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Unable to change Lounge privacy state in Database on LoungeSystem Lock Logic Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, targetChannel.Name, targetChannel.Id);
        }

    }
    
    
    
    private static async Task UnLockLoungeLogic(ComponentInteractionCreatedEventArgs eventArgs)
    {
        var discordMember = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        var targetChannel = await InterfaceTargetHelper.GetTargetDiscordChannelAsync(eventArgs.Channel, discordMember);

        if (ReferenceEquals(targetChannel, null))
        {
            return;
        }
        var lounge = targetChannel;

        var existingOverwrites = lounge.PermissionOverwrites;
        var overwriteBuilderList = new List<DiscordOverwriteBuilder>();
        
        foreach (var overwrite in existingOverwrites)
        {
            if (overwrite.Type == DiscordOverwriteType.Role)
                continue;
            
            var overwriteMember = await overwrite.GetMemberAsync();
            overwriteBuilderList.Add(await new DiscordOverwriteBuilder(overwriteMember).FromAsync(overwrite));
        }
        
        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        
        List<ulong> requiresRolesList;
        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            var requiredRolesQueryResult =
                await mySqlConnection.QueryAsync<ulong>(
                    "SELECT RoleId FROM RequiredRoleIndex WHERE GuildId=@GuildId AND ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id,ChannelId = targetChannel.Id});

            await mySqlConnection.CloseAsync();
            requiresRolesList = requiredRolesQueryResult.ToList();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Unable to retrieve required roles for Lounge on Lounge System Unlock Button Logic. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, targetChannel.Name, targetChannel.Id);
            return;
        }
        
        if (requiresRolesList.Count == 0)
            requiresRolesList.Add(eventArgs.Guild.EveryoneRole.Id);
        else
            overwriteBuilderList.Add(new DiscordOverwriteBuilder(eventArgs.Guild.EveryoneRole)
                .Deny(DiscordPermissions.AccessChannels)
                .Deny(DiscordPermissions.SendMessages)
                .Deny(DiscordPermissions.UseVoice)
                .Deny(DiscordPermissions.Speak)
                .Deny(DiscordPermissions.Stream));

        overwriteBuilderList.AddRange(requiresRolesList.Select(requiredRole => eventArgs.Guild.GetRole(requiredRole))
            .Select(role => new DiscordOverwriteBuilder(role!).Allow(DiscordPermissions.AccessChannels)
                .Allow(DiscordPermissions.SendMessages)
                .Allow(DiscordPermissions.UseVoice)
                .Allow(DiscordPermissions.Speak)
                .Allow(DiscordPermissions.Stream)));

        await targetChannel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);

        try
        {
            await using var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
            
            await mySqlConnection.ExecuteAsync("UPDATE LoungeIndex SET isPublic = TRUE WHERE GuildId=@GuildId AND ChannelId=@ChannelId", new {GuildId = eventArgs.Guild.Id,ChannelId = targetChannel.Id});
            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex,"Unable to update privacy status of lounge in Lounge System unlock button logic. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, targetChannel.Name, targetChannel.Id); 
        }


    }
}