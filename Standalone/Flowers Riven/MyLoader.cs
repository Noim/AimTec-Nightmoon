namespace Flowers_Riven
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
                if (ObjectManager.GetLocalPlayer().ChampionName != "Riven")
                {
                    return;
                }
            
                var RivenLoader = new MyBase.MyChampions();
            };
        }
    }
}
