#region Header
/*
 * Idea/Code from Qvin's auto pickup
 * Reworked into a more configurable version
*/
#endregion

using PoeHUD.Controllers;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Utilities;

namespace Pickit
{
    public class Main : BaseSettingsPlugin<Settings>
    {
        private readonly Stopwatch Pick_Up_Timer = Stopwatch.StartNew();
        private readonly List<Tuple<int, long, EntityWrapper>> SortedByDistDropItems = new List<Tuple<int, long, EntityWrapper>>();
        private readonly List<EntityWrapper> entities = new List<EntityWrapper>();
        private bool Am_I_Working;

        private HashSet<string> NonUniques;
        private HashSet<string> Uniques;
        private Vector2 _clickWindowOffset;
        private readonly int PIXEL_BORDER = 3;

        public Main()
        {
            PluginName = "Pickit";
        }

        public override void Initialise()
        {
            NonUniques = LoadPickit("Non Uniques");
            Uniques = LoadPickit("Uniques");
        }

        public override void Render()
        {
            //base.Render();

            try
            {
                if (Keyboard.IsKeyDown((int)Settings.PickUpKey.Value))
                {
                    if (Am_I_Working)
                        return;
                    Am_I_Working = true;
                    NewPickUp();
                }
            }
            catch { }
        }

        public HashSet<string> LoadPickit(string fileName)
        {
            string PickitFile = $@"{PluginDirectory}\Pickit\{fileName}.txt";
            if (!File.Exists(PickitFile))
            {
                return null;
            }
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] lines = File.ReadAllLines(PickitFile);
            lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).ForEach(x => hashSet.Add(x.Trim().ToLowerInvariant()));
            return hashSet;
        }

        private int GetEntityDistance(EntityWrapper entity)
        {
            var PlayerPosition = GameController.Player.GetComponent<Positioned>();
            var MonsterPosition = entity.GetComponent<Positioned>();
            var distanceToEntity = Math.Sqrt(Math.Pow(PlayerPosition.X - MonsterPosition.X, 2) +
                                             Math.Pow(PlayerPosition.Y - MonsterPosition.Y, 2));

            return (int)distanceToEntity;
        }
        private int GetEntityDistance(Entity entity)
        {
            var PlayerPosition = GameController.Player.GetComponent<Positioned>();
            var MonsterPosition = entity.GetComponent<Positioned>();
            var distanceToEntity = Math.Sqrt(Math.Pow(PlayerPosition.X - MonsterPosition.X, 2) +
                                             Math.Pow(PlayerPosition.Y - MonsterPosition.Y, 2));

            return (int)distanceToEntity;
        }

        public bool InListNonUnique(ItemsOnGroundLabelElement ItemEntity)
        {
            try
            {
                var Item = ItemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                var ItemEntityName = GameController.Files.BaseItemTypes.Translate(Item.Path).BaseName;
                ItemRarity Rarity = Item.GetComponent<Mods>().ItemRarity;
                if (NonUniques.Contains(ItemEntityName) && Rarity != ItemRarity.Unique)
                    return true;
            }
            catch { }

            return false;
        }

        public bool InListUnique(ItemsOnGroundLabelElement ItemEntity)
        {
            try
            {
                var Item = ItemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                var ItemEntityName = GameController.Files.BaseItemTypes.Translate(Item.Path).BaseName;
                ItemRarity Rarity = Item.GetComponent<Mods>().ItemRarity;
                if (Uniques.Contains(ItemEntityName) && Rarity == ItemRarity.Unique)
                    return true;
            }
            catch { }

            return false;
        }

        public bool MiscChecks(ItemsOnGroundLabelElement ItemEntity)
        {
            try
            {
                var Item = ItemEntity.ItemOnGround.GetComponent<WorldItem>().ItemEntity;

                if (Settings.SixSocket && Item.GetComponent<Sockets>().NumberOfSockets == 6)
                    return true;
                if (Settings.SixLink && Item.GetComponent<Sockets>().LargestLinkSize == 6)
                    return true;
                if (Settings.RGB && Item.GetComponent<Sockets>().IsRGB)
                    return true;
                if (Settings.AllDivs && GameController.Files.BaseItemTypes.Translate(Item.Path).ClassName == "DivinationCard")
                    return true;
                if (Settings.AllCurrency && GameController.Files.BaseItemTypes.Translate(Item.Path).ClassName == "StackableCurrency")
                    return true;
                if (Settings.AllUniques && Item.GetComponent<Mods>().ItemRarity == ItemRarity.Unique)
                    return true;
                if (Settings.Maps && Item.GetComponent<PoeHUD.Poe.Components.Map>().Tier >= Settings.MapTier.Value)
                    return true;
                if (Settings.Maps && Item.GetComponent<PoeHUD.Poe.Components.Map>().Tier >= Settings.MapTier.Value)
                    return true;
                if (Settings.Maps && Settings.MapFragments && GameController.Files.BaseItemTypes.Translate(Item.Path).ClassName == "MapFragment")
                    return true;
                if (Settings.Maps && Settings.UniqueMap && Item.GetComponent<PoeHUD.Poe.Components.Map>().Tier >= 1 && Item.GetComponent<Mods>().ItemRarity == ItemRarity.Unique)
                    return true;
                if (Settings.QuestItems && GameController.Files.BaseItemTypes.Translate(Item.Path).ClassName == "QuestItem")
                    return true;
                if (Settings.Gems && Item.GetComponent<Quality>().ItemQuality >= Settings.GemQuality.Value && GameController.Files.BaseItemTypes.Translate(Item.Path).ClassName.Contains("Skill Gem"))
                    return true;
            }
            catch { }

            return false;
        }

        public override void EntityAdded(EntityWrapper entityWrapper)
        {
            entities.Add(entityWrapper);
        }

        public override void EntityRemoved(EntityWrapper entityWrapper)
        {
            entities.Remove(entityWrapper);
        }
        private void NewPickUp()
        {
            if (Pick_Up_Timer.ElapsedMilliseconds < Settings.PickupTimerDelay)
            {
                Am_I_Working = false;
                return;
            }
            Pick_Up_Timer.Restart();

            var currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                .Where(x => x.ItemOnGround.Path.ToLower().Contains("worlditem") && x.IsVisible && (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds == 0))
                .Select(x => new Tuple<int, ItemsOnGroundLabelElement>(GetEntityDistance(x.ItemOnGround), x))
                .OrderBy(x => x.Item1)
                .ToList();


            var pickUpThisItem = (from x in currentLabels
                                  where (InListNonUnique(x.Item2) || InListUnique(x.Item2) || MiscChecks(x.Item2))
                                  && x.Item1 < Settings.PickupRange
                                  select x).FirstOrDefault();

            if (pickUpThisItem != null)
            {
                if (pickUpThisItem.Item1 >= Settings.PickupRange)
                {
                    Am_I_Working = false;
                    return;
                }
                var vect = pickUpThisItem.Item2.Label.GetClientRect().Center;
                var vectWindow = GameController.Window.GetWindowRectangle();
                if (vect.Y + PIXEL_BORDER > vectWindow.Bottom || vect.Y - PIXEL_BORDER < vectWindow.Top)
                {
                    Am_I_Working = false;
                    return;
                }
                if (vect.X + PIXEL_BORDER > vectWindow.Right || vect.X - PIXEL_BORDER < vectWindow.Left)
                {
                    Am_I_Working = false;
                    return;
                }
                _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
                Mouse.SetCursorPosAndLeftClick(vect + _clickWindowOffset, Settings.ExtraDelay);
                //Return cursor to center screen
                // I dont actually like this idea, it annoys eyes
                //var centerScreen = GameController.Window.GetWindowRectangle().Center;
                //Mouse.SetCursorPos(centerScreen);
            }
            else if(Settings.GroundChests)
            {
                ClickOnChests();
            }
            Am_I_Working = false;
        }

        // Copy-Paste - Qvin0000's version
        private void ClickOnChests()
        {
            var sortedByDistChest = new List<Tuple<int, long, EntityWrapper>>();

            foreach (var entity in entities)
            {
                if (entity.Path.ToLower().Contains("chests") && entity.IsAlive && entity.IsHostile)
                {
                    if (!entity.HasComponent<Chest>()) continue;
                    var ch = entity.GetComponent<Chest>();
                    if (ch.IsStrongbox) continue;
                    if (ch.IsOpened) continue;
                    var d = GetEntityDistance(entity);

                    var t = new Tuple<int, long, EntityWrapper>(d, entity.Address, entity);
                    if (sortedByDistChest.Any(x => x.Item2 == entity.Address)) continue;

                    sortedByDistChest.Add(t);
                }
            }

            var tempList = sortedByDistChest.OrderBy(x => x.Item1).ToList();
            if (tempList.Count <= 0) return;
            if (tempList[0].Item1 >= Settings.ChestRange) return;
            SetCursorToEntityAndClick(tempList[0].Item3);
            var centerScreen = GameController.Window.GetWindowRectangle().Center;
            Mouse.SetCursorPos(centerScreen);

            Am_I_Working = false;

        }
        //Copy-Paste - Sithylis_QoL
        private void SetCursorToEntityAndClick(EntityWrapper entity)
        {
            var camera = GameController.Game.IngameState.Camera;
            var chestScreenCoords =
                camera.WorldToScreen(entity.Pos.Translate(0, 0, 0), entity);
            if (chestScreenCoords != new Vector2())
            {
                var pos = Mouse.GetCursorPosition();
                var iconRect1 = new Vector2(chestScreenCoords.X, chestScreenCoords.Y);
                Mouse.SetCursorPosAndLeftClick(iconRect1, 100);
                Mouse.SetCursorPos(pos.X, pos.Y);

            }
        }

        private void SetCursorToEntityAndClick(Vector2 rect)
        {
            var _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            var finalRect = rect + _clickWindowOffset;
            Mouse.SetCursorPosAndLeftClick(finalRect, 30);
        }
    }
}
