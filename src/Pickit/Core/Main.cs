#region Header
/*
 * Idea from Qvin's auto pickup
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
using System.Threading;
using System.Windows.Forms;
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
                    PickItUp();
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

        private int GetEntityDistance(EntityWrapper ItemEntity)
        {
            var PlayerPosition = GameController.Player.GetComponent<Positioned>();
            var ItemPosition = ItemEntity.GetComponent<Positioned>();
            var DistanceToEntity = Math.Sqrt(Math.Pow(PlayerPosition.X - ItemPosition.X, 2) +
                                             Math.Pow(PlayerPosition.Y - ItemPosition.Y, 2));

            return (int)DistanceToEntity;
        }

        public bool InListNonUnique(Entity ItemEntity)
        {
            try
            {
                var ItemEntityName = GameController.Files.BaseItemTypes.Translate(ItemEntity.Path).BaseName;
                ItemRarity Rarity = ItemEntity.GetComponent<Mods>().ItemRarity;
                if (NonUniques.Contains(ItemEntityName) && Rarity != ItemRarity.Unique)
                    return true;
            }
            catch { }

            return false;
        }

        public bool InListUnique(Entity ItemEntity)
        {
            try
            {
                var ItemEntityName = GameController.Files.BaseItemTypes.Translate(ItemEntity.Path).BaseName;
                ItemRarity Rarity = ItemEntity.GetComponent<Mods>().ItemRarity;
                if (Uniques.Contains(ItemEntityName) && Rarity == ItemRarity.Unique)
                    return true;
            }
            catch { }

            return false;
        }

        public bool MiscChecks(Entity ItemEntity)
        {
            try
            {
                if (Settings.SixSocket && ItemEntity.GetComponent<Sockets>().NumberOfSockets == 6)
                    return true;
                if (Settings.SixLink && ItemEntity.GetComponent<Sockets>().LargestLinkSize == 6)
                    return true;
                if (Settings.RGB && ItemEntity.GetComponent<Sockets>().IsRGB)
                    return true;
                if (Settings.AllDivs && GameController.Files.BaseItemTypes.Translate(ItemEntity.Path).ClassName == "DivinationCard")
                    return true;
                if (Settings.AllCurrency && GameController.Files.BaseItemTypes.Translate(ItemEntity.Path).ClassName == "StackableCurrency")
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

        private void PickItUp()
        {
            if (Pick_Up_Timer.ElapsedMilliseconds < 100)
            {
                Am_I_Working = false;
                return;
            }
            Pick_Up_Timer.Restart();
            SortedByDistDropItems.Clear();


            foreach (var ItemEntity in entities)
            {
                Entity Item = null;
                if (ItemEntity.Path.ToLower().Contains("worlditem"))
                    Item = ItemEntity.GetComponent<WorldItem>().ItemEntity;



                if (Item == null) continue;
                var Skip = InListNonUnique(Item) || InListUnique(Item) || MiscChecks(Item);
                if (!Skip) continue;
                var Distance = GetEntityDistance(ItemEntity);
                var TupleList = new Tuple<int, long, EntityWrapper>(Distance, ItemEntity.Address, ItemEntity);
                SortedByDistDropItems.Add(TupleList);
            }


            var OrderedByDistance = SortedByDistDropItems.OrderBy(x => x.Item1).ToList();

            var _currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels
                .GroupBy(GroundLabel => GroundLabel.ItemOnGround.Address)
                .ToDictionary(y => y.Key, y => y.First());

            if (OrderedByDistance.Count == 0)
            {
                Am_I_Working = false;
                return;
            }

            foreach (var tuple in OrderedByDistance)
                if (_currentLabels.TryGetValue(tuple.Item2, out ItemsOnGroundLabelElement entityLabel))
                    if (entityLabel.IsVisible && (entityLabel.CanPickUp || entityLabel.MaxTimeForPickUp.TotalSeconds == 0))
                    {
                        var rect = entityLabel.Label.GetClientRect();
                        var vect = new Vector2(rect.Center.X, rect.Center.Y);
                        if (tuple.Item1 >= Settings.PickupRange.Value)
                        {
                            Am_I_Working = false;
                            return;
                        }
                        Thread.Sleep(5);
                        var vectWindow = GameController.Window.GetWindowRectangle();
                        if (vect.Y > vectWindow.Bottom || vect.Y < vectWindow.Top)
                        {
                            Am_I_Working = false;
                            return;
                        }
                        if (vect.X > vectWindow.Right || vect.X < vectWindow.Left)
                        {
                            Am_I_Working = false;
                            return;
                        }

                        SetCursorToEntityAndClick(vect);
                        break;
                    }


            Am_I_Working = false;
        }

        private void SetCursorToEntityAndClick(Vector2 rect)
        {
            var _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            var finalRect = rect + _clickWindowOffset;
            Mouse.SetCursorPosAndLeftClick(finalRect, 30);
        }
    }
}
