using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using System.Drawing;
using System.Windows.Forms;

namespace ClickA
{
    public class ClickA : BaseSettingsPlugin<ClickASettings>
    {
        private List<string> _parsedITEMMetadataKeywords;
        private DateTime _nextAllowedActionTime = DateTime.Now;

        private bool _isHotkeyCurrentlyPressed = false;
        private bool _wasHotkeyPreviouslyPressed = false;

        private bool _wasLeftPanelVisibleLastTick = false;
        private DateTime _xButton2ActivationTime = DateTime.MinValue;

        private bool _hasClickedInCurrentHideout = false;

        private IngameUIElements IngameUi => GameController.IngameState.IngameUi;
        private Vector3 PlayerPos => GameController.Player.Pos;

        public override bool Initialise()
        {
            Name = "ClickA";
            ParseKeywords();
            return base.Initialise();
        }

        public override void OnUnload()
        {
            base.OnUnload();
        }

        private void OnTogglePauseHotkey()
        {
            Settings.IsPaused.Value = !Settings.IsPaused.Value;
        }

        public override void AreaChange(AreaInstance area)
        {
            if (area.IsHideout) // Check if the new area is a hideout
            {
                // Auto-enable the plugin when entering a hideout
                Settings.IsPaused.Value = false;
                _hasClickedInCurrentHideout = false; // Reset the one-time click flag
                DebugWindow.LogMsg("Entered Hideout. ClickA auto-enabled and hideout click flag reset.");
            }
            else
            {
                // This state isn't explicitly managed by the request, but we keep the click flag reset.
                // The plugin's general "Enable" setting still controls its overall activity outside hideout.
                _hasClickedInCurrentHideout = true; // Set to true so it doesn't click outside hideout
                DebugWindow.LogMsg("Left Hideout. Hideout click flag set to true.");
            }

            base.AreaChange(area);
        }

        public override Job Tick()
        {
            _isHotkeyCurrentlyPressed = Keyboard.IsKeyPressed(Settings.TogglePauseHotkey.Value);
            if (_isHotkeyCurrentlyPressed && !_wasHotkeyPreviouslyPressed)
            {
                OnTogglePauseHotkey();
            }
            _wasHotkeyPreviouslyPressed = _isHotkeyCurrentlyPressed;

            var leftPanel = IngameUi.OpenLeftPanel;
            bool isLeftPanelVisible = leftPanel?.IsVisible == true;

            // NEW LOGIC: If left panel is visible, pause the plugin
            if (isLeftPanelVisible)
            {
                if (!Settings.IsPaused.Value) // Only change if not already paused
                {
                    Settings.IsPaused.Value = true;
                    DebugWindow.LogMsg("Left Panel opened. ClickA auto-paused.");
                }
                _xButton2ActivationTime = DateTime.MinValue; // Reset the left panel key press timer
            }
            else // If left panel is NOT visible
            {
                // If it was visible last tick and now it's not, and we were paused by it, potentially unpause IF not in hideout.
                // However, the new requirement is "only disable when left panel will open", implying it stays enabled otherwise.
                // The `AreaChange` handles auto-enabling in hideout.
                _xButton2ActivationTime = DateTime.MinValue; // Clear the key press timer
            }

            // Original Left Panel Key Press logic (still active if enabled)
            if (Settings.EnableLeftPanelKeyPress.Value && isLeftPanelVisible && _xButton2ActivationTime == DateTime.MinValue && DateTime.Now >= _xButton2ActivationTime)
            {
                // Setting activation time if left panel just became visible
                if (!_wasLeftPanelVisibleLastTick)
                {
                    _xButton2ActivationTime = DateTime.Now.AddSeconds(1);
                }
                // If time passed, press the key
                if (_xButton2ActivationTime != DateTime.MinValue && DateTime.Now >= _xButton2ActivationTime)
                {
                    Keyboard.KeyPress(Settings.KeyOnLeftPanelVisible.Value);
                    _xButton2ActivationTime = DateTime.MinValue; // Reset to avoid repeated presses
                }
            }
            _wasLeftPanelVisibleLastTick = isLeftPanelVisible; // Update for next tick


            // One-time click on hideout entry logic (unchanged in its conditions, but now part of the auto-enabled flow)
            if (Settings.EnableOneTimeHideoutClick.Value &&
                GameController.Area.CurrentArea.IsHideout &&
                !Settings.IsPaused.Value && // Still respects manual pause or pause by left panel
                !_hasClickedInCurrentHideout &&
                DateTime.Now >= _nextAllowedActionTime)
            {
                DebugWindow.LogMsg("Performing one-time click in hideout.");
                Mouse.LeftClick(new System.Drawing.Point((int)GameController.Window.GetWindowRectangle().Center.X, (int)GameController.Window.GetWindowRectangle().Center.Y), 50);

                _hasClickedInCurrentHideout = true;
                _nextAllowedActionTime = DateTime.Now.AddMilliseconds(Settings.ActionCooldown.Value);
                return null;
            }

            // Master enable/pause checks
            // The logic here is critical: if we're paused by the left panel or manually, we stop.
            // If we are in a hideout, `AreaChange` sets IsPaused to false, enabling the script.
            if (!Settings.Enable.Value || Settings.IsPaused.Value || GameController.IsLoading)
            {
                _nextAllowedActionTime = DateTime.Now;
                return null;
            }

            var newFilterKeywords = Settings.Filter.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(x => x.Trim().ToLower())
                                                          .ToList();

            if (_parsedITEMMetadataKeywords == null || !_parsedITEMMetadataKeywords.SequenceEqual(newFilterKeywords))
            {
                _parsedITEMMetadataKeywords = newFilterKeywords;
            }

            if (DateTime.Now < _nextAllowedActionTime)
            {
                return null;
            }

            return new Job("ClickA_PickUpItem", () =>
            {
                PickUpItem();
            });
        }

        private void ParseKeywords()
        {
            _parsedITEMMetadataKeywords = Settings.Filter.Value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLower())
                .ToList();
        }

        private bool PickUpItem()
        {
            var playerCurrentPos = PlayerPos;

            try
            {
                var items = IngameUi.ItemsOnGroundLabelsVisible;
                if (items != null)
                {
                    var itemLabelToPick = items
                        .Where(x => Settings.Enable.Value &&
                                    x.ItemOnGround?.Metadata != null &&
                                    _parsedITEMMetadataKeywords.Any(keyword => x.ItemOnGround.Metadata.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(x => Vector3.Distance(playerCurrentPos, new Vector3(x.ItemOnGround.Pos.X, x.ItemOnGround.Pos.Y, playerCurrentPos.Z)))
                        .FirstOrDefault();

                    if (itemLabelToPick == null) return false;

                    var itemWorldPos = new Vector3(itemLabelToPick.ItemOnGround.Pos.X, itemLabelToPick.ItemOnGround.Pos.Y, playerCurrentPos.Z);
                    var distanceToItem = Vector3.Distance(playerCurrentPos, itemWorldPos);

                    if (distanceToItem <= Settings.Range.Value)
                    {
                        if (itemLabelToPick.Label.Children.Count >= 3)
                        {
                            var targetChildElement = itemLabelToPick.Label.Children[2];
                            var screenPoint = new System.Drawing.Point(
                                (int)targetChildElement.GetClientRectCache.Center.X,
                                (int)targetChildElement.GetClientRectCache.Center.Y
                            );
                            Mouse.LeftClick(screenPoint, 50);
                        }
                        else
                        {
                            var labelRect = itemLabelToPick.Label.GetClientRectCache;
                            var screenX = (int)labelRect.Center.X;
                            var adjustedY = (int)labelRect.Center.Y + Settings.ItemClickYOffset.Value;
                            var screenPoint = new System.Drawing.Point(screenX, adjustedY);
                            Mouse.LeftClick(screenPoint, 50);
                        }

                        _nextAllowedActionTime = DateTime.Now.AddMilliseconds(Settings.ActionCooldown.Value);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            return false;
        }
    }
}
