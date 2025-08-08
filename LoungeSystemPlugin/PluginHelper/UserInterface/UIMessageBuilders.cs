using DSharpPlus.Entities;

namespace LoungeSystemPlugin.PluginHelper.UserInterface;

// ReSharper disable once InconsistentNaming
public static class UIMessageBuilders
    {
        private const string UiBaseMessageContent =
            ":desktop: **Lounge Setup UI.**\n*This UI will guide you trough the Lounge Setup Process.*\n*This UI will get invalid after 15 Minutes!*\n\n";
    
        private const string NoPermissionsMessage = ":x: You do not have permission to do this.";
    
        public static readonly DiscordMessageBuilder InitialMessageBuilder = new DiscordMessageBuilder()
            .WithContent(UiBaseMessageContent +
                     "Please select a Channel Users will enter in order to create a Lounge / Temporary VC")
            .AddActionRowComponent(
                new DiscordChannelSelectComponent(CustomComponentIdHelper.LoungeSetup.ChannelSelector, "Target Channel",[DiscordChannelType.Voice]));
    
        public static readonly DiscordInteractionResponseBuilder ChannelSelectedMessageBuilder = new DiscordInteractionResponseBuilder()
            .WithContent(UiBaseMessageContent +
                     ":white_check_mark: Channel Selected\n:point_down: Please click the Button Below to Enter the new Channel Name Pattern that will be used for new Channels")
            .AddActionRowComponent([
                new DiscordButtonComponent(DiscordButtonStyle.Primary,CustomComponentIdHelper.LoungeSetup.NamePatternButton,"Set Name Pattern")]);

        public static readonly DiscordInteractionResponseBuilder ChannelNamePatternModalBuilder =
            new DiscordInteractionResponseBuilder()
                .WithTitle("Set your Channel Name Pattern")
                .WithCustomId(CustomComponentIdHelper.LoungeSetup.NamePatternModal)
                .AddTextInputComponent(new DiscordTextInputComponent("Name Pattern",CustomComponentIdHelper.LoungeSetup.NamePatternModalNameComponent,"For example use {username}'s Lounge"))
                .AddTextInputComponent(new DiscordTextInputComponent("Decorator",CustomComponentIdHelper.LoungeSetup.NamePatternModalDecoratorComponent,"Displayed before the Name. For Example use: ~🗿»"));
        
        public static readonly DiscordInteractionResponseBuilder ChannelNamePatternRenameModalBuilder =
            new DiscordInteractionResponseBuilder()
                .WithTitle("Set your Channel Name Pattern")
                .WithCustomId(CustomComponentIdHelper.LoungeConfig.ResetPatternModal)
                .AddTextInputComponent(new DiscordTextInputComponent("Name Pattern",CustomComponentIdHelper.LoungeConfig.ResetPatternModalNameComponent,"For example use {username}'s Lounge"))
                .AddTextInputComponent(new DiscordTextInputComponent("Decorator",CustomComponentIdHelper.LoungeConfig.ResetPatternModalDecoratorComponent,"Displayed before the Name. For Example use: ~🗿»"));
    
    
        public static readonly DiscordInteractionResponseBuilder ModalSubmittedResponseBuilder = 
            new DiscordInteractionResponseBuilder()
                .WithContent(UiBaseMessageContent +
                         ":white_check_mark: Name Pattern Set\n:point_down: Please select below whether you want the Lounge Interface in a Separate Channel or in the Lounge's Chat")
                .AddActionRowComponent(
                    new DiscordSelectComponent(CustomComponentIdHelper.LoungeSetup.InterfaceSelector, "Click to select" , new List<DiscordSelectComponentOption>()
                    {
                        new("Separate", CustomComponentIdHelper.LoungeSetup.InterfaceOptionSeparate),
                        new("Internal", CustomComponentIdHelper.LoungeSetup.InterfaceOptionInternal)
                    }));
    
        public static readonly DiscordInteractionResponseBuilder InterfaceSelectedResponseBuilder = 
            new DiscordInteractionResponseBuilder()
                .WithContent(UiBaseMessageContent +
                         ":white_check_mark: Separate Interface Selected\n:point_down: Please select below which Channel should be used for the interface")
                .AddActionRowComponent(
                    new DiscordChannelSelectComponent(CustomComponentIdHelper.LoungeSetup.InterfaceChannelSelector, "Interface Channel",[DiscordChannelType.Text]));

        public static readonly DiscordInteractionResponseBuilder LoungeSetupComplete =
            new DiscordInteractionResponseBuilder()
                .WithContent(UiBaseMessageContent +
                            ":trophy: Setup Completed Successfully!\nNow you can use your new Lounge Channel");

        public static DiscordInteractionResponseBuilder InteractionFailedResponseBuilder(string errorMessage)
        {
            return new DiscordInteractionResponseBuilder()
                .WithContent("<:Aheto:1286757430381903973> A error occured during the interaction:\n\n"+errorMessage);
        }

    
        public static readonly DiscordInteractionResponseBuilder NoPermissionsResponseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent(NoPermissionsMessage);
    
        public static readonly DiscordMessageBuilder NoPermissionMessageBuilder = new DiscordMessageBuilder().WithContent(NoPermissionsMessage);
        
        public static readonly DiscordInteractionResponseBuilder NotInChannelResponseBuilder = new DiscordInteractionResponseBuilder()
            .WithContent(":x: You have to be in a lounge channel to use this");

        public static readonly DiscordMessageBuilder
            NoConfigurationsFound = new DiscordMessageBuilder().WithContent("No configurations found for this guild!\nTry setting up a new one using '/lounge setup'");

        public static DiscordInteractionResponseBuilder LoungeConfigSelectedResponseBuilder(string channelMention, bool canResetInterface = false)
        {
            List<DiscordButtonComponent> components = [];
            
            if (canResetInterface)
                components.Add(new DiscordButtonComponent(DiscordButtonStyle.Secondary, CustomComponentIdHelper.LoungeConfig.Reset, "Reset Interface", emoji: new DiscordComponentEmoji("\ud83d\udd03")));
                   
            components.Add(new DiscordButtonComponent(DiscordButtonStyle.Secondary, CustomComponentIdHelper.LoungeConfig.UpdateNamePattern, "Update Name Pattern"));
                    
            components.Add(new DiscordButtonComponent(DiscordButtonStyle.Danger, CustomComponentIdHelper.LoungeConfig.Delete, "Delete Configuration", emoji: new DiscordComponentEmoji("\u2716")));
            
            //Reset Interface
            return new DiscordInteractionResponseBuilder()
                .WithContent($"You selected the configuration for:\n{channelMention}")
                .AddActionRowComponent(components);
        }
    }