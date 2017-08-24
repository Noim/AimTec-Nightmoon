namespace Flowers_Riven.MyCommon
{
    using Aimtec;
    using System;
    using System.Collections.Generic;
    public static class DelayAction
    {
        public static List<Action> ActionList = new List<Action>();

        static DelayAction()
        {
            Game.OnUpdate += GameOnOnGameUpdate;
        }

        public delegate void Callback();

        public static void Queue(int time, Callback func)
        {
            var action = new Action(time, func);
            ActionList.Add(action);
        }

        private static void GameOnOnGameUpdate()
        {
            for (var i = ActionList.Count - 1; i >= 0; i--)
            {
                if (ActionList[i].Time <= MyExtraManager.GameTimeTickCount)
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
                Time = time + MyExtraManager.GameTimeTickCount;
                CallbackObject = callback;
            }
        }
    }
}
