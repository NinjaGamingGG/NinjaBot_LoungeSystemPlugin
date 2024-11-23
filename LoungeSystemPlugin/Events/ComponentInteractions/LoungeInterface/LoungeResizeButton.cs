using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Net.Models;
using LoungeSystemPlugin.PluginHelper;
using LoungeSystemPlugin.PluginHelper.LoungeInterface;
using LoungeSystemPlugin.PluginHelper.UserInterface;
using Serilog;

namespace LoungeSystemPlugin.Events.ComponentInteractions.LoungeInterface;

public static class LoungeResizeButton
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
        
        const int maxChannelSize = 25;
        const int minChannelSize = 2;
        
        var optionsList = new List<DiscordSelectComponentOption>();
        
        for (var i = minChannelSize; i < maxChannelSize; i++)
        {
            if (i == 4)
            {
                optionsList.Add(new DiscordSelectComponentOption(i.ToString(),CustomComponentIdHelper.LoungeInterface.ResizeLabel+ i,isDefault: true));
                continue;
            }
            
            optionsList.Add(new DiscordSelectComponentOption(i.ToString(),CustomComponentIdHelper.LoungeInterface.ResizeLabel+ i));
        }
        
        var dropdown = new DiscordSelectComponent(CustomComponentIdHelper.LoungeInterface.ResizeDropdownId, "Select a new Size Below", optionsList);

        var followUpMessageBuilder = new DiscordFollowupMessageBuilder().WithContent("Select a new Size Below").AddComponents(dropdown);

        await ThrowAwayFollowupMessage.HandleAsync(followUpMessageBuilder, eventArgs.Interaction);

        await eventArgs.Interaction.DeleteOriginalResponseAsync();
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

        var newSizeString = eventArgs.Interaction.Data.Values[0].Replace(CustomComponentIdHelper.LoungeInterface.ResizeLabel, "");
        
        var parseSuccess = int.TryParse(newSizeString, out var parseResult);
        
        if (parseSuccess == false)
        {
            Log.Error("Failed to parse new size for lounge");
            return;
        }
        
        var channel = targetChannel;
        
        if (ReferenceEquals(channel, null))
            return;
        
        void NewEditModel(ChannelEditModel editModel)
        {
            editModel.Userlimit = parseResult;
        }

        await channel.ModifyAsync(NewEditModel);

        await eventArgs.Interaction.DeleteOriginalResponseAsync();
    }
}