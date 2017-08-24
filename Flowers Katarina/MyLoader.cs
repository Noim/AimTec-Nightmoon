namespace Flowers_Katarina
{
    #region 

    using Aimtec;
    using Aimtec.SDK.Events;

    #endregion

    internal class MyLoader
    {
        public static void Main()
        {
            GameEvents.GameStart += () =>
            {
                if (ObjectManager.GetLocalPlayer().ChampionName != "Katarina")
                {
                    return;
                }

                var KatarinaLoader = new MyBase.MyChampions();
            };
        }
    }
}
