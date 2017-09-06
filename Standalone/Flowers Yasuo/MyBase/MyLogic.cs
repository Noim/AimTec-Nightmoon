namespace Flowers_Yasuo.MyBase
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Orbwalking;

    #endregion

    internal class MyLogic
    {
        internal static Orbwalker Orbwalker { get; set; }

        internal static Aimtec.SDK.Spell Q { get; set; }
        internal static Aimtec.SDK.Spell Q3 { get; set; }
        internal static Aimtec.SDK.Spell W { get; set; }
        internal static Aimtec.SDK.Spell E { get; set; }
        internal static Aimtec.SDK.Spell R { get; set; }
        internal static Aimtec.SDK.Spell Flash { get; set; }
        internal static Aimtec.SDK.Spell Ignite { get; set; }


        internal static SpellSlot IgniteSlot { get; set; } = SpellSlot.Unknown;
        internal static SpellSlot FlashSlot { get; set; } = SpellSlot.Unknown;

        internal static Obj_AI_Hero Me = ObjectManager.GetLocalPlayer();

        internal static Menu Menu { get; set; }
        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu ClearMenu { get; set; }
        internal static Menu LastHitMenu { get; set; }
        internal static Menu FleeMenu { get; set; }
        internal static Menu KillStealMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu EvadeMenu { get; set; }
        internal static Menu DrawMenu { get; set; }

        internal static Vector3 YasuolastEPos { get; set; } = Vector3.Zero;
        internal static int YasuolastETime { get; set; } = 0;
        internal static int lastWTime { get; set; } = 0;
        internal static bool isYasuoDashing { get; set; } = false;
        internal static bool HaveQ3 => ObjectManager.GetLocalPlayer().HasBuff("YasuoQ3W");
        internal static bool IsMyDashing { get; set; } = false;
        internal static int YasuolastEQFlashTime { get; set; } = 0;
    }
}
