using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Windows.Forms;

namespace ClickA
{
    public class ClickASettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        public RangeNode<float> ClickDistance { get; set; } = new RangeNode<float>(150, 10, 10000);
        public RangeNode<int> ClickCooldown { get; set; } = new RangeNode<int>(100, 0, 10000);
    }
}
