using DSharpPlus;
using DSharpPlus.EventArgs;
using LoungeSystemPlugin.Events.ModalsSubmitted;
using LoungeSystemPlugin.PluginHelper;


namespace LoungeSystemPlugin.Events;

internal static class ModalSubmitted
{
    internal static async Task ModalSubmittedHandler(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        switch (eventArgs.Interaction.Data.CustomId)
        {
                case CustomComponentIdHelper.LoungeRenameModalId:
                    await LoungeRenameModal.WasSubmitted(sender, eventArgs);
                    break;
                
                case CustomComponentIdHelper.LoungeSetup.NamePatternModal:
                    await LoungeSetupUiModal.ModalSubmitted(sender, eventArgs);
                    break;
                
                case CustomComponentIdHelper.LoungeConfig.ResetPatternModal:
                    await LoungeConfigResetNameModal.ModalSubmitted(sender, eventArgs);
                    break;
        }
        

    }
}