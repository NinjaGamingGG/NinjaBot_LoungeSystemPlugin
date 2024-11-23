using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;
using Serilog;

namespace LoungeSystemPlugin.Events.ModalsSubmitted;

public static class LoungeRenameModal
{
    public static async Task WasSubmitted(DiscordClient sender, ModalSubmittedEventArgs eventArgs)
    {
        await eventArgs.Interaction.DeferAsync();
        
        var isValuePresent = eventArgs.Values.TryGetValue(CustomComponentIdHelper.LoungeRenameModalNewName, out var userValue);
        
        if (isValuePresent == false || string.IsNullOrEmpty(userValue) )
            return;
        
        bool containsProfanity;
        
        try
        {
            containsProfanity  = await ProfanityCheck.CheckString(userValue);
        }
        catch (Exception ex)
        {
            Log.Error(ex,"[{PluginName}] Error while checking for Profanity", LoungeSystemPlugin.GetStaticPluginName());
            await eventArgs.Interaction.EditOriginalResponseAsync(
                new DiscordWebhookBuilder()
                    .WithContent("Interaction Failed, please try again later."));
            return;
        }

        if (containsProfanity)
        {
            await eventArgs.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Unable to change the name to the Provided string due to Profanity."));
            return;
        }
        
        if (ReferenceEquals(eventArgs.Interaction.Guild, null))
            return;

        var newChannelName = await ChannelNameBuilder.BuildAsync(eventArgs.Interaction.Guild.Id, eventArgs.Interaction.Channel.Id,
            userValue);
        
        var channel = await sender.GetChannelAsync(eventArgs.Interaction.Channel.Id);
        await channel.ModifyAsync(NewEditModel);
        
        await eventArgs.Interaction.DeleteOriginalResponseAsync();
        return;

        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Name = newChannelName;
        }
        

    }
    

    
}