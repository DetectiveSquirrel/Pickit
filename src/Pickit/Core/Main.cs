#region Header

/*
 * Idea/Code from Qvin's auto pickup
 * Reworked into a more configurable version
*/

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Pickit.Filters;
using Pickit.Utilities;
using PoeHUD.Framework;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Models.Interfaces;
using PoeHUD.Plugins;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using PoeHUD.Poe.EntityComponents;
using SharpDX;
using Map = PoeHUD.Poe.Components.Map;
using Memory = Pickit.Utilities.Memory;

namespace Pickit.Core
{
    public class Main : BaseSettingsPlugin<Settings>
    {
        private const int PixelBorder = 3;

        private const string PickitRuleDirectory = "Pickit Rules";

        private const string FitersConfigFile = "FitersConfig.txt";
        private readonly List<EntityWrapper> _entities = new List<EntityWrapper>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();

        private Vector2 _clickWindowOffset;
        private List<CustomFilter> _customFilters;
        private HashSet<string> _magicRules;

        private HashSet<string> _normalRules;
        private HashSet<string> _rareRules;
        private HashSet<string> _uniqueRules;
        private bool _working;

        public Main()
        {
            PluginName = "Pickit";
        }

        private string PickitConfigFileDirectory => LocalPluginDirectory + @"\" + PickitRuleDirectory;

        internal Memory Process { get; private set; }

        public override void Initialise()
        {
            Settings.NormalRuleFile.OnValueSelected += ReloadRuleOnSelectNormal;
            Settings.MagicRuleFile.OnValueSelected += ReloadRuleOnSelectMagic;
            Settings.RareRuleFile.OnValueSelected += ReloadRuleOnSelectRare;
            Settings.UniqueRuleFile.OnValueSelected += ReloadRuleOnSelectUnique;
            Settings.ReloadRules.OnPressed += LoadRuleFiles;

            LoadRuleFiles();

            Process = new Memory(GameController.Window.Process.Id);
        }

        private void LoadRuleFiles()
        {
            var dirInfo = new DirectoryInfo(PickitConfigFileDirectory);
            var pickitFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();

            //LoadCustomFilters();

            Settings.NormalRuleFile.SetListValues(pickitFiles);
            Settings.MagicRuleFile.SetListValues(pickitFiles);
            Settings.RareRuleFile.SetListValues(pickitFiles);
            Settings.UniqueRuleFile.SetListValues(pickitFiles);

            _normalRules = LoadPickit(Settings.NormalRuleFile.Value);
            _magicRules = LoadPickit(Settings.MagicRuleFile.Value);
            _rareRules = LoadPickit(Settings.RareRuleFile.Value);
            _uniqueRules = LoadPickit(Settings.UniqueRuleFile.Value);
        }

        private void ReloadRuleOnSelectNormal(string fileName)
        {
            _normalRules = LoadPickit(fileName);
        }

        private void ReloadRuleOnSelectMagic(string fileName)
        {
            _magicRules = LoadPickit(fileName);
        }

        private void ReloadRuleOnSelectRare(string fileName)
        {
            _rareRules = LoadPickit(fileName);
        }

        private void ReloadRuleOnSelectUnique(string fileName)
        {
            _uniqueRules = LoadPickit(fileName);
        }

        public override void Render()
        {
            //base.Render();

            try
            {
                if (!Keyboard.IsKeyDown((int) Settings.PickUpKey.Value)) return;
                if (_working)
                    return;
                _working = true;
                PickUpItem();
                //PickUpItemTest();
            }
            catch
            {
                // ignored
            }
        }

        public HashSet<string> LoadPickit(string fileName)
        {
            var pickitFile = $@"{PluginDirectory}\{PickitRuleDirectory}\{fileName}.txt";

            if (!File.Exists(pickitFile))
                return null;

            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var lines = File.ReadAllLines(pickitFile);

            lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#"))
                .ForEach(x => hashSet.Add(x.Trim().ToLowerInvariant()));

            LogMessage($"PICKIT :: (Re)Loaded {fileName}", 5, Color.GreenYellow);

            return hashSet;
        }

        private int GetEntityDistance(EntityWrapper entity)
        {
            var playerPosition = GameController.Player.GetComponent<Positioned>();
            var monsterPosition = entity.GetComponent<Positioned>();
            var distanceToEntity = Math.Sqrt(Math.Pow(playerPosition.X - monsterPosition.X, 2) +
                                             Math.Pow(playerPosition.Y - monsterPosition.Y, 2));

            return (int) distanceToEntity;
        }

        private int GetEntityDistance(IEntity entity)
        {
            var playerPosition = GameController.Player.GetComponent<Positioned>();
            var monsterPosition = entity.GetComponent<Positioned>();
            var distanceToEntity = Math.Sqrt(Math.Pow(playerPosition.X - monsterPosition.X, 2) +
                                             Math.Pow(playerPosition.Y - monsterPosition.Y, 2));

            return (int) distanceToEntity;
        }

        public bool InListNormal(ItemsOnGroundLabelElement itemEntity)
        {
            try
            {
                var item = itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                var itemEntityName = GameController.Files.BaseItemTypes.Translate(item.Path).BaseName;
                var rarity = item.GetComponent<Mods>().ItemRarity;
                if (_normalRules.Contains(itemEntityName) &&
                    rarity != ItemRarity.Magic &&
                    rarity != ItemRarity.Rare &&
                    rarity != ItemRarity.Unique)
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        public bool InListMagic(ItemsOnGroundLabelElement itemEntity)
        {
            try
            {
                var item = itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                var itemEntityName = GameController.Files.BaseItemTypes.Translate(item.Path).BaseName;
                var rarity = item.GetComponent<Mods>().ItemRarity;
                if (_magicRules.Contains(itemEntityName) &&
                    rarity != ItemRarity.Normal &&
                    rarity != ItemRarity.Rare &&
                    rarity != ItemRarity.Unique)
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        public bool InListRare(ItemsOnGroundLabelElement itemEntity)
        {
            try
            {
                var item = itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                var itemEntityName = GameController.Files.BaseItemTypes.Translate(item.Path).BaseName;
                var rarity = item.GetComponent<Mods>().ItemRarity;
                if (_rareRules.Contains(itemEntityName) &&
                    rarity != ItemRarity.Normal &&
                    rarity != ItemRarity.Magic &&
                    rarity != ItemRarity.Unique)
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        public bool InListUnique(ItemsOnGroundLabelElement itemEntity)
        {
            try
            {
                var item = itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                var itemEntityName = GameController.Files.BaseItemTypes.Translate(item.Path).BaseName;
                var rarity = item.GetComponent<Mods>().ItemRarity;
                if (_uniqueRules.Contains(itemEntityName) &&
                    rarity != ItemRarity.Normal &&
                    rarity != ItemRarity.Magic &&
                    rarity != ItemRarity.Rare)
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }


        public bool OverrideChecks(ItemsOnGroundLabelElement itemEntity)
        {
            try
            {
                var item = itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;
                var className = GameController.Files.BaseItemTypes.Translate(item.Path).ClassName;

                if (Settings.ElderItems)
                    if (item.GetComponent<Base>().isElder)
                        return true;

                if (Settings.ShaperItems)
                    if (item.GetComponent<Base>().isShaper)
                        return true;

                if (Settings.Rares && item.GetComponent<Mods>().ItemRarity == ItemRarity.Rare)
                {
                    if (Settings.RareJewels && (className == "Jewel" || className == "AbyssJewel"))
                        return true;
                    if (Settings.RareRings && className == "Ring" &&
                        item.GetComponent<Mods>().ItemLevel >= Settings.RareRingsilvl)
                        return true;
                    if (Settings.RareAmulets && className == "Amulet" &&
                        item.GetComponent<Mods>().ItemLevel >= Settings.RareAmuletsilvl)
                        return true;
                    if (Settings.RareBelts && className == "Belt" &&
                        item.GetComponent<Mods>().ItemLevel >= Settings.RareBeltsilvl)
                        return true;
                    if (Settings.RareGloves && className == "Gloves" &&
                        item.GetComponent<Mods>().ItemLevel >= Settings.RareGlovesilvl)
                        return true;
                    if (Settings.RareBoots && className == "Boots" &&
                        item.GetComponent<Mods>().ItemLevel >= Settings.RareBootsilvl)
                        return true;
                    if (Settings.RareHelmets && className == "Helmet" &&
                        item.GetComponent<Mods>().ItemLevel >= Settings.RareHelmetsilvl)
                        return true;
                    if (Settings.RareArmour && className == "Body Armour" &&
                        item.GetComponent<Mods>().ItemLevel >= Settings.RareArmourilvl)
                        return true;
                }

                if (Settings.SixSocket && item.GetComponent<Sockets>().NumberOfSockets == 6)
                    return true;
                if (Settings.SixLink && item.GetComponent<Sockets>().LargestLinkSize == 6)
                    return true;
                if (Settings.RGB && item.GetComponent<Sockets>().IsRGB)
                    return true;
                if (Settings.AllDivs && className == "DivinationCard")
                    return true;
                if (Settings.AllCurrency && className == "StackableCurrency")
                    return true;
                if (Settings.AllUniques && item.GetComponent<Mods>().ItemRarity == ItemRarity.Unique)
                    return true;
                if (Settings.Maps && item.GetComponent<Map>().Tier >= Settings.MapTier.Value)
                    return true;
                if (Settings.Maps && item.GetComponent<Map>().Tier >= Settings.MapTier.Value)
                    return true;
                if (Settings.Maps && Settings.MapFragments && className == "MapFragment")
                    return true;
                if (Settings.Maps && Settings.UniqueMap && item.GetComponent<Map>().Tier >= 1 &&
                    item.GetComponent<Mods>().ItemRarity == ItemRarity.Unique)
                    return true;
                if (Settings.QuestItems && className == "QuestItem")
                    return true;
                if (Settings.Gems && item.GetComponent<Quality>().ItemQuality >= Settings.GemQuality.Value &&
                    className.Contains("Skill Gem"))
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }

        private void LoadCustomFilters()
        {
            var filterPath = Path.Combine(PluginDirectory, FitersConfigFile);
            var filtersLines = File.ReadAllLines(filterPath);
            var unused = new FilterParser();
            _customFilters = FilterParser.Parse(filtersLines);
        }

        public override void EntityAdded(EntityWrapper entityWrapper)
        {
            _entities.Add(entityWrapper);
        }

        public override void EntityRemoved(EntityWrapper entityWrapper)
        {
            _entities.Remove(entityWrapper);
        }

        public bool DoWePickThis(ItemsOnGroundLabelElement itemEntity)
        {
            var pickItemUp = false;

            switch (itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity.GetComponent<Mods>().ItemRarity)
            {
                case ItemRarity.Normal:
                    if (InListNormal(itemEntity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Magic:
                    if (InListMagic(itemEntity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Rare:
                    if (InListRare(itemEntity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Unique:
                    if (InListUnique(itemEntity))
                        pickItemUp = true;
                    break;
            }

            if (OverrideChecks(itemEntity))
                pickItemUp = true;

            return pickItemUp;
        }

        public bool DoWePickThisTest(ItemsOnGroundLabelElement itemEntity)
        {
            var pickItemUp = false;

            switch (itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity.GetComponent<Mods>().ItemRarity)
            {
                case ItemRarity.Normal:
                    if (InListNormal(itemEntity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Magic:
                    if (InListMagic(itemEntity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Rare:
                    if (InListRare(itemEntity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Unique:
                    if (InListUnique(itemEntity))
                        pickItemUp = true;
                    break;
            }

            if (OverrideChecks(itemEntity))
                pickItemUp = true;

            return pickItemUp;
        }

        private void PickUpItemTest()
        {
            if (_pickUpTimer.ElapsedMilliseconds < Settings.PickupTimerDelay)
            {
                _working = false;
                return;
            }
            _pickUpTimer.Restart();

            var currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                .Where(x => x.ItemOnGround.Path.ToLower().Contains("worlditem") && x.IsVisible &&
                            (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds == 0))
                .Select(x => new Tuple<int, ItemsOnGroundLabelElement>(GetEntityDistance(x.ItemOnGround), x))
                .OrderBy(x => x.Item1)
                .ToList();

            LogMessage(currentLabels.Count, 10);


            Tuple<int, ItemsOnGroundLabelElement> pickUpThisItem = null;
            foreach (var x in currentLabels)
                if (CheckFilters(new ItemData(x.Item2,
                        GameController.Files.BaseItemTypes.Translate(x.Item2.ItemOnGround.Path))) != null &&
                    x.Item1 < Settings.PickupRange)
                {
                    pickUpThisItem = x;
                    break;
                }

            if (pickUpThisItem != null)
            {
                if (PickItem(pickUpThisItem)) return;
            }
            else if (Settings.GroundChests)
            {
                ClickOnChests();
            }
            _working = false;
        }

        private bool CheckFilters(ItemData itemData)
        {
            var result = _customFilters.Any(filter => filter.CompareItem(itemData));
            LogMessage(result.ToString(), 10);
            return result;
        }

        private void PickUpItem()
        {
            if (_pickUpTimer.ElapsedMilliseconds < Settings.PickupTimerDelay)
            {
                _working = false;
                return;
            }
            _pickUpTimer.Restart();

            var currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                .Where(x => x.ItemOnGround.Path.ToLower().Contains("worlditem") && x.IsVisible &&
                            (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds == 0))
                .Select(x => new Tuple<int, ItemsOnGroundLabelElement>(GetEntityDistance(x.ItemOnGround), x))
                .OrderBy(x => x.Item1)
                .ToList();


            var pickUpThisItem = (from x in currentLabels
                where DoWePickThis(x.Item2)
                      && x.Item1 < Settings.PickupRange
                select x).FirstOrDefault();

            if (pickUpThisItem != null)
            {
                if (PickItem(pickUpThisItem)) return;
            }
            else if (Settings.GroundChests)
            {
                ClickOnChests();
            }
            _working = false;
        }

        private bool PickItem(Tuple<int, ItemsOnGroundLabelElement> pickUpThisItem)
        {
            if (pickUpThisItem.Item1 >= Settings.PickupRange)
            {
                _working = false;
                return true;
            }
            var vect = pickUpThisItem.Item2.Label.GetClientRect().Center;
            var vectWindow = GameController.Window.GetWindowRectangle();
            if (vect.Y + PixelBorder > vectWindow.Bottom || vect.Y - PixelBorder < vectWindow.Top)
            {
                _working = false;
                return true;
            }
            if (vect.X + PixelBorder > vectWindow.Right || vect.X - PixelBorder < vectWindow.Left)
            {
                _working = false;
                return true;
            }
            _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;

            var address = pickUpThisItem.Item2.ItemOnGround.GetComponent<Targetable>().Address;
            var isTargeted = address != 0 && Memory.ReadByte(address + 0x2A) == 1;

            Mouse.SetCursorPos(vect + _clickWindowOffset);
            if (!isTargeted) return false;

            if (Settings.LeftClickToggleNode)
                Mouse.LeftClick(Settings.ExtraDelay);
            else
                Mouse.RightClick(Settings.ExtraDelay);


            return false;
        }

        // Copy-Paste - Qvin0000's version
        private void ClickOnChests()
        {
            var sortedByDistChest = new List<Tuple<int, long, EntityWrapper>>();

            foreach (var entity in _entities)
                if (entity.Path.ToLower().Contains("chests") && entity.IsAlive && entity.IsHostile)
                {
                    if (!entity.HasComponent<Chest>()) continue;
                    var chest = entity.GetComponent<Chest>();
                    if (chest.IsStrongbox) continue;
                    if (chest.IsOpened) continue;
                    var d = GetEntityDistance(entity);

                    var tuple = new Tuple<int, long, EntityWrapper>(d, entity.Address, entity);
                    if (sortedByDistChest.Any(x => x.Item2 == entity.Address)) continue;

                    sortedByDistChest.Add(tuple);
                }

            var tempList = sortedByDistChest.OrderBy(x => x.Item1).ToList();
            if (tempList.Count <= 0) return;
            if (tempList[0].Item1 >= Settings.ChestRange) return;
            SetCursorToEntityAndClick(tempList[0].Item3);
            var centerScreen = GameController.Window.GetWindowRectangle().Center;
            Mouse.SetCursorPos(centerScreen);

            _working = false;
        }

        //Copy-Paste - Sithylis_QoL
        private void SetCursorToEntityAndClick(EntityWrapper entity)
        {
            var camera = GameController.Game.IngameState.Camera;
            var chestScreenCoords =
                camera.WorldToScreen(entity.Pos.Translate(0, 0, 0), entity);
            if (chestScreenCoords == new Vector2()) return;
            var pos = Mouse.GetCursorPosition();
            var coords = new Vector2(chestScreenCoords.X, chestScreenCoords.Y);
            Mouse.SetCursorPosAndLeftOrRightClick(coords, 100, Settings.LeftClickToggleNode.Value);
            Mouse.SetCursorPos(pos.X, pos.Y);
        }
    }
}