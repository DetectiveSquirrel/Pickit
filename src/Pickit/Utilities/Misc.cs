using PoeHUD.Models;
using PoeHUD.Poe;
using System;
using CharacterData.Utils;

namespace Utilities
{
    public class Misc
    {
        public static int EntityDistance(EntityWrapper entity)
        {
            var Object = entity.GetComponent<PoeHUD.Poe.Components.Positioned>();
            var Distance = Math.Sqrt(Math.Pow(Player.X - Object.X, 2) + Math.Pow(Player.Y - Object.Y, 2));
            return (int)Distance;
        }

        public static int EntityDistance(Entity entity)
        {
            var Object = entity.GetComponent<PoeHUD.Poe.Components.Positioned>();
            var Distance = Math.Sqrt(Math.Pow(Player.X - Object.X, 2) + Math.Pow(Player.Y - Object.Y, 2));
            return (int)Distance;
        }
    }
}
