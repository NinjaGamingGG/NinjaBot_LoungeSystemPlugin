using DSharpPlus.Entities;

namespace LoungeSystemPlugin.PluginHelper;

public static class ThrowAwayFollowupMessage
{
    public static async Task HandleAsync(DiscordFollowupMessageBuilder builder, DiscordInteraction interaction, int waitDelay = 20)
    {
        var followupMessage = await interaction.CreateFollowupMessageAsync(builder);
        
        HandleAndDelete(followupMessage, waitDelay);
    }

    public static void Handle(DiscordMessage followupMessage, int waitDelay = 15)
    {
        HandleAndDelete(followupMessage, waitDelay);
    }

    private static void HandleAndDelete(DiscordMessage message, int waitDelay)
    {
        var waitTask = Task.Delay(TimeSpan.FromSeconds(waitDelay));

        while (!waitTask.IsCompleted)
        {
        }
        
        try
        {
            message.DeleteAsync();
        }
        catch (DSharpPlus.Exceptions.NotFoundException)
        {
            //Do nothing
        }
    }
}