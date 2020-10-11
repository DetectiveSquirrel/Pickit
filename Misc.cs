using System;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;

namespace PickIt
{
    public class Misc
    {
        public static float EntityDistance(Entity entity, Entity player)
        {
            var component = entity?.GetComponent<Render>();

            if (component == null)
                return 9999999f;

            var objectPosition = component.Pos;

            return Vector3.Distance(objectPosition, player.GetComponent<Render>().Pos);
        }

        public static bool CanFitInventory(CustomItem groundItem)
        {
            return FindSpotInventory(groundItem) != new Vector2(-1, -1);
        }

        /* Container.FindSpot(item)
         *	Finds a spot available in the buffer to place the item.
         */
        public static Vector2 FindSpotInventory(CustomItem item)
        {
            var location = new Vector2(-1, -1);
            var inventory = GetInventoryArray();
            var width = 12;
            var height = 5;
            if (inventory == null)
                return location;

            PickIt.Controller.LogMessage($@"item.Width: {item.Width}", 5);
            PickIt.Controller.LogMessage($@"item.Height: {item.Height}", 5);

            for (var y = 0; y < height - item.Width; y++)
            {
                ItemDidntFitCellLoop:
                for (var x = 0; x < width - item.Height; x++)
                {
                    PickIt.Controller.LogMessage($@"inventory[Y{y}, X{x}]: {inventory[y, x]}", 5);
                    //Check if there is something in this spot.
                    if (inventory[y, x] != 0) continue;

                    for (var y2 = 0; y2 < item.Height; y++)
                    {
                        for (var x2 = 0; x2 < item.Width; x++)
                        {
                            PickIt.Controller.LogMessage($@"inventory[{y} + {y2}, {x} + {x2}]: {inventory[y + y2, x + x2]}", 5);
                            if (inventory[y + y2, x + x2] != 0)
                            {
                                PickIt.Controller.LogMessage(@"ItemDidntFitCellLoop Fail", 5);
                                goto ItemDidntFitCellLoop;
                            }
                        }
                    }

                    PickIt.Controller.LogMessage(@"Item Spot Check Pass", 5);
                    location = new Vector2(x, y);
                    return location;
                }
            }
            return location;
        }

        public static int[,] GetInventoryArray()
        {
            var inventoryCells = new[,]
            {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
            };

            try
            {
                var inventory = PickIt.Controller.GameController.Game.IngameState.ServerData.PlayerInventories[0].Inventory;
                foreach (var item in inventory.InventorySlotItems)
                {
                    var itemSizeX = item.SizeX;
                    var itemSizeY = item.SizeY;
                    var inventPosX = item.PosX; 
                    var inventPosY = item.PosY;
                    for (var y = 0; y < itemSizeY; y++)
                    {
                        for (var x = 0; x < itemSizeX; x++)
                        {
                            //PickIt.Controller.LogMessage(@"inventoryCells[y + inventPosY, x + inventPosX] = 1", 5);
                            inventoryCells[y + inventPosY, x + inventPosX] = 1;
                        }
                    }
                }

                return inventoryCells;
            }
            catch (Exception e)
            {
                // ignored
                PickIt.Controller.LogMessage(e.ToString(), 5);
            }

            return inventoryCells;
        }
    }
}
