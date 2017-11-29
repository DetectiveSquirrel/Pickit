using System;
using PoeHUD.Models;
using PoeHUD.Poe;
using PoeHUD.Poe.Components;

namespace Pickit.Utilities
{
    public class Misc
    {
        public static int EntityDistance(EntityWrapper entity)
        {
            var Object = entity.GetComponent<Positioned>();
            var distance = Math.Sqrt(Math.Pow(Player.X - Object.X, 2) + Math.Pow(Player.Y - Object.Y, 2));
            return (int) distance;
        }

        public static int EntityDistance(Entity entity)
        {
            var Object = entity.GetComponent<Positioned>();
            var distance = Math.Sqrt(Math.Pow(Player.X - Object.X, 2) + Math.Pow(Player.Y - Object.Y, 2));
            return (int) distance;
        }
    }
}