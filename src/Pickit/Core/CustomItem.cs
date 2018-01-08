using PoeHUD.Models.Enums;
using PoeHUD.Plugins;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using PoeHUD.Poe.EntityComponents;
using Map = PoeHUD.Poe.Components.Map;

namespace Pickit.Core
{
    public class CustomItem
    {
        public string BaseName;
        public string ClassName;
        public ItemsOnGroundLabelElement CompleteItem;
        public Entity GroundItem;
        public bool IsElder;
        public bool IsIdentified;
        public bool IsRGB;
        public bool IsShaper;
        public int ItemLevel;
        public int LargestLink;
        public int MapTier;
        public string Path;
        public int Quality;
        public ItemRarity Rarity;
        public int Sockets;

        public CustomItem()
        {
        }

        public CustomItem(ItemsOnGroundLabelElement item)
        {
            CompleteItem = item;

            var groundItem = item.ItemOnGround.GetComponent<WorldItem>().ItemEntity;
            GroundItem = groundItem;
            Path = groundItem.Path;
            var baseItemType = BasePlugin.API.GameController.Files.BaseItemTypes.Translate(Path);
            ClassName = baseItemType.ClassName;
            BaseName = baseItemType.BaseName;

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
            }

            if (groundItem.HasComponent<Sockets>())
            {
                var sockets = groundItem.GetComponent<Sockets>();
                IsRGB = sockets.IsRGB;
                Sockets = sockets.NumberOfSockets;
                LargestLink = sockets.LargestLinkSize;
            }

            MapTier = groundItem.HasComponent<Map>()
                ? groundItem.GetComponent<Map>().Tier
                : 0;
        }
    }
}