namespace Flowers_Riven.MyCommon
{
    #region

    using Aimtec.SDK.Orbwalking;

    using System;

    #endregion

    internal static class MyOrbwalkerManager
    {
        public delegate void OnResetAutoAttackEventHandler();

        public static event OnResetAutoAttackEventHandler OnAutoAttackReset;

        internal static void Reset()
        {
            try
            {
                Orbwalker.Implementation.ResetAutoAttackTimer();
                OnAutoAttackReset?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                //Console.WriteLine("Reset Auto Attack Event Handler");
            }
        }
    }
}
