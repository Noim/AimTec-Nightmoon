namespace Flowers_Yasuo
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
                if (ObjectManager.GetLocalPlayer().ChampionName != "Yasuo")
                {
                    return;
                }
            
                var YasuoLoader = new MyBase.MyChampions();
            };
        }
    }
}
