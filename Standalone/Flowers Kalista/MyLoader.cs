namespace Flowers_Kalista
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
                if (ObjectManager.GetLocalPlayer().ChampionName != "Kalista")
                {
                    return;
                }

                var KalistaLoader = new MyBase.MyChampions();
            };
        }
    }
}
