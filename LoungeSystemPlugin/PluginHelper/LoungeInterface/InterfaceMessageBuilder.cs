using DSharpPlus;
using DSharpPlus.Entities;

namespace LoungeSystemPlugin.PluginHelper.LoungeInterface;

/// <summary>
/// Provides methods to build interface messages for the lounge system plugin.
/// </summary>
public static class InterfaceMessageBuilder
{
    /// <summary>
    /// Returns a DiscordMessageBuilder instance with pre-configured message content and button components.
    /// </summary>
    /// <param name="client">The DiscordClient instance used to construct button components.</param>
    /// <param name="messageContent">The message content to be set for the DiscordMessageBuilder instance.</param>
    /// <returns>A DiscordMessageBuilder instance with pre-configured message content and button components.</returns>
    public static DiscordMessageBuilder GetBuilder(DiscordClient client,string messageContent)
    {
       return new DiscordMessageBuilder()
                .WithContent(messageContent)
                .AddActionRowComponent([
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, CustomComponentIdHelper.LoungeInterface.Buttons.RenameButtonId,
                          "Rename",false, new DiscordComponentEmoji( DiscordEmoji.FromName(client, ":black_nib:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, CustomComponentIdHelper.LoungeInterface.Buttons.ResizeButtonId,
                        "Resize", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":busts_in_silhouette:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, CustomComponentIdHelper.LoungeInterface.Buttons.TrustButtonId,
                        "Trust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":people_hugging:")))
                ])
                .AddActionRowComponent([
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, CustomComponentIdHelper.LoungeInterface.Buttons.ClaimButtonId,
                        "Claim", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":triangular_flag_on_post:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, CustomComponentIdHelper.LoungeInterface.Buttons.KickButtonId,
                        "Kick", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":athletic_shoe:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Secondary, CustomComponentIdHelper.LoungeInterface.Buttons.UnTrustButtonId,
                        "Un-Trust", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":bust_in_silhouette:")))
                ])
                .AddActionRowComponent([
                    new DiscordButtonComponent(DiscordButtonStyle.Danger, CustomComponentIdHelper.LoungeInterface.Buttons.LockButtonId,
                        "Un/Lock", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":lock:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Danger, CustomComponentIdHelper.LoungeInterface.Buttons.BanButtonId,
                        "Ban", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":judge:"))),
                    new DiscordButtonComponent(DiscordButtonStyle.Danger, CustomComponentIdHelper.LoungeInterface.Buttons.DeleteButtonId,
                        "Delete", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":put_litter_in_its_place:")))
                ]);
    }
}