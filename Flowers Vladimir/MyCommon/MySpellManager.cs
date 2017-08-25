namespace Flowers_Vladimir.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Vladimir.MyBase;

    using System;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q, 600f);

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W, 350f);

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E, 610f);

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 625f);
                MyLogic.R.SetSkillshot(0.25f, 175f, 700f, false, SkillshotType.Circle);

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