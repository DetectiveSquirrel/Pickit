using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using System.Windows.Forms;

namespace Pickit
{
    public class Settings : SettingsBase
    {
        public Settings()
        {
            PickUpKey = Keys.F1;
            PickupRange = new RangeNode<int>(600, 1, 1000);
            ExtraDelay = new RangeNode<int>(0, 0, 200);
            PickupTimerDelay = new RangeNode<int>(124, 1, 200);
            SixSocket = true;
            SixLink = true;
            RGB = true;
            AllDivs = true;
            AllCurrency = true;
            AllUniques = true;
            Maps = true;
            UniqueMap = true;
            MapTier = new RangeNode<int>(1, 1, 16);
            QuestItems = true;
            Gems = true;
            GemQuality = new RangeNode<int>(1, 0, 20);
        }

        [Menu("Pickup Key")]
        public HotkeyNode PickUpKey { get; set; }

        [Menu("Pickup Radius")]
        public RangeNode<int> PickupRange { get; set; }
        [Menu("Extra Click Delay")]
        public RangeNode<int> ExtraDelay { get; set; }
        [Menu("Pickup Delay")]
        public RangeNode<int> PickupTimerDelay { get; set; }

        [Menu("6 Sockets")]
        public ToggleNode SixSocket { get; set; }
        [Menu("6 Links")]
        public ToggleNode SixLink { get; set; }
        [Menu("RGB")]
        public ToggleNode RGB { get; set; }
        [Menu("All Divination Cards")]
        public ToggleNode AllDivs { get; set; }
        [Menu("All Currency")]
        public ToggleNode AllCurrency { get; set; }
        [Menu("All Uniques")]
        public ToggleNode AllUniques { get; set; }
        [Menu("Maps", 1)]
        public ToggleNode Maps { get; set; }
        [Menu("Lowest Tier", 11, 1)]
        public RangeNode<int> MapTier { get; set; }
        [Menu("All Unique Maps", 12, 1)]
        public ToggleNode UniqueMap { get; set; }
        [Menu("Gems", 2)]
        public ToggleNode Gems { get; set; }
        [Menu("Lowest Gem Quality", 22, 2)]
        public RangeNode<int> GemQuality { get; set; }
        [Menu("Quest Items")]
        public ToggleNode QuestItems { get; set; }

    }
}