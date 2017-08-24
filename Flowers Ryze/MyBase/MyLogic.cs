using Aimtec.SDK.Extensions;

namespace Flowers_Ryze.MyBase
{
    #region

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Orbwalking;

    using System;
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
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawMenu { get; set; }

        internal static float Qcd { get; set; }
        internal static float QcdEnd { get; set; }
        internal static float Wcd { get; set; }
        internal static float WcdEnd { get; set; }
        internal static float Ecd { get; set; }
        internal static float EcdEnd { get; set; }
        internal static int LastCastTime { get; set; }
        internal static bool CanShield { get; set; }

        internal static bool HaveShield
            => ObjectManager.GetLocalPlayer().HasBuff("RyzeQShield");

        internal static bool NoStack
            => ObjectManager.GetLocalPlayer().HasBuff("ryzeqiconnocharge");

        internal static bool HalfStack
            => ObjectManager.GetLocalPlayer().HasBuff("ryzeqiconhalfcharge");

        internal static bool FullStack
            => ObjectManager.GetLocalPlayer().HasBuff("ryzeqiconfullcharge");
    }
}