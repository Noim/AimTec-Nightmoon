namespace Flowers_Fiora.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Fiora.MyBase;

    using System;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q, 400f + 350f);
                MyLogic.Q.SetSkillshot(0.25f, 50f, 1200f, false, SkillshotType.Line);

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W, 400f);
                MyLogic.W.SetSkillshot(0.5f, 80f, 2000f, false, SkillshotType.Line);

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E);

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 500f);

                MyLogic.IgniteSlot = ObjectManager.GetLocalPlayer().GetSpellSlotFromName("summonerdot");

                if (MyLogic.IgniteSlot != SpellSlot.Unknown)
                {
                    MyLogic.Ignite = new Aimtec.SDK.Spell(MyLogic.IgniteSlot, 600);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MySpellManager.Initializer." + ex);
            }
        }
    }
}