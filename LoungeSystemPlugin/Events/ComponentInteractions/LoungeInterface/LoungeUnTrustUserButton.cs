using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class LoungeUnTrustUserButton
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

        if (existsAsOwner == false)
            return;
        
        var optionsList = new List<DiscordSelectComponentOption>();

        var channelOverwrites = targetChannel.PermissionOverwrites;

        foreach (var overwriteEntry in channelOverwrites)
        {
            if (overwriteEntry.Type == DiscordOverwriteType.Role)
                continue;
            
            //Check if User is Owner / command sender
            if (overwriteEntry.Id == owningMember.Id)
                continue;
            
            var memberInChannel = await eventArgs.Guild.GetMemberAsync(overwriteEntry.Id);
            
            optionsList.Add(new DiscordSelectComponentOption("@"+memberInChannel.DisplayName, memberInChannel.Id.ToString()));
        }
        
        var sortedList = optionsList.OrderBy(x => x.Label);
        
        var dropdown = new DiscordSelectComponent(CustomComponentIdHelper.LoungeInterface.UnTrustSelectComponentId, "Please select an user", sortedList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(dropdown);

        await ThrowAwayFollowupMessage.HandleAsync(followUpMessageBuilder, eventArgs.Interaction);
    }
    
    internal static async Task DropdownInteracted(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
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

        if (existsAsOwner == false)
            return;
        
        var interactionId = eventArgs.Message.Id;
        var message = await targetChannel.GetMessageAsync(interactionId);
        await message.DeleteAsync();
        
        var selectedUserIds = eventArgs.Interaction.Data.Values.ToList();
        
        var existingOverwrites = targetChannel.PermissionOverwrites;

        var overwriteBuilderList = new List<DiscordOverwriteBuilder>();
        
        foreach (var existingOverwrite in existingOverwrites)
        {
            if (selectedUserIds.Contains(existingOverwrite.Id.ToString()))
                continue;


            if (existingOverwrite.Type == DiscordOverwriteType.Role)
            {
                var role = eventArgs.Guild.GetRole(existingOverwrite.Id);

                if (ReferenceEquals(role, null))
                {
                    Log.Error("[{PluginName}] Unable to get role with id {RoleId} During Dropdown Interacted Login in LoungeUnTrustUserButton", LoungeSystemPlugin.GetStaticPluginName(), existingOverwrite.Id);
                    continue;
                }
                    
                
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(role).FromAsync(existingOverwrite));
            }
            else
            {
                var user = await eventArgs.Guild.GetMemberAsync(existingOverwrite.Id);
                
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(user).FromAsync(existingOverwrite));
            }
        }
        
        await targetChannel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);
        await eventArgs.Interaction.DeleteOriginalResponseAsync();
    }
}