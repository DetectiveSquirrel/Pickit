using System.Windows.Forms;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;
using ImGuiVector2 = System.Numerics.Vector2;

namespace Pickit.Core
{
    public class Settings : SettingsBase
    {
        public Settings()
        {
            var centerPos = BasePlugin.API.GameController.Window.GetWindowRectangle().Center;
            PickUpKey = Keys.F1;
            PickupRange = new RangeNode<int>(600, 1, 1000);
            ChestRange = new RangeNode<int>(500, 1, 1000);
            ExtraDelay = new RangeNode<int>(0, 0, 200);
            ClickItemTimerDelay = new RangeNode<int>(124, 1, 200);
            Sockets = true;
            TotalSockets = new RangeNode<int>(6, 1, 6);
            Links = true;
            LargestLink = new RangeNode<int>(6, 1, 6);
            RGB = true;
            AllDivs = true;
            AllCurrency = true;
            AllUniques = true;
            Maps = true;
            UniqueMap = true;
            MapFragments = true;
            MapTier = new RangeNode<int>(1, 1, 16);
            QuestItems = true;
            Gems = true;
            GemQuality = new RangeNode<int>(1, 0, 20);
            GroundChests = false;
            ShaperItems = true;
            ElderItems = true;
            Rares = true;
            RareJewels = true;
            RareRings = true;
            RareRingsilvl = new RangeNode<int>(1, 0, 100);
            RareAmulets = true;
            RareAmuletsilvl = new RangeNode<int>(1, 0, 100);
            RareBelts = true;
            RareBeltsilvl = new RangeNode<int>(1, 0, 100);
            RareGloves = false;
            RareGlovesilvl = new RangeNode<int>(1, 0, 100);
            RareBoots = false;
            RareBootsilvl = new RangeNode<int>(1, 0, 100);
            RareHelmets = false;
            RareHelmetsilvl = new RangeNode<int>(1, 0, 100);
            RareWeapon = false;
            RareWeaponWidth = new RangeNode<int>(2, 1, 2);
            RareWeaponHeight = new RangeNode<int>(3, 1, 4);
            RareWeaponilvl = new RangeNode<int>(1, 0, 100);
            RareArmour = false;
            RareArmourilvl = new RangeNode<int>(1, 0, 100);
            PickUpEverything = false;
            NormalRuleFile = string.Empty;
            MagicRuleFile = string.Empty;
            RareRuleFile = string.Empty;
            UniqueRuleFile = string.Empty;
            LeftClickToggleNode = true;
            OverrideItemPickup = false;

            LastSettingSize = new ImGuiVector2(620, 376);
            LastSettingPos = new ImGuiVector2(centerPos.X - LastSettingSize.X / 2,centerPos.Y - LastSettingSize.Y / 2);
        }

        public HotkeyNode PickUpKey { get; set; }
        public RangeNode<int> PickupRange { get; set; }
        public RangeNode<int> ChestRange { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }
        public RangeNode<int> ClickItemTimerDelay { get; set; }
        public ToggleNode ShaperItems { get; set; }
        public ToggleNode ElderItems { get; set; }
        public ToggleNode Rares { get; set; }
        public ToggleNode RareJewels { get; set; }
        public ToggleNode RareRings { get; set; }
        public RangeNode<int> RareRingsilvl { get; set; }
        public ToggleNode RareAmulets { get; set; }
        public RangeNode<int> RareAmuletsilvl { get; set; }
        public ToggleNode RareBelts { get; set; }
        public RangeNode<int> RareBeltsilvl { get; set; }
        public ToggleNode RareGloves { get; set; }
        public RangeNode<int> RareGlovesilvl { get; set; }
        public ToggleNode RareBoots { get; set; }
        public RangeNode<int> RareBootsilvl { get; set; }
        public ToggleNode RareHelmets { get; set; }
        public RangeNode<int> RareHelmetsilvl { get; set; }
        public ToggleNode RareArmour { get; set; }
        public RangeNode<int> RareArmourilvl { get; set; }
        public ToggleNode RareWeapon { get; set; }
        public RangeNode<int> RareWeaponWidth{ get; set; }
        public RangeNode<int> RareWeaponHeight { get; set; }
        public RangeNode<int> RareWeaponilvl { get; set; }
        public EmptyNode LinkSocketRgbEmptyNode { get; set; }
        public ToggleNode Sockets { get; set; }
        public RangeNode<int> TotalSockets { get; set; }
        public ToggleNode Links { get; set; }
        public RangeNode<int> LargestLink { get; set; }
        public ToggleNode RGB { get; set; }
        public EmptyNode AllOverridEmptyNode { get; set; }
        public ToggleNode PickUpEverything { get; set; }
        public ToggleNode AllDivs { get; set; }
        public ToggleNode AllCurrency { get; set; }
        public ToggleNode AllUniques { get; set; }
        public ToggleNode Maps { get; set; }
        public RangeNode<int> MapTier { get; set; }
        public ToggleNode UniqueMap { get; set; }
        public ToggleNode MapFragments { get; set; }
        public ToggleNode Gems { get; set; }
        public RangeNode<int> GemQuality { get; set; }
        public ToggleNode QuestItems { get; set; }
        public ToggleNode GroundChests { get; set; }
        public ToggleNode LeftClickToggleNode { get; set; }
        public ToggleNode OverrideItemPickup { get; set; }

        public string NormalRuleFile { get; set; }
        public string MagicRuleFile { get; set; }
        public string RareRuleFile { get; set; }
        public string UniqueRuleFile { get; set; }

        public ImGuiVector2 LastSettingPos { get; set; }
        public ImGuiVector2 LastSettingSize { get; set; }
    }
}