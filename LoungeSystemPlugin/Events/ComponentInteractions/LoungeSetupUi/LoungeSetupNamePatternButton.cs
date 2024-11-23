using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.UserInterface;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;

public static class LoungeSetupNamePatternButton
{
    internal static async Task ButtonPressed(ComponentInteractionCreatedEventArgs eventArgs, DiscordMember member)
    {
        //Check if User has Admin Permissions
        if (!member.Permissions.HasPermission(DiscordPermissions.Administrator))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,UIMessageBuilders.NoPermissionsResponseBuilder);
            return;
        }
        
        //Update the message this Button was Attached to
        await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal,
            UIMessageBuilders.ChannelNamePatternModalBuilder);
    }
}