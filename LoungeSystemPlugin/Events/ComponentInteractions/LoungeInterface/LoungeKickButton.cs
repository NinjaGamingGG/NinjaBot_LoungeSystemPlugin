using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class LoungeKickButton
{
    internal static async Task DropdownInteraction(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember owningMember)
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
        
        //Owner cant be kicked
        if (!existsAsOwner)
            return;

        await eventArgs.Message.DeleteAsync();
        
        var selectedUserIds = eventArgs.Interaction.Data.Values.ToList();

        foreach (var userId in selectedUserIds)
        {
            var user = await eventArgs.Guild.GetMemberAsync(ulong.Parse(userId));
            
            var afkChannel = await eventArgs.Guild.GetAfkChannelAsync();

            await user.ModifyAsync(delegate (MemberEditModel kick)
            {
                kick.VoiceChannel = afkChannel!;
            });
        }
    }

    internal static async Task ButtonInteraction(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember owningMember)
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

        var membersInChannel = targetChannel.Users;

        var optionsList = new List<DiscordSelectComponentOption>();

        foreach (var channelMember in membersInChannel)
        {
            optionsList.Add(new DiscordSelectComponentOption("@"+channelMember.DisplayName, channelMember.Id.ToString()));
        }
        
        var sortedList = optionsList.OrderBy(x => x.Label);
        
        var dropdown = new DiscordSelectComponent(CustomComponentIdHelper.LoungeInterface.KickDropdownId, "Please select an user", sortedList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(dropdown);
        
        await ThrowAwayFollowupMessage.HandleAsync(followUpMessageBuilder, eventArgs.Interaction);
    }
}