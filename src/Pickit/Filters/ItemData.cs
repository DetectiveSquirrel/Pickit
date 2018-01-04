#region Header

//-----------------------------------------------------------------
//   Class:          ItemData
//   Description:    Input item data for filter
//   Author:         Stridemann        Date: 08.26.2017
//-----------------------------------------------------------------

#endregion

using PoeHUD.Models;
using PoeHUD.Models.Enums;
using PoeHUD.Poe.Components;
using PoeHUD.Poe.Elements;
using PoeHUD.Poe.EntityComponents;

namespace Pickit.Filters
{
    public class ItemData
    {
        public string Path;
        public string ClassName;
        public string BaseName;
        public ItemRarity Rarity;
        public int ItemQuality;
        public bool BIdentified;
        public int ItemLevel;
        public int MapTier;
        public bool IsElder;
        public bool IsShaper;
        
        public ItemData(ItemsOnGroundLabelElement inventoryItem, BaseItemType baseItemType)
        {
            var item = inventoryItem.ItemOnGround;
            Path = item.Path;
            var @base = item.GetComponent<Base>();
            IsElder = @base.isElder;
            IsShaper = @base.isShaper;
            var mods = item.GetComponent<Mods>();
            Rarity = mods.ItemRarity;
            BIdentified = mods.Identified;
            ItemLevel = mods.ItemLevel;
            
            var quality = item.GetComponent<Quality>();
            ItemQuality = quality.ItemQuality;
            ClassName = baseItemType.ClassName;
            BaseName = baseItemType.BaseName;

            MapTier = item.HasComponent<PoeHUD.Poe.Components.Map>() ? item.GetComponent<PoeHUD.Poe.Components.Map>().Tier : 0;
        }
    }
}