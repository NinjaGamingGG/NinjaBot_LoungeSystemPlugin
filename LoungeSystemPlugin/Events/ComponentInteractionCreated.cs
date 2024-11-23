using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.Events.ComponentInteractions.LoungeConfigEditor;
using LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;
using LoungeSystemPlugin.Events.ComponentInteractions.LoungeSetupUi;
using LoungeSystemPlugin.PluginHelper;
using Serilog;


namespace LoungeSystemPlugin.Events;

public static class ComponentInteractionCreated
{
    public static async Task InterfaceButtonPressed(DiscordClient sender, ComponentInteractionCreatedEventArgs eventArgs)
    {
        if (ReferenceEquals(eventArgs.User, null))
        {
            await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
            return;
        }


        var member = await eventArgs.Guild.GetMemberAsync(eventArgs.User.Id);
        
        switch (eventArgs.Interaction.Data.CustomId)
        {
            case CustomComponentIdHelper.LoungeInterface.Buttons.RenameButtonId:
                await RenameButton.ButtonInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.Buttons.ResizeButtonId:
                await LoungeResizeButton.ButtonInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.Buttons.TrustButtonId:
                await LoungeTrustUserButton.ButtonInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.Buttons.UnTrustButtonId:
                await LoungeUnTrustUserButton.ButtonInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.Buttons.ClaimButtonId:
                await LoungeClaimButton.ButtonInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.Buttons.KickButtonId:
                await LoungeKickButton.ButtonInteraction(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.Buttons.LockButtonId:
                await LoungeLockButton.ButtonInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.Buttons.BanButtonId:
                await LoungeBanButton.ButtonInteracted(eventArgs, member);
                break;
                
            case CustomComponentIdHelper.LoungeInterface.Buttons.DeleteButtonId:
                await LoungeDeleteButtonLogic.ButtonInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.BanDropdownId:
                await LoungeBanButton.DropdownInteracted (eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.KickDropdownId:
                await LoungeKickButton.DropdownInteraction(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.ResizeDropdownId:
                await LoungeResizeButton.DropdownInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.UnTrustSelectComponentId:
                await LoungeUnTrustUserButton.DropdownInteracted(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeInterface.TrustSelectComponentId:
                await LoungeTrustUserButton.UserSelected(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeSetup.ChannelSelector:
                await LoungeSetupChannelSelect.ChannelSelected(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeSetup.NamePatternButton:
                await LoungeSetupNamePatternButton.ButtonPressed(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeSetup.InterfaceSelector:
                await LoungeSetupInterfaceSelector.SelectionMade(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeSetup.InterfaceChannelSelector:
                await LoungeSetupInterfaceChannelSelection.SelectionMade(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeConfig.EntrySelector:
                await LoungeConfigurationSelected.ChannelSelectionMade(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeConfig.Reset:
                await LoungeConfigurationSelected.ResetInterfaceButton(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeConfig.UpdateNamePattern:
                await LoungeConfigurationSelected.ResetNamePatternButton(eventArgs, member);
                break;
            
            case CustomComponentIdHelper.LoungeConfig.Delete:
                await LoungeConfigurationSelected.DeleteButton(eventArgs, member);
                break;
            
            default:
                Log.Debug("[{PluginName}] Unknown Component Id: {ComponentId} in ComponentInteractionCreatedEvent",LoungeSystemPlugin.GetStaticPluginName,eventArgs.Interaction.Data.CustomId);
                await eventArgs.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);
                return;
        }
    }
}