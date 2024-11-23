using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class LoungeTrustUserButton
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

        if (existsAsOwner == false)
            return;
        
        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Please select an user below").AddComponents(new DiscordUserSelectComponent(CustomComponentIdHelper.LoungeInterface.TrustSelectComponentId,""));
        
        await ThrowAwayFollowupMessage.HandleAsync(followUpMessageBuilder, eventArgs.Interaction);
    }
    
    public static async Task UserSelected(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
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

        foreach (var selectedUserId in selectedUserIds)
        {
            var selectedUser = await eventArgs.Guild.GetMemberAsync(ulong.Parse(selectedUserId));

            var overwriteBuilderList = new List<DiscordOverwriteBuilder>
            {
                new DiscordOverwriteBuilder(selectedUser)
                    .Allow(DiscordPermissions.AccessChannels)
                    .Allow(DiscordPermissions.SendMessages)
                    .Allow(DiscordPermissions.UseVoice)
                    .Allow(DiscordPermissions.Speak)
                    .Allow(DiscordPermissions.Stream)
            };

            var existingOverwrites = targetChannel.PermissionOverwrites;

            foreach (var overwrite in existingOverwrites)
            {
                overwriteBuilderList.Add(await new DiscordOverwriteBuilder(selectedUser).FromAsync(overwrite));
            }
            
            
            await targetChannel.ModifyAsync(x => x.PermissionOverwrites = overwriteBuilderList);
            
            await eventArgs.Interaction.DeleteOriginalResponseAsync();
        }
    }
}