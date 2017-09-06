namespace Flowers_Fiora
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
                if (ObjectManager.GetLocalPlayer().ChampionName != "Fiora")
                {
                    return;
                }
            
                var FioraLoader = new MyBase.MyChampions();
            };
        }
    }
}
