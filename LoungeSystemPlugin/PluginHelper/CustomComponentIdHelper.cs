namespace LoungeSystemPlugin.PluginHelper;

public static class CustomComponentIdHelper
{
    public static class LoungeInterface
    {
        public const string BanDropdownId = "lounge_ban_dropdown";
        public const string KickDropdownId = "lounge_kick_dropdown";
        public const string ResizeDropdownId = "lounge_resize_dropdown";
        public const string ResizeLabel = "lounge_resize_label_";
        public const string TrustSelectComponentId = "lounge_trust_user-selection";
        public const string UnTrustSelectComponentId = "lounge_un-trust_dropdown";
        
        public static class Buttons
        {
            public const string RenameButtonId = "lounge_rename_button";
            public const string DeleteButtonId = "lounge_delete_button";
            public const string ResizeButtonId = "lounge_resize_button";
            public const string BanButtonId = "lounge_ban_button";
            public const string KickButtonId = "lounge_kick_button";
            public const string TrustButtonId = "lounge_trust_button";
            public const string UnTrustButtonId = "lounge_untrust_button";
            public const string ClaimButtonId = "lounge_claim_button";
            public const string LockButtonId = "lounge_lock_button";
        }

        
    }

    public static class LoungeConfig
    {
        public const string Reset = "lounge_config_reset";
        public const string UpdateNamePattern = "lounge_config_update_name_pattern";
        public const string Delete = "lounge_config_delete";
        public const string EntrySelector = "lounge_config_selector";
        public const string ResetPatternModal = "lounge_config_reset-pattern_modal";
        public const string ResetPatternModalNameComponent = "lounge_config_reset-pattern_modal_name";
        public const string ResetPatternModalDecoratorComponent = "lounge_config_reset-pattern_modal_decorator";
    }

    public static class LoungeSetup
    {
        public const string ChannelSelector = "lounge_setup_channel_select";
        public const string NamePatternButton = "lounge_setup_name_pattern_button";
        public const string NamePatternModal = "lounge_setup_name_pattern_modal";
        public const string NamePatternModalNameComponent = "lounge_setup_name_pattern_modal_name";
        public const string NamePatternModalDecoratorComponent = "lounge_setup_name_pattern_modal_decorator";
        public const string InterfaceSelector = "lounge_setup_interface_selector";
        public const string InterfaceOptionSeparate = "lounge_interface_separate";
        public const string InterfaceOptionInternal = "lounge_interface_internal";
        public const string InterfaceChannelSelector = "lounge_setup_interface_channel_select";
    }

    public const string LoungeRenameModalId = "lounge_rename_modal";
    public const string LoungeRenameModalNewName = "lounge_new_name";



}