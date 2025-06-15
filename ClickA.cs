using System;
using System.Linq;
using System.Drawing;
using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using SharpDX;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using ExileCore.Shared.Attributes;

namespace ClickA
{
    public class ClickA : BaseSettingsPlugin<ClickASettings>
    {
        private DateTime _nextAllowedPortalClickTime = DateTime.Now;

        private IngameUIElements IngameUi => GameController.IngameState.IngameUi;
        private Camera Camera => GameController.Game.IngameState.Camera;
        private Vector3 PlayerPos => GameController.Player.Pos;

        public override bool Initialise()
        {
            Name = "ClickA";
            return base.Initialise();
        }

        public override void Render()
        {
            if (!Settings.Enable.Value || !GameController.Window.IsForeground() || GameController.Player == null || GameController.IsLoading)
            {
                return;
            }
            
            TryClickTransition();
        }

        private LabelOnGround GetClosestTransitionLabel()
        {
            try
            {
                if (IngameUi?.ItemsOnGroundLabelsVisible == null)
                {
                    return null;
                }

                var transitionLabels = IngameUi.ItemsOnGroundLabelsVisible
                    .Where(x => 
                        x?.ItemOnGround != null &&
                        (x.ItemOnGround.Metadata.ToLower().Contains("areatransition") ||
                         x.ItemOnGround.Metadata.ToLower().Contains("arena") ||
                         x.ItemOnGround.Metadata.ToLower().EndsWith("ultimatumentrance"))
                    )
                    .OrderBy(x => Vector3.Distance(PlayerPos, x.ItemOnGround.Pos))
                    .ToList();

                if (!transitionLabels.Any())
                {
                    return null;
                }

                return transitionLabels.FirstOrDefault();
            }
            catch (Exception ex)
            {
                LogError($"Error in GetClosestTransitionLabel: {ex.Message}");
                return null;
            }
        }

        private void TryClickTransition()
        {
            if (DateTime.Now < _nextAllowedPortalClickTime)
            {
                return;
            }

            var portal = GetClosestTransitionLabel();
            if (portal != null)
            {
                var distanceToPortal = Vector3.Distance(PlayerPos, portal.ItemOnGround.Pos);

                if (distanceToPortal <= Settings.ClickDistance.Value)
                {
                    try
                    {
                        var screenPos = Camera.WorldToScreen(portal.ItemOnGround.Pos);
                        var screenPoint = new System.Drawing.Point((int)screenPos.X, (int)screenPos.Y);
                        Mouse.LeftClick(screenPoint, 50);
                        _nextAllowedPortalClickTime = DateTime.Now.AddMilliseconds(Settings.ClickCooldown.Value);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error during transition click: {ex.Message}");
                    }
                }
            }
        }
    }
}
