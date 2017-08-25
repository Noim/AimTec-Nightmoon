namespace Flowers_Vladimir.MyBase
{
    #region

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Orbwalking;

    using System.Linq;

    #endregion

    internal class MyLogic
    {
        internal static Orbwalker Orbwalker { get; set; }

        internal static Aimtec.SDK.Spell Q { get; set; }
        internal static Aimtec.SDK.Spell W { get; set; }
        internal static Aimtec.SDK.Spell E { get; set; }
        internal static Aimtec.SDK.Spell R { get; set; }
        internal static Aimtec.SDK.Spell Ignite { get; set; }

        internal static SpellSlot IgniteSlot { get; set; } = SpellSlot.Unknown;

        internal static Obj_AI_Hero Me = ObjectManager.GetLocalPlayer();

        internal static Menu Menu { get; set; }
        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu ClearMenu { get; set; }
        internal static Menu LastHitMenu { get; set; }
        internal static Menu KillStealMenu { get; set; }
        internal static Menu EvadeMenu { get; set; }
        internal static Menu DrawMenu { get; set; }

        internal static int lastQTime { get; set; }
        internal static int lastETime { get; set; }

        internal static bool isQActive
            =>
               ObjectManager.GetLocalPlayer()
                   .Buffs.Any(
                       x =>
                           x.IsActive && x.Name.ToLower() == "vladimirqfrenzy");

        internal static bool isWActive
           =>
               ObjectManager.GetLocalPlayer().Buffs.Any(x => x.IsActive && x.Name.ToLower() == "vladimirsanguinepool");

        internal static bool isEActive
            =>
                ObjectManager.GetLocalPlayer().Buffs.Any(x => x.IsActive && x.Name.ToLower() == "vladimire") ||
                Game.TickCount - lastETime <= 1200 + Game.Ping;
    }
}