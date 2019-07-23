﻿#region Header

/*
 * Idea/Code from Qvin's auto pickup
 * Reworked into a more configurable version
*/

#endregion

using ImGuiNET;
using Pickit.Utilities;
using PoeHUD.Framework.Helpers;
using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Pickit.Core
{
    public class Main : BaseSettingsPlugin<Settings>
    {
        //https://stackoverflow.com/questions/826777/how-to-have-an-auto-incrementing-version-number-visual-studio
        public Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        public string PluginVersion;
        public DateTime buildDate;
        private const int PixelBorder = 3;
        private const string PickitRuleDirectory = "Pickit Rules";
        private const string ClickitRuleDirectory = "Clickit Rules";
        private readonly List<EntityWrapper> _entities = new List<EntityWrapper>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();
        private Vector2 _clickWindowOffset;
        private List<string> _magicRules;
        private List<string> _normalRules;
        private List<string> _rareRules;
        private List<string> _uniqueRules;
        private List<string> _questRules;
        private List<string> _miscRules;
        public string MagicRuleFile;
        public string NormalRuleFile;
        public string RareRuleFile;
        public string UniqueRuleFile;
        public string QuestRuleFile;
        public string MiscRuleFile;

        public Main() => PluginName = "Clickit";
        private List<string> PickitFiles { get; set; }
        private List<string> ClickitFiles { get; set; }
        public DateTime LastRenderTick { get; set; }

        private string PickitConfigFileDirectory => LocalPluginDirectory + @"\" + PickitRuleDirectory;
        private string ClickitConfigFileDirectory => LocalPluginDirectory + @"\" + ClickitRuleDirectory;

        public override void Initialise()
        {
            LoadRuleFiles();
            buildDate = new DateTime(2000, 1, 1).AddDays(version.Build).AddSeconds(version.Revision * 2);
            PluginVersion = $"{version}";
            LastRenderTick = DateTime.Now;
        }
        
        public override void DrawSettingsMenu()
        {
            ImGui.BulletText($"v{PluginVersion}");
            ImGui.BulletText($"Last Updated: {buildDate}");
            Settings.UpdatesPerSecond.Value = ImGuiExtension.IntSlider("Render Cycles Per Second", Settings.UpdatesPerSecond); ImGuiExtension.ToolTip("Set to 0 to disable");
            Settings.PickUpKey = ImGuiExtension.HotkeySelector("Pickup Key", Settings.PickUpKey);
            Settings.LeftClickToggleNode.Value = ImGuiExtension.Checkbox("Mouse Button: " + (Settings.LeftClickToggleNode ? "Left" : "Right"), Settings.LeftClickToggleNode);
            Settings.GroundChests.Value = ImGuiExtension.Checkbox("Click Chests If No Items Around", Settings.GroundChests);
            Settings.ClickitClickables.Value = ImGuiExtension.Checkbox("Click Misc Quest Things", Settings.ClickitClickables);
            Settings.PickupRange.Value = ImGuiExtension.IntSlider("Pickup Radius", Settings.PickupRange);
            Settings.ChestRange.Value = ImGuiExtension.IntSlider("Chest Radius", Settings.ChestRange);
            Settings.ClickitRange.Value = ImGuiExtension.IntSlider("Quest Radius", Settings.ClickitRange);
            Settings.ExtraDelay.Value = ImGuiExtension.IntSlider("Extra Click Delay", Settings.ExtraDelay);
            Settings.ClickItemTimerDelay.Value = ImGuiExtension.IntSlider("Pickup Delay", Settings.ClickItemTimerDelay);
            Settings.ShowPickupRange.Value = ImGuiExtension.Checkbox("Display Pickup Radius", Settings.ShowPickupRange);
            Settings.ShowChestRange.Value = ImGuiExtension.Checkbox("Display Chest Radius", Settings.ShowChestRange);
            Settings.ShowClickitRange.Value = ImGuiExtension.Checkbox("Display Quest Radius", Settings.ShowClickitRange);
            //Settings.OverrideItemPickup.Value = ImGuiExtension.Checkbox("Item Pickup Override", Settings.OverrideItemPickup); ImGui.SameLine();
            //ImGuiExtension.ToolTip("Override item.CanPickup\n\rDO NOT enable this unless you know what you're doing!");
            if (ImGui.Button("Reload All Files")) LoadRuleFiles();
            if (ImGui.CollapsingHeader("Pickit Rules", TreeNodeFlags.Framed | TreeNodeFlags.DefaultOpen))
            {
                ImGuiNative.igIndent();
                Settings.NormalRuleFile = ImGuiExtension.ComboBox("Normal Rules", Settings.NormalRuleFile, PickitFiles, out var tempRef);
                if (tempRef) _normalRules = LoadPickit(Settings.NormalRuleFile);
                Settings.MagicRuleFile = ImGuiExtension.ComboBox("Magic Rules", Settings.MagicRuleFile, PickitFiles, out tempRef);
                if (tempRef) _magicRules = LoadPickit(Settings.MagicRuleFile);
                Settings.RareRuleFile = ImGuiExtension.ComboBox("Rare Rules", Settings.RareRuleFile, PickitFiles, out tempRef);
                if (tempRef) _rareRules = LoadPickit(Settings.RareRuleFile);
                Settings.UniqueRuleFile = ImGuiExtension.ComboBox("Unique Rules", Settings.UniqueRuleFile, PickitFiles, out tempRef);
                if (tempRef) _uniqueRules = LoadPickit(Settings.UniqueRuleFile);
                ImGuiNative.igUnindent();
            }
            if (ImGui.CollapsingHeader("Clickit Rules", TreeNodeFlags.Framed | TreeNodeFlags.DefaultOpen))
            {
                ImGuiNative.igIndent();
                Settings.QuestRuleFile = ImGuiExtension.ComboBox("Quest Object Rules", Settings.QuestRuleFile, ClickitFiles, out var tempRef);
                if (tempRef) _questRules = LoadClickit(Settings.QuestRuleFile);
                Settings.MiscRuleFile = ImGuiExtension.ComboBox("Misc Object Rules", Settings.MiscRuleFile, ClickitFiles, out tempRef);
                if (tempRef) _miscRules = LoadClickit(Settings.MiscRuleFile);
                ImGuiNative.igUnindent();
            }

            if (ImGui.CollapsingHeader("Special Items", TreeNodeFlags.Framed | TreeNodeFlags.DefaultOpen))

            {
                Settings.PickupScraps.Value = ImGuiExtension.Checkbox("Pickup Veiled Mods", Settings.PickupScraps);
                Settings.PickupTrans.Value = ImGuiExtension.Checkbox("Pickup Incubators", Settings.PickupTrans);
            }
            if (ImGui.CollapsingHeader("Item Logic", TreeNodeFlags.Framed | TreeNodeFlags.DefaultOpen))
            {
                ImGuiNative.igIndent();


                if (ImGui.TreeNode("Scroll Limiter"))
                {
                    Settings.MaxScrollsToPickup.Value = ImGuiExtension.Checkbox("Set Limit Of Scrolls To Pickup || 0 = Disable Feature", Settings.MaxScrollsToPickup);
                    Settings.PickupPortal.Value = ImGuiExtension.Checkbox("Pickup Portal Scrolls", Settings.PickupPortal);
                    Settings.MaxScrollsToPickupAmount_Portal.Value = ImGuiExtension.IntSlider("Portal Scroll", Settings.MaxScrollsToPickupAmount_Portal);

                    Settings.PickupIdent.Value = ImGuiExtension.Checkbox("Pickup Wisdom Scrolls", Settings.PickupIdent);
                    Settings.MaxScrollsToPickupAmount_Ident.Value = ImGuiExtension.IntSlider("Scroll of Wisdom", Settings.MaxScrollsToPickupAmount_Ident);

                    Settings.PickupScraps.Value = ImGuiExtension.Checkbox("Pickup Armourer Scraps/Blacksmith Whetstone", Settings.PickupScraps);
                    Settings.PickupTrans.Value = ImGuiExtension.Checkbox("Pickup Orb of Transmutation", Settings.PickupTrans);
                    Settings.PickupAug.Value = ImGuiExtension.Checkbox("Pickup Orb of Augmentation", Settings.PickupAug);
                    ImGui.TreePop();
                }



                if (ImGui.TreeNode("Links/Sockets/RGB"))
                {
                    Settings.RGB.Value = ImGuiExtension.Checkbox("RGB Items", Settings.RGB);
                    Settings.RGBMinWidth.Value = ImGuiExtension.IntSlider("Minimum Width##RGBMinWidth", Settings.RGBMinWidth);
                    Settings.RGBMinHeight.Value = ImGuiExtension.IntSlider("Minimum Height##RGBMinHeight", Settings.RGBMinHeight);
                    Settings.RGBWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RGBWidth", Settings.RGBWidth);
                    Settings.RGBHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RGBHeight", Settings.RGBHeight);
                    ImGui.Spacing();

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
                    Settings.ShaperItems.Value = ImGuiExtension.Checkbox("Pickup Shaper Items", Settings.ShaperItems);
                    Settings.ElderItems.Value = ImGuiExtension.Checkbox("Pickup Elder Items", Settings.ElderItems);

                    Settings.MinimumFracturedLines.Value = ImGuiExtension.IntSlider("##MinimumFracturedLines", "Minimum Fractured Lines", Settings.MinimumFracturedLines);
                    ImGui.SameLine();
                    Settings.FracturedItemsToggle.Value = ImGuiExtension.Checkbox("Fractured Lines", Settings.FracturedItemsToggle);
                    ImGui.SameLine();
                    Settings.FracturedItemsToggleOverrideEverything.Value = ImGuiExtension.Checkbox("[Override Pickup Everything]", Settings.FracturedItemsToggleOverrideEverything);


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
                ImGui.SetNextTreeNodeOpen(true);
                if (ImGui.TreeNode("Rares"))
                {
                    Settings.RareUnidOnly.Value = ImGuiExtension.Checkbox("Pickup Unid Rares Only", Settings.RareUnidOnly);
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
                    ImGui.Spacing();
                    Settings.RareWeaponilvl.Value = ImGuiExtension.IntSlider("##RareWeapons", "Lowest iLvl", Settings.RareWeaponilvl);
                    ImGui.SameLine();
                    Settings.RareWeapon.Value = ImGuiExtension.Checkbox("Weapons", Settings.RareWeapon);

                    Settings.RareWeaponMinWidth.Value = ImGuiExtension.IntSlider("Minimum Width##RareWeaponWidth2", Settings.RareWeaponMinWidth);
                    Settings.RareWeaponMinHeight.Value = ImGuiExtension.IntSlider("Minimum Height##RareWeaponHeight2", Settings.RareWeaponMinHeight);

                    Settings.RareWeaponWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RareWeaponWidth", Settings.RareWeaponWidth);
                    Settings.RareWeaponHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RareWeaponHeight", Settings.RareWeaponHeight);
                    ImGui.TreePop();
                }
                ImGuiNative.igUnindent();
            }
        }

        public override void Render()
        {
            if (Settings.ShowPickupRange)
            {
                Vector3 pos = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Render>().Pos;
                DrawEllipseToWorld(pos, Settings.PickupRange.Value, 25, 2, Color.Red);
            }
            if (Settings.ShowChestRange)
            {
                Vector3 pos = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Render>().Pos;
                DrawEllipseToWorld(pos, Settings.ChestRange.Value, 25, 2, Color.Orange);
            }
            if (Settings.ShowClickitRange)
            {
                Vector3 pos = GameController.Game.IngameState.Data.LocalPlayer.GetComponent<Render>().Pos;
                DrawEllipseToWorld(pos, Settings.ClickitRange.Value, 25, 2, Color.LawnGreen);
            }
            try
            {

                if (Settings.UpdatesPerSecond != 0)
                {
                    if (LastRenderTick.AddMilliseconds(1000 / Settings.UpdatesPerSecond) > DateTime.Now)
                        return;

                    LastRenderTick = DateTime.Now;
                }

                if (!Keyboard.IsKeyDown((int) Settings.PickUpKey.Value))
                    return;
                FindItemToPick();
            }
            catch
            {
                // ignored
            }
        }

        public void DrawEllipseToWorld(Vector3 vector3Pos, int radius, int points, int lineWidth, Color color)
        {
            var camera = GameController.Game.IngameState.Camera;
            var plottedCirclePoints = new List<Vector3>();
            var slice = 2 * Math.PI / points;
            for (var i = 0; i < points; i++)
            {
                var angle = slice * i;
                var x = (decimal)vector3Pos.X + decimal.Multiply(radius, (decimal)Math.Cos(angle));
                var y = (decimal)vector3Pos.Y + decimal.Multiply(radius, (decimal)Math.Sin(angle));
                plottedCirclePoints.Add(new Vector3((float)x, (float)y, vector3Pos.Z));
            }

            var rndEntity = GameController.Entities.FirstOrDefault(x => x.HasComponent<Render>() && GameController.Player.Address != x.Address);
            for (var i = 0; i < plottedCirclePoints.Count; i++)
            {
                if (i >= plottedCirclePoints.Count - 1)
                {
                    var pointEnd1 = camera.WorldToScreen(plottedCirclePoints.Last(), rndEntity);
                    var pointEnd2 = camera.WorldToScreen(plottedCirclePoints[0], rndEntity);
                    Graphics.DrawLine(pointEnd1, pointEnd2, lineWidth, color);
                    return;
                }

                var point1 = camera.WorldToScreen(plottedCirclePoints[i], rndEntity);
                var point2 = camera.WorldToScreen(plottedCirclePoints[i + 1], rndEntity);
                Graphics.DrawLine(point1, point2, lineWidth, color);
            }
        }

        public bool InCustomList(List<string> checkList, CustomItem itemEntity, ItemRarity rarity)
        {
            try
            {
                if (checkList.Contains(itemEntity.BaseName.ToLower()) && itemEntity.Rarity == rarity) return true;
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

                #region Fractured Mods

                if (Settings.FracturedItemsToggle)
                    if (item.FracturedMods >= Settings.MinimumFracturedLines)
                        return true;

                #endregion

                #region Rare Overrides

                if (Settings.Rares && item.Rarity == ItemRarity.Rare)
                {
                    if (Settings.RareUnidOnly.Value)
                    {
                        if (!item.IsIdentified)
                        {
                            if (Settings.RareJewels && (item.ClassName == "Jewel" || item.ClassName == "AbyssJewel")) return true;
                            if (Settings.RareRings && item.ClassName == "Ring" && item.ItemLevel >= Settings.RareRingsilvl) return true;
                            if (Settings.RareAmulets && item.ClassName == "Amulet" && item.ItemLevel >= Settings.RareAmuletsilvl) return true;
                            if (Settings.RareBelts && item.ClassName == "Belt" && item.ItemLevel >= Settings.RareBeltsilvl) return true;
                            if (Settings.RareGloves && item.ClassName == "Gloves" && item.ItemLevel >= Settings.RareGlovesilvl) return true;
                            if (Settings.RareBoots && item.ClassName == "Boots" && item.ItemLevel >= Settings.RareBootsilvl) return true;
                            if (Settings.RareHelmets && item.ClassName == "Helmet" && item.ItemLevel >= Settings.RareHelmetsilvl) return true;
                            if (Settings.RareArmour && item.ClassName == "Body Armour" && item.ItemLevel >= Settings.RareArmourilvl) return true;
                            if (Settings.RareWeapon && item.IsWeapon && item.ItemLevel >= Settings.RareWeaponilvl
                                && item.Width >= Settings.RareWeaponMinWidth && item.Height >= Settings.RareWeaponMinHeight
                                && item.Width <= Settings.RareWeaponWidth && item.Height <= Settings.RareWeaponHeight) return true;
                        }
                    }
                    else
                    {
                        if (Settings.RareJewels && (item.ClassName == "Jewel" || item.ClassName == "AbyssJewel")) return true;
                        if (Settings.RareRings && item.ClassName == "Ring" && item.ItemLevel >= Settings.RareRingsilvl) return true;
                        if (Settings.RareAmulets && item.ClassName == "Amulet" && item.ItemLevel >= Settings.RareAmuletsilvl) return true;
                        if (Settings.RareBelts && item.ClassName == "Belt" && item.ItemLevel >= Settings.RareBeltsilvl) return true;
                        if (Settings.RareGloves && item.ClassName == "Gloves" && item.ItemLevel >= Settings.RareGlovesilvl) return true;
                        if (Settings.RareBoots && item.ClassName == "Boots" && item.ItemLevel >= Settings.RareBootsilvl) return true;
                        if (Settings.RareHelmets && item.ClassName == "Helmet" && item.ItemLevel >= Settings.RareHelmetsilvl) return true;
                        if (Settings.RareArmour && item.ClassName == "Body Armour" && item.ItemLevel >= Settings.RareArmourilvl) return true;
                        if (Settings.RareWeapon && item.IsWeapon && item.ItemLevel >= Settings.RareWeaponilvl
                            && item.Width >= Settings.RareWeaponMinWidth && item.Height >= Settings.RareWeaponMinHeight
                            && item.Width <= Settings.RareWeaponWidth && item.Height <= Settings.RareWeaponHeight) return true;
                    }
                }

                #endregion

                #region Sockets/Links/RGB

                if (Settings.Sockets && item.Sockets >= Settings.TotalSockets.Value) return true;
                if (Settings.Links && item.LargestLink >= Settings.LargestLink) return true;
                if (Settings.RGB && item.IsRGB && item.Height >= Settings.RGBMinHeight && item.Width >= Settings.RGBMinWidth && item.Height <= Settings.RGBHeight && item.Width <= Settings.RGBWidth) return true;

                #endregion

                #region Divination Cards

                if (Settings.AllDivs && item.ClassName == "DivinationCard") return true;

                #endregion

                #region Currency

                if (Settings.AllCurrency && item.ClassName == "StackableCurrency" || item.ClassName == "DelveSocketableCurrency")
                {
                    if (Settings.MaxScrollsToPickup)
                        if (item.BaseName == "Scroll of Wisdom" || item.BaseName == "Portal Scroll")
                        {
                            List<Entity> stash = (from serverDataPlayerInventory in GameController.Game.IngameState.ServerData.PlayerInventories
                                    where serverDataPlayerInventory.Inventory.InventType == InventoryTypeE.MainInventory
                                    from entity
                                        in serverDataPlayerInventory.Inventory.Items
                                    select entity)
                                .ToList();

                            int identCount = 0;
                            int portalCount = 0;

                            // Get current count of our scrolls
                            foreach (Entity entity in stash)
                                if (entity.Path == "Metadata/Items/Currency/CurrencyIdentification")
                                    identCount += entity.GetComponent<Stack>().Size;
                                else if (entity.Path == "Metadata/Items/Currency/CurrencyPortal")
                                    portalCount += entity.GetComponent<Stack>().Size;

                            //LogMessage($"Portal={portalCount} || Id={identCount}", 10);

                            if (item.BaseName == "Scroll of Wisdom")
                            {
                                if (Settings.MaxScrollsToPickupAmount_Ident > 0 && identCount + item.Stack > Settings.MaxScrollsToPickupAmount_Ident) return false;
                            }
                            else if (item.BaseName == "Portal Scroll")
                            {
                                if (Settings.MaxScrollsToPickupAmount_Portal > 0 && portalCount + item.Stack > Settings.MaxScrollsToPickupAmount_Portal) return false;
                            }
                        }

                    return true;
                }

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
            if (itemEntity.CompleteItem.Address == 0) return false;

            var pickItemUp = false;

            #region Force Pickup All + Overrides of pickup all

            // Overrides before pickup everything, some users *cough* snapz uses pickup everything toggle
            if (Settings.FracturedItemsToggle && Settings.FracturedItemsToggleOverrideEverything)
            {
                return itemEntity.FracturedMods >= Settings.MinimumFracturedLines;
            }
            if (Settings.PickUpEverything)
            {
                return true;
            }

            #endregion

            #region Rarity Rule Switch


            switch (itemEntity.Rarity)
            {
                case ItemRarity.Normal:
                    if (InCustomList(_normalRules, itemEntity, itemEntity.Rarity))
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

            #endregion

            #region Override Rules

            if (OverrideChecks(itemEntity)) pickItemUp = true;

            #endregion

            return pickItemUp;
        }

        private void FindItemToPick()
        {
            List<Tuple<int, CustomItem>> currentLabels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabels.ToList().Where(x => x.Address != 0 && x.ItemOnGround.Path.ToLower().Contains("worlditem") && x.IsVisible && (x.CanPickUp || x.MaxTimeForPickUp.TotalSeconds <= 0))
                .Select(x => new Tuple<int, CustomItem>(Misc.EntityDistance(x.ItemOnGround), new CustomItem(x)))
                .OrderBy(x => x.Item1)
                .ToList();
            Tuple<int, CustomItem> pickUpThisItem = (from x in currentLabels where DoWePickThis(x.Item2) && x.Item1 < Settings.PickupRange select x).FirstOrDefault();
            if (pickUpThisItem != null)
                TryToPick(pickUpThisItem.Item2);
            else if (Settings.GroundChests)
                ClickOnChests();
            // Lets face it, no one wants to do this themself
            else if (Settings.ClickitClickables)
                ClickMiscObjects();
        }

        private void TryToPick(CustomItem pickUpThisItem)
        {
            if (pickUpThisItem.CompleteItem.Address == 0) return;

            if (Misc.EntityDistance(pickUpThisItem.CompleteItem.ItemOnGround) >= Settings.PickupRange)
                return;

            Vector2 vect = pickUpThisItem.CompleteItem.Label.GetClientRect().Center;
            RectangleF vectWindow = GameController.Window.GetWindowRectangle();
            _clickWindowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            vect += _clickWindowOffset;
            if (vect.Y + PixelBorder > vectWindow.Bottom || vect.Y - PixelBorder < vectWindow.Top)
                return;

            if (vect.X + PixelBorder > vectWindow.Right || vect.X - PixelBorder < vectWindow.Left)
                return;

            long address = pickUpThisItem.CompleteItem.ItemOnGround.GetComponent<Targetable>().Address;
            bool isTargeted = address != 0 && Memory.ReadBytes(address + 0x30, 4)[2] == 1;
            //var isTargeted = address != 0 && Memory.ReadByte(address + 0x30)[3] == 1;
            Mouse.SetCursorPos(vect);
            if (isTargeted && _pickUpTimer.ElapsedMilliseconds >= Settings.ClickItemTimerDelay)
            {
                _pickUpTimer.Restart();
                if (Settings.LeftClickToggleNode)
                    Mouse.LeftClick(Settings.ExtraDelay);
                else
                    Mouse.RightClick(Settings.ExtraDelay);
            }
        }

        // Copy-Paste - Qvin0000's version
        private void ClickOnChests()
        {
            List<Tuple<int, long, EntityWrapper>> sortedByDistChest = new List<Tuple<int, long, EntityWrapper>>();
            foreach (EntityWrapper entity in _entities)
                if (entity.Path.ToLower().Contains("chests") && entity.IsAlive && entity.IsHostile)
                {
                    if (!entity.HasComponent<Chest>()) continue;
                    Chest chest = entity.GetComponent<Chest>();
                    if (chest.IsStrongbox) continue;
                    if (chest.IsOpened) continue;
                    Tuple<int, long, EntityWrapper> tuple = new Tuple<int, long, EntityWrapper>(Misc.EntityDistance(entity), entity.Address, entity);
                    if (sortedByDistChest.Any(x => x.Item2 == entity.Address)) continue;
                    sortedByDistChest.Add(tuple);
                }

            List<Tuple<int, long, EntityWrapper>> tempList = sortedByDistChest.OrderBy(x => x.Item1).ToList();
            if (tempList.Count <= 0) return;
            if (tempList[0].Item1 >= Settings.ChestRange) return;
            Camera camera = GameController.Game.IngameState.Camera;
            Vector2 chestScreenCoords = camera.WorldToScreen(tempList[0].Item3.Pos.Translate(0, 0, 0), tempList[0].Item3);
            if (chestScreenCoords == new Vector2()) return;
            Vector2 coords = new Vector2(chestScreenCoords.X, chestScreenCoords.Y);
            Mouse.SetCursorPos(coords);
            bool isTargeted = tempList[0].Item3.GetComponent<Targetable>().isTargeted;
            Mouse.SetCursorPos(coords);
            if (!isTargeted)
                return;

            if (_pickUpTimer.ElapsedMilliseconds < Settings.ClickItemTimerDelay)
                return;
            _pickUpTimer.Restart();
            if (Settings.LeftClickToggleNode)
                Mouse.LeftClick(Settings.ExtraDelay);
            else
                Mouse.RightClick(Settings.ExtraDelay);
        }

        private void ClickMiscObjects()
        {
            List<Tuple<int, long, EntityWrapper>> sortedDistance = new List<Tuple<int, long, EntityWrapper>>();
            List<string> list = new List<string>();
            list.AddRange(_miscRules);
            list.AddRange(_questRules);
            foreach (EntityWrapper entity in _entities)
            {
                foreach (string questChest in list)
                {
                    if (!entity.Path.ToLower().Contains(questChest.ToLower()))
                        continue;
                    Targetable targetComp = entity.GetComponent<Targetable>();
                    if (!targetComp.isTargetable) continue;
                    Tuple<int, long, EntityWrapper> tuple = new Tuple<int, long, EntityWrapper>(Misc.EntityDistance(entity), entity.Address, entity);
                    if (sortedDistance.Any(x => x.Item2 == entity.Address)) continue;
                    sortedDistance.Add(tuple);
                }
            }

            List<Tuple<int, long, EntityWrapper>> tempList = sortedDistance.OrderBy(x => x.Item1).ToList();
            if (tempList.Count <= 0)
                return;

            foreach (Tuple<int, long, EntityWrapper> tuple in tempList)
            {
                if (tuple.Item1 >= Settings.ClickitRange) continue;
                Camera camera = GameController.Game.IngameState.Camera;
                Vector2 chestScreenCoords = camera.WorldToScreen(tuple.Item3.Pos.Translate(0, 0, 0), tuple.Item3);
                Vector2 coords = new Vector2(chestScreenCoords.X, chestScreenCoords.Y);
                Mouse.SetCursorPos(coords);
                bool isTargeted = tuple.Item3.GetComponent<Targetable>().isTargeted;
                Mouse.SetCursorPos(coords);
                if (!isTargeted)
                    continue;
                if (_pickUpTimer.ElapsedMilliseconds < Settings.ClickItemTimerDelay)
                    continue;
                _pickUpTimer.Restart();
                if (Settings.LeftClickToggleNode)
                    Mouse.LeftClick(Settings.ExtraDelay);
                else
                    Mouse.RightClick(Settings.ExtraDelay);
                break;
            }
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

            dirInfo = new DirectoryInfo(ClickitConfigFileDirectory);
            ClickitFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            _questRules = LoadClickit(Settings.QuestRuleFile);
            _miscRules = LoadClickit(Settings.MiscRuleFile);
        }

        public List<string> LoadPickit(string fileName)
        {
            if (fileName == string.Empty) return null;
            var pickitFile = $@"{PluginDirectory}\{PickitRuleDirectory}\{fileName}.txt";
            if (!File.Exists(pickitFile)) return null;
            var hashSet = new List<string>();
            var lines = File.ReadAllLines(pickitFile);
            lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).ForEach(x => hashSet.Add(x.Trim().ToLowerInvariant()));
            LogMessage($"Pickit :: (Re)Loaded - {fileName}", 5, Color.GreenYellow);
            return hashSet;
        }

        public List<string> LoadClickit(string fileName)
        {
            if (fileName == string.Empty) return null;
            var pickitFile = $@"{PluginDirectory}\{ClickitRuleDirectory}\{fileName}.txt";
            if (!File.Exists(pickitFile)) return null;
            var hashSet = new List<string>();
            var lines = File.ReadAllLines(pickitFile);
            lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")).ForEach(x => hashSet.Add(x.Trim().ToLowerInvariant()));
            LogMessage($"Clickit :: (Re)Loaded - {fileName}", 5, Color.GreenYellow);
            return hashSet;
        }

        #endregion

        #region Adding / Removing Entities

        public override void EntityAdded(EntityWrapper entityWrapper) { _entities.Add(entityWrapper); }

        public override void EntityRemoved(EntityWrapper entityWrapper) { _entities.Remove(entityWrapper); }

        #endregion
    }
}