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
using ImGuiNET;
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
        public string MagicRuleFile;
        public string NormalRuleFile;
        public string RareRuleFile;
        public string UniqueRuleFile;

        public Main() => PluginName = "Pickit";
        private List<string> PickitFiles { get; set; }

        private string PickitConfigFileDirectory => LocalPluginDirectory + @"\" + PickitRuleDirectory;

        internal Memory Process { get; private set; }

        public override void Initialise()
        {
            LoadRuleFiles();
            Process = new Memory(GameController.Window.Process.Id);
        }
        
        public override void DrawSettingsMenu()
        {
            Settings.PickUpKey = ImGuiExtension.HotkeySelector("Pickup Key", Settings.PickUpKey);
            Settings.LeftClickToggleNode.Value = ImGuiExtension.Checkbox("Mouse Button: " + (Settings.LeftClickToggleNode ? "Left" : "Right"), Settings.LeftClickToggleNode);
            Settings.GroundChests.Value = ImGuiExtension.Checkbox("Click Chests If No Items Around", Settings.GroundChests);
            Settings.PickupRange.Value = ImGuiExtension.IntSlider("Pickup Radius", Settings.PickupRange);
            Settings.ChestRange.Value = ImGuiExtension.IntSlider("Chest Radius", Settings.ChestRange);
            Settings.ExtraDelay.Value = ImGuiExtension.IntSlider("Extra Click Delay", Settings.ExtraDelay);
            Settings.ClickItemTimerDelay.Value = ImGuiExtension.IntSlider("Pickup Delay", Settings.ClickItemTimerDelay);
            //Settings.OverrideItemPickup.Value = ImGuiExtension.Checkbox("Item Pickup Override", Settings.OverrideItemPickup); ImGui.SameLine();
            //ImGuiExtension.ToolTip("Override item.CanPickup\n\rDO NOT enable this unless you know what you're doing!");
            if (ImGui.CollapsingHeader("Pickit Rules", TreeNodeFlags.Framed | TreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Reload All Files")) LoadRuleFiles();
                Settings.NormalRuleFile = ImGuiExtension.ComboBox("Normal Rules", Settings.NormalRuleFile, PickitFiles, out var tempRef);
                if (tempRef) _normalRules = LoadPickit(Settings.NormalRuleFile);
                Settings.MagicRuleFile = ImGuiExtension.ComboBox("Magic Rules", Settings.MagicRuleFile, PickitFiles, out tempRef);
                if (tempRef) _magicRules = LoadPickit(Settings.MagicRuleFile);
                Settings.RareRuleFile = ImGuiExtension.ComboBox("Rare Rules", Settings.RareRuleFile, PickitFiles, out tempRef);
                if (tempRef) _rareRules = LoadPickit(Settings.RareRuleFile);
                Settings.UniqueRuleFile = ImGuiExtension.ComboBox("Unique Rules", Settings.UniqueRuleFile, PickitFiles, out tempRef);
                if (tempRef) _uniqueRules = LoadPickit(Settings.UniqueRuleFile);
            }

            if (ImGui.CollapsingHeader("Item Logic", TreeNodeFlags.Framed | TreeNodeFlags.DefaultOpen))
            {
                Settings.ShaperItems.Value = ImGuiExtension.Checkbox("Pickup Shaper Items", Settings.ShaperItems);
                ImGui.SameLine();
                Settings.ElderItems.Value = ImGuiExtension.Checkbox("Pickup Elder Items", Settings.ElderItems);
                if (ImGui.TreeNode("Links/Sockets/RGB"))
                {
                    Settings.RGB.Value = ImGuiExtension.Checkbox("RGB Items", Settings.RGB);
                    Settings.TotalSockets.Value = ImGuiExtension.IntSlider("##Sockets", Settings.TotalSockets);
                    ImGui.SameLine();
                    Settings.Sockets.Value = ImGuiExtension.Checkbox("Sockets", Settings.Sockets);
                    Settings.LargestLink.Value = ImGuiExtension.IntSlider("##Links", Settings.LargestLink);
                    ImGui.SameLine();
                    Settings.Links.Value = ImGuiExtension.Checkbox("Links", Settings.Links);
                    ImGui.Separator();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Overrides"))
                {
                    Settings.PickUpEverything.Value = ImGuiExtension.Checkbox("Pickup Everything", Settings.PickUpEverything);
                    Settings.AllDivs.Value = ImGuiExtension.Checkbox("All Divination Cards", Settings.AllDivs);
                    Settings.AllCurrency.Value = ImGuiExtension.Checkbox("All Currency", Settings.AllCurrency);
                    Settings.AllUniques.Value = ImGuiExtension.Checkbox("All Uniques", Settings.AllUniques);
                    Settings.QuestItems.Value = ImGuiExtension.Checkbox("Quest Items", Settings.QuestItems);
                    Settings.Maps.Value = ImGuiExtension.Checkbox("##Maps", Settings.Maps);
                    ImGui.SameLine();
                    if (ImGui.TreeNode("Maps"))
                    {
                        Settings.MapTier.Value = ImGuiExtension.IntSlider("Lowest Tier", Settings.MapTier);
                        Settings.UniqueMap.Value = ImGuiExtension.Checkbox("All Unique Maps", Settings.UniqueMap);
                        Settings.MapFragments.Value = ImGuiExtension.Checkbox("Fragments", Settings.MapFragments);
                        ImGui.Spacing();
                        ImGui.TreePop();
                    }

                    Settings.GemQuality.Value = ImGuiExtension.IntSlider("##Gems", "Lowest Quality", Settings.GemQuality);
                    ImGui.SameLine();
                    Settings.Gems.Value = ImGuiExtension.Checkbox("Gems", Settings.Gems);
                    ImGui.Separator();
                    ImGui.TreePop();
                }

                Settings.Rares.Value = ImGuiExtension.Checkbox("##Rares", Settings.Rares);
                ImGui.SameLine();
                if (ImGui.TreeNode("Rares"))
                {
                    Settings.RareJewels.Value = ImGuiExtension.Checkbox("Jewels", Settings.RareJewels);
                    Settings.RareRingsilvl.Value = ImGuiExtension.IntSlider("##RareRings", "Lowest iLvl", Settings.RareRingsilvl);
                    ImGui.SameLine();
                    Settings.RareRings.Value = ImGuiExtension.Checkbox("Rings", Settings.RareRings);
                    Settings.RareAmuletsilvl.Value = ImGuiExtension.IntSlider("##RareAmulets", "Lowest iLvl", Settings.RareAmuletsilvl);
                    ImGui.SameLine();
                    Settings.RareAmulets.Value = ImGuiExtension.Checkbox("Amulets", Settings.RareAmulets);
                    Settings.RareBeltsilvl.Value = ImGuiExtension.IntSlider("##RareBelts", "Lowest iLvl", Settings.RareBeltsilvl);
                    ImGui.SameLine();
                    Settings.RareBelts.Value = ImGuiExtension.Checkbox("Belts", Settings.RareBelts);
                    Settings.RareGlovesilvl.Value = ImGuiExtension.IntSlider("##RareGloves", "Lowest iLvl", Settings.RareGlovesilvl);
                    ImGui.SameLine();
                    Settings.RareGloves.Value = ImGuiExtension.Checkbox("Gloves", Settings.RareGloves);
                    Settings.RareBootsilvl.Value = ImGuiExtension.IntSlider("##RareBoots", "Lowest iLvl", Settings.RareBootsilvl);
                    ImGui.SameLine();
                    Settings.RareBoots.Value = ImGuiExtension.Checkbox("Boots", Settings.RareBoots);
                    Settings.RareHelmetsilvl.Value = ImGuiExtension.IntSlider("##RareHelmets", "Lowest iLvl", Settings.RareHelmetsilvl);
                    ImGui.SameLine();
                    Settings.RareHelmets.Value = ImGuiExtension.Checkbox("Helmets", Settings.RareHelmets);
                    Settings.RareArmourilvl.Value = ImGuiExtension.IntSlider("##RareArmours", "Lowest iLvl", Settings.RareArmourilvl);
                    ImGui.SameLine();
                    Settings.RareArmour.Value = ImGuiExtension.Checkbox("Armours", Settings.RareArmour);
                    ImGui.TreePop();
                }
            }
        }

        public override void Render()
        {
            try
            {
                if (!Keyboard.IsKeyDown((int) Settings.PickUpKey.Value)) return;
                if (_working) return;
                _working = true;
                FindItemToPick();
            }
            catch {
                // ignored
            }
        }

        public bool InCustomList(HashSet<string> checkList, CustomItem itemEntity, ItemRarity rarity)
        {
            try
            {
                if (checkList.Contains(itemEntity.BaseName) && itemEntity.Rarity == rarity) return true;
            }
            catch {
                // ignored
            }

            return false;
        }


        public bool OverrideChecks(CustomItem item)
        {
            try
            {
                #region Shaper & Elder

                if (Settings.ElderItems)
                    if (item.IsElder)
                        return true;
                if (Settings.ShaperItems)
                    if (item.IsShaper)
                        return true;

                #endregion

                #region Rare Overrides

                if (Settings.Rares && item.Rarity == ItemRarity.Rare)
                {
                    if (Settings.RareJewels && (item.ClassName == "Jewel" || item.ClassName == "AbyssJewel")) return true;
                    if (Settings.RareRings && item.ClassName == "Ring" && item.ItemLevel >= Settings.RareRingsilvl) return true;
                    if (Settings.RareAmulets && item.ClassName == "Amulet" && item.ItemLevel >= Settings.RareAmuletsilvl) return true;
                    if (Settings.RareBelts && item.ClassName == "Belt" && item.ItemLevel >= Settings.RareBeltsilvl) return true;
                    if (Settings.RareGloves && item.ClassName == "Gloves" && item.ItemLevel >= Settings.RareGlovesilvl) return true;
                    if (Settings.RareBoots && item.ClassName == "Boots" && item.ItemLevel >= Settings.RareBootsilvl) return true;
                    if (Settings.RareHelmets && item.ClassName == "Helmet" && item.ItemLevel >= Settings.RareHelmetsilvl) return true;
                    if (Settings.RareArmour && item.ClassName == "Body Armour" && item.ItemLevel >= Settings.RareArmourilvl) return true;
                }

                #endregion

                #region Sockets/Links/RGB

                if (Settings.Sockets && item.Sockets >= Settings.TotalSockets.Value) return true;
                if (Settings.Links && item.LargestLink >= Settings.LargestLink) return true;
                if (Settings.RGB && item.IsRGB) return true;

                #endregion

                #region Divination Cards

                if (Settings.AllDivs && item.ClassName == "DivinationCard") return true;

                #endregion

                #region Currency

                if (Settings.AllCurrency && item.ClassName == "StackableCurrency") return true;

                #endregion

                #region Maps

                if (Settings.Maps && item.MapTier >= Settings.MapTier.Value) return true;
                if (Settings.Maps && Settings.UniqueMap && item.MapTier >= 1 && item.Rarity == ItemRarity.Unique) return true;
                if (Settings.Maps && Settings.MapFragments && item.ClassName == "MapFragment") return true;

                #endregion

                #region Quest Items

                if (Settings.QuestItems && item.ClassName == "QuestItem") return true;

                #endregion

                #region Skill Gems

                if (Settings.Gems && item.Quality >= Settings.GemQuality.Value && item.ClassName.Contains("Skill Gem")) return true;

                #endregion

                #region Uniques

                if (Settings.AllUniques && item.Rarity == ItemRarity.Unique) return true;

                #endregion
            }
            catch
            {
                // ignored
            }

            return false;
        }

        public bool DoWePickThis(CustomItem itemEntity)
        {
            var pickItemUp = false;

            #region Force Pickup All

            if (Settings.PickUpEverything) return true;

            #endregion

            #region Rarity Rule Switch

            switch (itemEntity.Rarity)
            {
                case ItemRarity.Normal:
                    if (InCustomList(_normalRules, itemEntity, itemEntity.Rarity)) pickItemUp = true;
                    break;
                case ItemRarity.Magic:
                    if (InCustomList(_magicRules, itemEntity, itemEntity.Rarity)) pickItemUp = true;
                    break;
                case ItemRarity.Rare:
                    if (InCustomList(_rareRules, itemEntity, itemEntity.Rarity)) pickItemUp = true;
                    break;
                case ItemRarity.Unique:
                    if (InCustomList(_uniqueRules, itemEntity, itemEntity.Rarity)) pickItemUp = true;
                    break;
            }

            #endregion

            #region Override Rules

            if (OverrideChecks(itemEntity)) pickItemUp = true;

            #endregion

            return pickItemUp;
        }

        private void FindItemToPick()
        {
            var currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.Where(x => x.ItemOnGround.Path.ToLower().Contains("worlditem") && x.IsVisible && (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds <= 0))
                                              .Select(x => new Tuple<int, CustomItem>(Misc.EntityDistance(x.ItemOnGround), new CustomItem(x)))
                                              .OrderBy(x => x.Item1)
                                              .ToList();
            var pickUpThisItem = (from x in currentLabels where DoWePickThis(x.Item2) && x.Item1 < Settings.PickupRange select x).FirstOrDefault();
            if (pickUpThisItem != null)
            {
                if (TryToPick(pickUpThisItem.Item2)) return;
            }
            else if (Settings.GroundChests)
                ClickOnChests();

            _working = false;
        }

        private bool TryToPick(CustomItem pickUpThisItem)
        {
            if (Misc.EntityDistance(pickUpThisItem.CompleteItem.ItemOnGround) >= Settings.PickupRange)
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
            if (_pickUpTimer.ElapsedMilliseconds < Settings.ClickItemTimerDelay)
            {
                _working = false;
                return false;
            }
            _pickUpTimer.Restart();
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
                    var tuple = new Tuple<int, long, EntityWrapper>(Misc.EntityDistance(entity), entity.Address, entity);
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
            var chestScreenCoords = camera.WorldToScreen(entity.Pos.Translate(0, 0, 0), entity);
            if (chestScreenCoords == new Vector2()) return;
            var pos = Mouse.GetCursorPosition();
            var coords = new Vector2(chestScreenCoords.X, chestScreenCoords.Y);
            Mouse.SetCursorPosAndLeftOrRightClick(coords, 100, Settings.LeftClickToggleNode.Value);
            Mouse.SetCursorPos(pos.X, pos.Y);
        }

        #region (Re)Loading Rules

        private void LoadRuleFiles()
        {
            var dirInfo = new DirectoryInfo(PickitConfigFileDirectory);
            PickitFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            _normalRules = LoadPickit(Settings.NormalRuleFile);
            _magicRules = LoadPickit(Settings.MagicRuleFile);
            _rareRules = LoadPickit(Settings.RareRuleFile);
            _uniqueRules = LoadPickit(Settings.UniqueRuleFile);
        }

        public HashSet<string> LoadPickit(string fileName)
        {
            if (fileName == string.Empty) return null;
            var pickitFile = $@"{PluginDirectory}\{PickitRuleDirectory}\{fileName}.txt";
            if (!File.Exists(pickitFile)) return null;
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lines = File.ReadAllLines(pickitFile);
            lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).ForEach(x => hashSet.Add(x.Trim().ToLowerInvariant()));
            LogMessage($"PICKIT :: (Re)Loaded {fileName}", 5, Color.GreenYellow);
            return hashSet;
        }

        #endregion

        #region Adding / Removing Entities

        public override void EntityAdded(EntityWrapper entityWrapper) { _entities.Add(entityWrapper); }

        public override void EntityRemoved(EntityWrapper entityWrapper) { _entities.Remove(entityWrapper); }

        #endregion
    }
}