namespace Flowers_Ryze.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Ryze.MyBase;

    using System;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1000f);
                MyLogic.Q.SetSkillshot(0.25f, 50f, float.MaxValue, true, SkillshotType.Line);//Speed = 1700f

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W, 615f) {Delay = 0.35f};

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E, 600f) {Delay = 0.5f};

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 1500f);
                MyLogic.R.SetSkillshot(2.50f, 475f, float.MaxValue, false, SkillshotType.Circle);

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