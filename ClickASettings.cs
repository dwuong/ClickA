using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ClickA
{
    public class ClickASettings : ISettings
    {
        [Menu("Enable Plugin", "Toggle to enable or disable the ClickA plugin (master switch).")]
        public ToggleNode Enable { get; set; } = new ToggleNode(true);

        [Menu("Toggle Pause Hotkey", "Press this key to pause or unpause the plugin's item clicking operations.")]
        public HotkeyNode TogglePauseHotkey { get; set; } = new HotkeyNode(Keys.XButton2);

        [Menu("Is Paused", "Indicates if the plugin is currently paused. Can also be toggled via hotkey.")]
        public ToggleNode IsPaused { get; set; } = new ToggleNode(false);

        [Menu("Action Cooldown (ms)", "Delay between pickup attempts to prevent spamming. Recommended: 100-200ms.")]
        public RangeNode<int> ActionCooldown { get; set; } = new RangeNode<int>(150, 50, 1000);

        [Menu("Pickup Filter", "Comma-separated list of item metadata keywords to pick up. Examples: \"currency, map, divination, unique, item/armours/bodyarmours\"")]
        public TextNode Filter { get; set; } = new TextNode("/stash");

        [Menu("Pickup Range", "Maximum distance from player to pick up items (in-game units).")]
        public RangeNode<float> Range { get; set; } = new RangeNode<float>(500f, 50f, 1000f);

        [Menu("Item Click Y Offset", "Adjusts the Y-coordinate for clicking item labels. Useful if clicks miss or hit wrong part of label.")]
        public RangeNode<int> ItemClickYOffset { get; set; } = new RangeNode<int>(0, -20, 20); // CORRECTED LINE HERE

        [Menu("Left Panel Key Press", "Enable or disable sending a key when the left UI panel becomes visible and the plugin pauses.")]
        public ToggleNode EnableLeftPanelKeyPress { get; set; } = new ToggleNode(true);

        [Menu("Key to Press on Left Panel", "Choose which key to press 1 second after the left UI panel becomes visible.")]
        public HotkeyNode KeyOnLeftPanelVisible { get; set; } = new HotkeyNode(Keys.XButton2);

        // New setting for hideout click
        [Menu("Enable One-Time Hideout Click", "If enabled, the plugin will perform a single click when entering a hideout.")]
        public ToggleNode EnableOneTimeHideoutClick { get; set; } = new ToggleNode(false);
    }
}
