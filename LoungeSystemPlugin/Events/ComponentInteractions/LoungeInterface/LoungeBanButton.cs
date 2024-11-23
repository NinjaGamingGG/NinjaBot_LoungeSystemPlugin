using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class LoungeBanButton
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
        
        //Only non owners can ban
        if (!existsAsOwner)
            return;

        var membersInChannel = targetChannel.Users;

        var optionsList = membersInChannel.Select(channelMember => new DiscordSelectComponentOption("@" + channelMember.DisplayName, channelMember.Id.ToString())).ToList();

        var sortedList = optionsList.OrderBy(x => x.Label);
        
        var dropdown = new DiscordSelectComponent(CustomComponentIdHelper.LoungeInterface.BanDropdownId, "Please select an user to ban (from channel)", sortedList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(dropdown);
        
        await ThrowAwayFollowupMessage.HandleAsync(followUpMessageBuilder, eventArgs.Interaction);
    }
    
    internal static async Task DropdownInteracted(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember owningMember)
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
        
        //Only owner can ban
        if (!existsAsOwner)
            return;

        await eventArgs.Message.DeleteAsync();
        
        var selectedUserIds = eventArgs.Interaction.Data.Values.ToList();

        var selectedUsersAsDiscordMember = new List<DiscordMember>();

        foreach (var userIdAsUlong in selectedUserIds.Select(ulong.Parse))
        {
            selectedUsersAsDiscordMember.Add(await eventArgs.Guild.GetMemberAsync(userIdAsUlong));
        }

        var existingOverwrites = targetChannel.PermissionOverwrites.ToList();

        var overwriteBuilderList = new List<DiscordOverwriteBuilder>();
        
        foreach (var overwrite in existingOverwrites)
        {
            if (overwrite.Type == DiscordOverwriteType.Member && selectedUsersAsDiscordMember.Contains(await overwrite.GetMemberAsync()))
                continue;
            
            if (overwrite.Type == DiscordOverwriteType.Member)
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(await overwrite.GetMemberAsync()).FromAsync(overwrite));
            
            if (overwrite.Type == DiscordOverwriteType.Role)
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(await overwrite.GetRoleAsync()).FromAsync(overwrite));
        }

        overwriteBuilderList.AddRange(selectedUsersAsDiscordMember.Select(member => new DiscordOverwriteBuilder(member).Allow(DiscordPermissions.AccessChannels)
            .Deny(DiscordPermissions.SendMessages)
            .Deny(DiscordPermissions.UseVoice)
            .Deny(DiscordPermissions.Speak)
            .Deny(DiscordPermissions.Stream)));
        
        await targetChannel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);
        
        await eventArgs.Interaction.DeleteOriginalResponseAsync();

    }
    
}