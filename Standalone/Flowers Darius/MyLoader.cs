namespace Flowers_Darius
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
                if (ObjectManager.GetLocalPlayer().ChampionName != "Darius")
                {
                    return;
                }

                var DariusLoader = new MyBase.MyChampions();
            };
        }
    }
}
