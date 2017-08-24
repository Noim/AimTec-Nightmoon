namespace Flowers_Darius.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Darius.MyBase;

    using System;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q, 425f);

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W, 170f);

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E, 550f);
                MyLogic.E.SetSkillshot(0.20f, 100f, float.MaxValue, false, SkillshotType.Cone);

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 475f);

                MyLogic.IgniteSlot = ObjectManager.GetLocalPlayer().GetSpellSlot("summonerdot");

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