using PoeHUD.Models;
using PoeHUD.Poe.Components;
using System;

namespace CharacterData.Utils
{
    public class Player
    {
        public static EntityWrapper Entity => PoeHUD.Plugins.BasePlugin.API.GameController.Player;
        public static long Experience => Entity.GetComponent<PoeHUD.Poe.Components.Player>().XP;
        public static String Name => Entity.GetComponent<PoeHUD.Poe.Components.Player>().PlayerName;
        public static float X => Entity.GetComponent<Positioned>().X;
        public static float Y => Entity.GetComponent<Positioned>().Y;
        public static int Level => Entity.GetComponent<PoeHUD.Poe.Components.Player>().Level;
        public static Life Health => Entity.GetComponent<Life>();
        public static bool HasBuff(string BuffName) { return Entity.GetComponent<Life>().HasBuff(BuffName); }
        public static AreaInstance Area => PoeHUD.Plugins.BasePlugin.API.GameController.Area.CurrentArea;
        public static int AreaHash => PoeHUD.Plugins.BasePlugin.API.GameController.Game.IngameState.Data.CurrentAreaHash;
    }
}