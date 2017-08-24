namespace Flowers_Fiora.MyBase
{
    #region 

    using Flowers_Fiora.MyCommon;

    using System;

    #endregion

    internal class MyChampions
    {
        public MyChampions()
        {
            try
            {
                Initializer();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyChampions." + ex);
            }
        }

        internal void Initializer()
        {
            try
            {
                MySpellManager.Initializer();
                MyMenuManager.Initializer();
                MyPassiveManager.Initializer();
                MyEventManager.Initializer();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyChampions.Initializer" + ex);
            }
        }
    }
}
