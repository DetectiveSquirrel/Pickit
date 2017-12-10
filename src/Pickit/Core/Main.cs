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
using PoeHUD.Models.Interfaces;
using PoeHUD.Plugins;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using SharpDX;
using Map = PoeHUD.Poe.Components.Map;

namespace Pickit.Core
{
    public class Main : BaseSettingsPlugin<Settings>
    {
        private const int PixelBorder = 3;

        private const string PickitRuleDirectory = "Pickit Rules";
        private readonly List<EntityWrapper> _entities = new List<EntityWrapper>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();
        private bool _amIWorking;

        private Vector2 _clickWindowOffset;

        private HashSet<string> _nonUniqueAndRare;

        private HashSet<string> _rares
            ;

        private HashSet<string> _uniques;

        public Main()
        {
            PluginName = "Pickit";
        }

        private string PickitConfigFileDirectory => LocalPluginDirectory + @"\" + PickitRuleDirectory;

        public override void Initialise()
        {
            Settings.NonUniqueAndRareRuleFile.OnValueSelected += ReloadRuleOnSelectNonUnique;
            Settings.UniqueRuleFile.OnValueSelected += ReloadRuleOnSelectUnique;
            Settings.RareRuleFile.OnValueSelected += ReloadRuleOnSelectRare;
            Settings.ReloadRules.OnPressed += LoadRuleFiles;

            LoadRuleFiles();
        }

        private void LoadRuleFiles()
        {
            var dirInfo = new DirectoryInfo(PickitConfigFileDirectory);
            var pickitFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();

            Settings.UniqueRuleFile.SetListValues(pickitFiles);
            Settings.NonUniqueAndRareRuleFile.SetListValues(pickitFiles);
            Settings.RareRuleFile.SetListValues(pickitFiles);

            _uniques = LoadPickit(Settings.UniqueRuleFile.Value);
            _nonUniqueAndRare = LoadPickit(Settings.NonUniqueAndRareRuleFile.Value);
            _rares = LoadPickit(Settings.RareRuleFile.Value);
        }

        private void ReloadRuleFiles()
        {
            _uniques = LoadPickit(Settings.UniqueRuleFile.Value);
            _nonUniqueAndRare = LoadPickit(Settings.NonUniqueAndRareRuleFile.Value);
            _rares = LoadPickit(Settings.RareRuleFile.Value);
        }

        private void ReloadRuleOnSelectNonUnique(string fileName)
        {
            _nonUniqueAndRare = LoadPickit(fileName);
        }

        private void ReloadRuleOnSelectUnique(string fileName)
        {
            _uniques = LoadPickit(fileName);
        }

        private void ReloadRuleOnSelectRare(string fileName)
        {
            _rares = LoadPickit(fileName);
        }

        public override void Render()
        {
            //base.Render();

            try
            {
                if (!Keyboard.IsKeyDown((int) Settings.PickUpKey.Value)) return;
                if (_amIWorking)
                    return;
                _amIWorking = true;
                NewPickUp();
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

            LogMessage($"PICKIT :: completed load for {fileName}", 5, Color.GreenYellow);

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

        public bool InListNonUniqueAndRare(ItemsOnGroundLabelElement itemEntity)
        {
            try
            {
                var item = itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                var itemEntityName = GameController.Files.BaseItemTypes.Translate(item.Path).BaseName;
                var rarity = item.GetComponent<Mods>().ItemRarity;
                if (_nonUniqueAndRare.Contains(itemEntityName) && rarity != ItemRarity.Unique &&
                    rarity != ItemRarity.Rare)
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
                if (_uniques.Contains(itemEntityName) && rarity == ItemRarity.Unique)
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
                if (_rares.Contains(itemEntityName) && rarity == ItemRarity.Rare)
                    return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }



        public bool MiscChecks(ItemsOnGroundLabelElement itemEntity)
        {
            try
            {
                var item = itemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;
                var className = GameController.Files.BaseItemTypes.Translate(item.Path).ClassName;

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

        public override void EntityAdded(EntityWrapper entityWrapper)
        {
            _entities.Add(entityWrapper);
        }

        public override void EntityRemoved(EntityWrapper entityWrapper)
        {
            _entities.Remove(entityWrapper);
        }

        private void NewPickUp()
        {
            if (_pickUpTimer.ElapsedMilliseconds < Settings.PickupTimerDelay)
            {
                _amIWorking = false;
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
                where (InListNonUniqueAndRare(x.Item2) || InListUnique(x.Item2) || InListRare(x.Item2) ||
                       MiscChecks(x.Item2))
                      && x.Item1 < Settings.PickupRange
                select x).FirstOrDefault();

            if (pickUpThisItem != null)
            {
                if (pickUpThisItem.Item1 >= Settings.PickupRange)
                {
                    _amIWorking = false;
                    return;
                }
                var vect = pickUpThisItem.Item2.Label.GetClientRect().Center;
                var vectWindow = GameController.Window.GetWindowRectangle();
                if (vect.Y + PixelBorder > vectWindow.Bottom || vect.Y - PixelBorder < vectWindow.Top)
                {
                    _amIWorking = false;
                    return;
                }
                if (vect.X + PixelBorder > vectWindow.Right || vect.X - PixelBorder < vectWindow.Left)
                {
                    _amIWorking = false;
                    return;
                }
                _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
                Mouse.SetCursorPosAndLeftClick(vect + _clickWindowOffset, Settings.ExtraDelay);
                //Return cursor to center screen
                // I dont actually like this idea, it annoys eyes
                //var centerScreen = GameController.Window.GetWindowRectangle().Center;
                //Mouse.SetCursorPos(centerScreen);
            }
            else if (Settings.GroundChests)
            {
                ClickOnChests();
            }
            _amIWorking = false;
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

            _amIWorking = false;
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
            Mouse.SetCursorPosAndLeftClick(coords, 100);
            Mouse.SetCursorPos(pos.X, pos.Y);
        }
    }
}