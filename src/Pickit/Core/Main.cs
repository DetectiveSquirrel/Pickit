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
using Pickit.Utilities;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe.Components;
using SharpDX;

namespace Pickit.Core
{
    public class Main : BaseSettingsPlugin<Settings>
    {
        private const int PixelBorder = 3;

        private const string PickitRuleDirectory = "Pickit Rules";

        private readonly List<EntityWrapper> _entities = new List<EntityWrapper>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();

        private Vector2 _clickWindowOffset;
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
                FindItemToPick();
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

        public bool InCustomList(HashSet<string> checkList , CustomItem itemEntity, ItemRarity rarity)
        {
            try
            {
                if (checkList.Contains(itemEntity.BaseName) && itemEntity.Rarity == rarity)
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }


        public bool OverrideChecks(CustomItem item)
        {
            try
            {
                if (Settings.ElderItems)
                    if (item.IsElder)
                        return true;

                if (Settings.ShaperItems)
                    if (item.IsShaper)
                        return true;

                if (Settings.Rares && item.Rarity == ItemRarity.Rare)
                {
                    if (Settings.RareJewels && (item.ClassName == "Jewel" || item.ClassName == "AbyssJewel"))
                        return true;
                    if (Settings.RareRings && item.ClassName == "Ring" &&
                        item.ItemLevel >= Settings.RareRingsilvl)
                        return true;
                    if (Settings.RareAmulets && item.ClassName == "Amulet" &&
                        item.ItemLevel >= Settings.RareAmuletsilvl)
                        return true;
                    if (Settings.RareBelts && item.ClassName == "Belt" &&
                        item.ItemLevel >= Settings.RareBeltsilvl)
                        return true;
                    if (Settings.RareGloves && item.ClassName == "Gloves" &&
                        item.ItemLevel >= Settings.RareGlovesilvl)
                        return true;
                    if (Settings.RareBoots && item.ClassName == "Boots" &&
                        item.ItemLevel >= Settings.RareBootsilvl)
                        return true;
                    if (Settings.RareHelmets && item.ClassName == "Helmet" &&
                        item.ItemLevel >= Settings.RareHelmetsilvl)
                        return true;
                    if (Settings.RareArmour && item.ClassName == "Body Armour" &&
                        item.ItemLevel >= Settings.RareArmourilvl)
                        return true;
                }

                if (Settings.Sockets && item.Sockets >= Settings.TotalSockets.Value)
                    return true;
                if (Settings.Links && item.LargestLink >= Settings.LargestLink)
                    return true;
                if (Settings.RGB && item.IsRGB)
                    return true;
                if (Settings.AllDivs && item.ClassName == "DivinationCard")
                    return true;
                if (Settings.AllCurrency && item.ClassName == "StackableCurrency")
                    return true;
                if (Settings.AllUniques && item.Rarity == ItemRarity.Unique)
                    return true;
                if (Settings.Maps && item.MapTier >= Settings.MapTier.Value)
                    return true;
                if (Settings.Maps && item.MapTier >= Settings.MapTier.Value)
                    return true;
                if (Settings.Maps && Settings.MapFragments && item.ClassName == "MapFragment")
                    return true;
                if (Settings.Maps && Settings.UniqueMap && item.MapTier >= 1 &&
                    item.Rarity == ItemRarity.Unique)
                    return true;
                if (Settings.QuestItems && item.ClassName == "QuestItem")
                    return true;
                if (Settings.Gems && item.Quality >= Settings.GemQuality.Value &&
                    item.ClassName.Contains("Skill Gem"))
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }


        public override void EntityAdded(EntityWrapper entityWrapper)
        {
            _entities.Add(entityWrapper);
        }

        public override void EntityRemoved(EntityWrapper entityWrapper)
        {
            _entities.Remove(entityWrapper);
        }

        public bool DoWePickThis(CustomItem itemEntity)
        {
            var pickItemUp = false;

            if (Settings.PickUpEverything)
                return true;

            switch (itemEntity.Rarity)
            {
                case ItemRarity.Normal:
                    if (InCustomList(_normalRules,itemEntity, itemEntity.Rarity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Magic:
                    if (InCustomList(_magicRules, itemEntity, itemEntity.Rarity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Rare:
                    if (InCustomList(_rareRules, itemEntity, itemEntity.Rarity))
                        pickItemUp = true;
                    break;
                case ItemRarity.Unique:
                    if (InCustomList(_uniqueRules, itemEntity, itemEntity.Rarity))
                        pickItemUp = true;
                    break;
            }

            if (OverrideChecks(itemEntity))
                pickItemUp = true;

            return pickItemUp;
        }

        private void FindItemToPick()
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
                .Select(x => new Tuple<int, CustomItem>(x.ItemOnGround.Distance, new CustomItem(x)))
                .OrderBy(x => x.Item1)
                .ToList();


            var pickUpThisItem = (from x in currentLabels
                where DoWePickThis(x.Item2)
                      && x.Item1 < Settings.PickupRange
                select x).FirstOrDefault();

            if (pickUpThisItem != null)
            {
                if (TryToPick(pickUpThisItem.Item2)) return;
            }

            else if (Settings.GroundChests)
            {
                ClickOnChests();
            }

            _working = false;
        }

        private bool TryToPick(CustomItem pickUpThisItem)
        {
            if (pickUpThisItem.CompleteItem.ItemOnGround.Distance >= Settings.PickupRange)
            {
                _working = false;
                return true;
            }

            var vect = pickUpThisItem.CompleteItem.Label.GetClientRect().Center;
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

            var address = pickUpThisItem.CompleteItem.ItemOnGround.GetComponent<Targetable>().Address;
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

                    var tuple = new Tuple<int, long, EntityWrapper>(entity.Distance, entity.Address, entity);
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