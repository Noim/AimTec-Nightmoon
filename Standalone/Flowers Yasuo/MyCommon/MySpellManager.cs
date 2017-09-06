namespace Flowers_Yasuo.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Yasuo.MyBase;

    using System;
    using System.Linq;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q, 475f);
                MyLogic.Q.SetSkillshot(Q1Delay, 30, float.MaxValue, false, SkillshotType.Line);

                MyLogic.Q3 = new Aimtec.SDK.Spell(SpellSlot.Q, 1000f);
                MyLogic.Q3.SetSkillshot(Q3Delay, 90, 1200, false, SkillshotType.Line);

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W, 400f);

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E, 475f) {Delay = 0.075f, Speed = 1025};

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 1200f);

                MyLogic.IgniteSlot = ObjectManager.GetLocalPlayer().GetSpellSlotFromName("summonerdot");

                if (MyLogic.IgniteSlot != SpellSlot.Unknown)
                {
                    MyLogic.Ignite = new Aimtec.SDK.Spell(MyLogic.IgniteSlot, 600);
                }

                MyLogic.FlashSlot = ObjectManager.GetLocalPlayer().GetSpellSlotFromName("summonerflash");

                if (MyLogic.FlashSlot != SpellSlot.Unknown)
                {
                    MyLogic.Flash = new Aimtec.SDK.Spell(MyLogic.FlashSlot, 425);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MySpellManager.Initializer." + ex);
            }
        }

        private static float DefaultDelay => 1 - Math.Min((ObjectManager.GetLocalPlayer().AttackSpeedMod - 1) * 0.0058552631578947f, 0.6675f);

        private static float Q1Delay => 0.4f * DefaultDelay;

        private static float Q3Delay => 0.5f * DefaultDelay;
    }
}