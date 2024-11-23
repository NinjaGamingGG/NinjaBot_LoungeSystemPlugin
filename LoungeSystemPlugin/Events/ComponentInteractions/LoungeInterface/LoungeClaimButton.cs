using Dapper;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using MySqlConnector;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class LoungeClaimButton
{
    internal static async Task ButtonInteracted(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        var targetChannel = await InterfaceTargetHelper.GetTargetDiscordChannelAsync(eventArgs.Channel, member);

        if (ReferenceEquals(targetChannel, null))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                UIMessageBuilders.NotInChannelResponseBuilder);
            return;
        }
        
        await eventArgs.Interaction.DeferAsync();

        var existsAsOwner = await LoungeOwnerCheck.IsLoungeOwnerAsync(member, targetChannel, eventArgs.Guild);
        
        //Only non owners can claim
        if (existsAsOwner)
            return;

        var ownerId = await LoungeOwnerCheck.GetOwnerIdAsync(targetChannel);
        
        var membersInChannel = targetChannel.Users;

        var isOwnerPresent = false;
        
        foreach (var discordMember in membersInChannel)
        {
            if (discordMember.Id == ownerId)
                isOwnerPresent = true;
        }

        if (isOwnerPresent)
            return;
        
        var builder = new DiscordFollowupMessageBuilder().WithContent(eventArgs.User.Mention + " please wait, claiming channel for you");
        
        var followupMessage = await eventArgs.Interaction.CreateFollowupMessageAsync(builder);

        var connectionString = LoungeSystemPlugin.MySqlConnectionHelper.GetMySqlConnectionString();
        int updateCount;
        try
        {
            var mySqlConnection = new MySqlConnection(connectionString);
            await mySqlConnection.OpenAsync();
        
            updateCount = await mySqlConnection.ExecuteAsync(
                "UPDATE LoungeIndex SET OwnerId = @OwnerId WHERE ChannelId = @ChannelId",
                new {OwnerId = eventArgs.User.Id, ChannelId = targetChannel.Id});

            await mySqlConnection.CloseAsync();
        }
        catch (MySqlException ex)
        {
            Log.Error(ex, "Unable to Change owner of Lounge in LoungeSystem Plugin. Guild: {GuildName}/{GuildId}, Channel: {ChannelName}/{ChannelId}",
                eventArgs.Guild.Name, eventArgs.Guild.Id, targetChannel.Name, targetChannel.Id); 
            return;
        }

        
        if (updateCount == 0)
        {
            await followupMessage.ModifyAsync("Failed to claim Lounge, please try again later");
            return;
        }
        
        await followupMessage.ModifyAsync("Lounge claimed successfully, you are now the owner");

        ThrowAwayFollowupMessage.Handle(followupMessage);
        
        await eventArgs.Interaction.DeleteOriginalResponseAsync();


    }
}