namespace Flowers_Kalista.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Kalista.MyBase;

    using System;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1150f);
                MyLogic.Q.SetSkillshot(0.35f, 40f, 2400f, true, SkillshotType.Line);

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W, 5000f);

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E, 950f);

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 1500f);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MySpellManager.Initializer." + ex);
            }
        }
    }
}