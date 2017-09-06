namespace Flowers_Draven.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Draven.MyBase;

    using System;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q);

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W);

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E, 950f);
                MyLogic.E.SetSkillshot(0.25f, 100f, 1400f, false, SkillshotType.Line);

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 3000f);
                MyLogic.R.SetSkillshot(0.4f, 160f, 2000f, false, SkillshotType.Line);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MySpellManager.Initializer." + ex);
            }
        }
    }
}