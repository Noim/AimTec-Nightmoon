namespace Flowers_Katarina.MyCommon
{
    #region

    using Aimtec;

    using System;
    using System.Collections.Generic;

    #endregion

    internal static class MyDelayAction // Basic From LeagueSharp.Common
    {
        public static List<Action> ActionList = new List<Action>();

        static MyDelayAction()
        {
            Game.OnUpdate += OnUpdate;
        }

        public delegate void Callback();

        public static void Queue(int time, Callback func)
        {
            var action = new Action(time, func);
            ActionList.Add(action);
        }

        private static void OnUpdate()
        {
            for (var i = ActionList.Count - 1; i >= 0; i--)
            {
                if (ActionList[i].Time <= (int)(Game.ClockTime * 1000f))
                {
                    try
                    {
                        ActionList[i].CallbackObject?.Invoke();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    ActionList.RemoveAt(i);
                }
            }
        }

        public struct Action
        {
            public Callback CallbackObject;

            public int Time;

            public Action(int time, Callback callback)
            {
                Time = time + (int)(Game.ClockTime * 1000f);
                CallbackObject = callback;
            }
        }
    }
}
