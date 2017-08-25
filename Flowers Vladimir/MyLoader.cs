namespace Flowers_Vladimir
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
                if (ObjectManager.GetLocalPlayer().ChampionName != "Vladimir")
                {
                    return;
                }

                var VladimirLoader = new MyBase.MyChampions();
            };
        }
    }
}
