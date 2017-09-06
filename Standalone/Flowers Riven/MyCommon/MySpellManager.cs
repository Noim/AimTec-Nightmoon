namespace Flowers_Riven.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Riven.MyBase;

    using System;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q, 325);
                MyLogic.Q.SetSkillshot(0.25f, 100f, 2200f, false, SkillshotType.Circle);

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W, 260f);

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E, 320f) { Delay = 0.1f };

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 900f);
                MyLogic.R.SetSkillshot(0.25f, 40f, 1600f, false, SkillshotType.Cone);

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
    }
}