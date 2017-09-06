namespace Flowers_Ryze
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
                if (ObjectManager.GetLocalPlayer().ChampionName != "Ryze")
                {
                    return;
                }

                var RyzeLoader = new MyBase.MyChampions();
            };
        }
    }
}
