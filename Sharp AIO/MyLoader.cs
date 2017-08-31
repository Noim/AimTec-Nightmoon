namespace Sharp_AIO
{
    #region 

    using Aimtec.SDK.Events;

    #endregion

    public class MyLoader
    {
        private static void Main()
        {
            GameEvents.GameStart += () =>
            {
                var MainLoader = new MyBase.MyChampions();
            };
        }
    }
}
