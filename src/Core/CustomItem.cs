using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.RemoteMemoryObjects;
using System.Collections.Generic;
using System.Linq;
using Map = PoeHUD.Poe.Components.Map;

namespace Pickit.Core
{
    public class CustomItem
    {
        public string BaseName;
        public string ClassName;
        public LabelOnGround CompleteItem;
        public Entity GroundItem;
        public int Height;
        public bool IsElder;
        public bool IsIdentified;
        public bool IsRGB;
        public bool IsShaper;
        public bool IsWeapon;
        public int ItemLevel;
        public int LargestLink;
        public int MapTier;
        public string Path;
        public int Quality;
        public ItemRarity Rarity;
        public int Sockets;
        public int Width;
        public int Stack;
        public int FracturedMods;

        public CustomItem() { }

        public CustomItem(LabelOnGround item)
        {
            CompleteItem = item;
            var groundItem = item.ItemOnGround.GetComponent<WorldItem>().ItemEntity;
            GroundItem = groundItem;
            Path = groundItem.Path;
            var baseItemType = BasePlugin.API.GameController.Files.BaseItemTypes.Translate(Path);
            ClassName = baseItemType.ClassName;
            BaseName = baseItemType.BaseName;
            Width = baseItemType.Width;
            Height = baseItemType.Height;
            var WeaponClass = new List<string>
            {
                    "One Hand Mace",
                    "Two Hand Mace",
                    "One Hand Axe",
                    "Two Hand Axe",
                    "One Hand Sword",
                    "Two Hand Sword",
                    "Thrusting One Hand Sword",
                    "Bow",
                    "Claw",
                    "Dagger",
                    "Sceptre",
                    "Staff",
                    "Wand"
            };
            if (groundItem.HasComponent<Quality>())
            {
                var quality = groundItem.GetComponent<Quality>();
                Quality = quality.ItemQuality;
            }

            if (groundItem.HasComponent<Base>())
            {
                var @base = groundItem.GetComponent<Base>();
                IsElder = @base.isElder;
                IsShaper = @base.isShaper;
            }

            if (groundItem.HasComponent<Mods>())
            {
                var mods = groundItem.GetComponent<Mods>();
                Rarity = mods.ItemRarity;
                IsIdentified = mods.Identified;
                ItemLevel = mods.ItemLevel;
                FracturedMods = mods.FracturedMods;
            }

            if (groundItem.HasComponent<Sockets>())
            {
                try
                {
                    var sockets = groundItem.GetComponent<Sockets>();
                    IsRGB = sockets.IsRGB;
                    Sockets = sockets.NumberOfSockets;
                    LargestLink = sockets.LargestLinkSize;
                }
                catch { }
            }

            if (groundItem.HasComponent<Stack>())
            {
                var stack = groundItem.GetComponent<Stack>();
                Stack = stack.Size;
            }

            if (WeaponClass.Any(ClassName.Equals))
            {
                IsWeapon = true;
            }

            MapTier = groundItem.HasComponent<Map>() ? groundItem.GetComponent<Map>().Tier : 0;
        }
    }
}
