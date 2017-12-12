using System.Windows.Forms;
using PoeHUD.Hud.Settings;
using PoeHUD.Plugins;

namespace Pickit.Core
{
    public class Settings : SettingsBase
    {
        public Settings()
        {
            PickUpKey = Keys.F1;
            ShowHideKey = Keys.Z;
            PickupRange = new RangeNode<int>(600, 1, 1000);
            ChestRange = new RangeNode<int>(500, 1, 1000);
            ExtraDelay = new RangeNode<int>(0, 0, 200);
            PickupTimerDelay = new RangeNode<int>(124, 1, 200);
            SixSocket = true;
            SixLink = true;
            RGB = true;
            AllDivs = true;
            AllCurrency = true;
            //ScrollLimiToggleNode = false;
            //ScrollLimit = new RangeNode<int>(40, 1, 40);
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
            RareArmour = false;
            RareArmourilvl = new RangeNode<int>(1, 0, 100);


            NonUniqueAndRareRuleFile = new ListNode();
            UniqueRuleFile = new ListNode();
            RareRuleFile = new ListNode();
            ReloadRules = new ButtonNode();

            LeftClickToggleNode = true;
        }

        [Menu("Pickit Rules", 23443)]
        public EmptyNode PickitRulesEmptyNode { get; set; }

        [Menu("Non Unique/Rare File", 12314, 23443)]
        public ListNode NonUniqueAndRareRuleFile { get; set; }

        [Menu("Unique File", 12315, 23443)]
        public ListNode UniqueRuleFile { get; set; }

        [Menu("Rare File", 12313, 23443)]
        public ListNode RareRuleFile { get; set; }

        [Menu("Reload All Rules", 3264, 23443)]
        public ButtonNode ReloadRules { get; set; }

        [Menu("Pickup Key")]
        public HotkeyNode PickUpKey { get; set; }

        [Menu("Show/Hide Items Key")]
        public HotkeyNode ShowHideKey { get; set; }

        [Menu("Pickup Radius")]
        public RangeNode<int> PickupRange { get; set; }

        [Menu("Chest Radius")]
        public RangeNode<int> ChestRange { get; set; }

        [Menu("Extra Click Delay")]
        public RangeNode<int> ExtraDelay { get; set; }

        [Menu("Pickup Delay")]
        public RangeNode<int> PickupTimerDelay { get; set; }

        [Menu("Shaper Items")]
        public ToggleNode ShaperItems { get; set; }

        [Menu("Elder Items")]
        public ToggleNode ElderItems { get; set; }

        [Menu("Rares", 3)]
        public ToggleNode Rares { get; set; }

        [Menu("Jewels", 33, 3)]
        public ToggleNode RareJewels { get; set; }

        [Menu("Rings", 31, 3)]
        public ToggleNode RareRings { get; set; }

        [Menu("ilvl", 311, 31)]
        public RangeNode<int> RareRingsilvl { get; set; }

        [Menu("Amulets", 32, 3)]
        public ToggleNode RareAmulets { get; set; }

        [Menu("ilvl", 321, 32)]
        public RangeNode<int> RareAmuletsilvl { get; set; }

        [Menu("Belts", 34, 3)]
        public ToggleNode RareBelts { get; set; }

        [Menu("ilvl", 341, 34)]
        public RangeNode<int> RareBeltsilvl { get; set; }

        [Menu("Gloves", 35, 3)]
        public ToggleNode RareGloves { get; set; }

        [Menu("ilvl", 351, 35)]
        public RangeNode<int> RareGlovesilvl { get; set; }

        [Menu("Boots", 36, 3)]
        public ToggleNode RareBoots { get; set; }

        [Menu("ilvl", 361, 36)]
        public RangeNode<int> RareBootsilvl { get; set; }

        [Menu("Helmets", 37, 3)]
        public ToggleNode RareHelmets { get; set; }

        [Menu("ilvl", 371, 37)]
        public RangeNode<int> RareHelmetsilvl { get; set; }

        [Menu("Armour", 38, 3)]
        public ToggleNode RareArmour { get; set; }

        [Menu("ilvl", 381, 38)]
        public RangeNode<int> RareArmourilvl { get; set; }

        [Menu("Links/Socket/RGB", 5435)]
        public EmptyNode LinkSocketRgbEmptyNode { get; set; }

        [Menu("6 Sockets", 23432, 5435)]
        public ToggleNode SixSocket { get; set; }

        [Menu("6 Links", 23433, 5435)]
        public ToggleNode SixLink { get; set; }

        [Menu("RGB", 23431, 5435)]
        public ToggleNode RGB { get; set; }

        [Menu("Overrides", 24251)]
        public EmptyNode AllOverridEmptyNode { get; set; }

        [Menu("All Divination Cards", 3451, 24251)]
        public ToggleNode AllDivs { get; set; }

        [Menu("All Currency", 3452, 24251)]
        public ToggleNode AllCurrency { get; set; }

        //[Menu("Limit Scroll Pickup", 34521, 3452)]
        //public ToggleNode ScrollLimiToggleNode { get; set; }

        //[Menu("Amount", 345211, 34521)]
        //public RangeNode<int> ScrollLimit { get; set; }

        [Menu("All Uniques", 3453, 24251)]
        public ToggleNode AllUniques { get; set; }

        [Menu("Maps", 1, 24251)]
        public ToggleNode Maps { get; set; }

        [Menu("Lowest Tier", 11, 1)]
        public RangeNode<int> MapTier { get; set; }

        [Menu("All Unique Maps", 12, 1)]
        public ToggleNode UniqueMap { get; set; }

        [Menu("Fragments", 13, 1)]
        public ToggleNode MapFragments { get; set; }

        [Menu("Gems", 2, 24251)]
        public ToggleNode Gems { get; set; }

        [Menu("Lowest Gem Quality", 22, 2)]
        public RangeNode<int> GemQuality { get; set; }

        [Menu("Quest Items", 645, 24251)]
        public ToggleNode QuestItems { get; set; }

        [Menu("Chests")]
        public ToggleNode GroundChests { get; set; }

        [Menu("Left Click = On / Right Click = Off")]
        public ToggleNode LeftClickToggleNode { get; set; }

    }
}