namespace Flowers_Draven
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
                if (ObjectManager.GetLocalPlayer().ChampionName != "Draven")
                {
                    return;
                }

                var DravenLoader = new MyBase.MyChampions();
            };
        }
    }
}
