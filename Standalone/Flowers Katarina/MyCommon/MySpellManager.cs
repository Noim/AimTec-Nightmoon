namespace Flowers_Katarina.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Skillshots;

    using Flowers_Katarina.MyBase;

    using System;

    #endregion

    internal class MySpellManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Q = new Aimtec.SDK.Spell(SpellSlot.Q, 625f);

                MyLogic.W = new Aimtec.SDK.Spell(SpellSlot.W, 300f);

                MyLogic.E = new Aimtec.SDK.Spell(SpellSlot.E, 725f);

                MyLogic.R = new Aimtec.SDK.Spell(SpellSlot.R, 550f);
                MyLogic.R.SetCharged("KatarinaR", "KatarinaR", 550, 550, 1.0f);

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