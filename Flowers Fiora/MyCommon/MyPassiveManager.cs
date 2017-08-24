namespace Flowers_Fiora.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;

    using Flowers_Fiora.MyBase;

    using System.Collections.Generic;
    using System.Linq;

    #endregion

    internal static class MyPassiveManager
    {
        public enum PassiveDirection
        {
            NE = 0,
            NW = 1,
            SE = 2,
            SW = 3,
            None = 4
        }

        public enum PassiveType
        {
            Prepassive = 0,
            Passive = 1,
            TimeOut = 2,
            Ult = 3,
            None = 4
        }

        public class PassiveList
        {
            public PassiveType PassiveType { get; set; }
            public GameObject Passive { get; set; }
            public PassiveDirection Direction { get; set; }
            public int NetWorkId { get; set; }

            public PassiveList(GameObject pass, PassiveType type, PassiveDirection dircetion)
            {
                Passive = pass;
                PassiveType = type;
                Direction = dircetion;
            }
        }

        internal static List<PassiveList> AllPassive = new List<PassiveList>();

        public static bool IsPassive(this GameObject emitter)
        {
            if (emitter == null || emitter.Type != GameObjectType.obj_GeneralParticleEmitter || !emitter.IsValid || !emitter.IsVisible)
            {
                return false;
            }

            switch (emitter.Name)
            {     
                // 人物右边
                case "Fiora_Base_Passive_NW_Warning.troy":
                case "Fiora_Base_Passive_NW.troy":
                case "Fiora_Base_Passive_NW_Timeout.troy":
                case "Fiora_Base_R_Mark_NW_FioraOnly.troy":
                case "Fiora_Base_R_NW_Timeout_FioraOnly.troy":

                // 人物左边
                case "Fiora_Base_Passive_SE_Warning.troy":
                case "Fiora_Base_Passive_SE.troy":
                case "Fiora_Base_Passive_SE_Timeout.troy":
                case "Fiora_Base_R_Mark_SE_FioraOnly.troy":
                case "Fiora_Base_R_SE_Timeout_FioraOnly.troy":

                // 人物北边
                case "Fiora_Base_Passive_NE_Warning.troy":
                case "Fiora_Base_Passive_NE.troy":
                case "Fiora_Base_Passive_NE_Timeout.troy":
                case "Fiora_Base_R_Mark_NE_FioraOnly.troy":
                case "Fiora_Base_R_NE_Timeout_FioraOnly.troy":

                // 人物南边
                case "Fiora_Base_Passive_SW_Warning.troy":
                case "Fiora_Base_Passive_SW.troy":
                case "Fiora_Base_Passive_SW_Timeout.troy":
                case "Fiora_Base_R_Mark_SW_FioraOnly.troy":
                case "Fiora_Base_R_SW_Timeout_FioraOnly.troy":

                    return true;
                default:
                    return false;
            }
        }

        public static int PassiveCount(Obj_AI_Hero target)
        {
            var allPassive =
                AllPassive.Where(
                    x => x.Passive != null && x.Passive.IsValid && x.Passive.ServerPosition.Distance(target.ServerPosition) <= 50);

            return allPassive.Count();
        }

        public static List<Vector3> GetPassivePosList(Obj_AI_Hero target)
        {
            var positionList = new List<Vector3>();

            var allPassive =
                AllPassive.Where(
                    x => x.Passive != null && x.Passive.IsValid && x.Passive.ServerPosition.Distance(target.ServerPosition) < 150).ToArray();

            var position = target.ServerPosition;

            foreach (var x in allPassive)
            {
                if (x.PassiveType == PassiveType.Ult)
                {
                    switch (x.Direction)
                    {
                        case PassiveDirection.NE:
                            {
                                var pos = new Vector3
                                {
                                    X = position.X,
                                    Y = position.Y + 250,
                                    Z = target.ServerPosition.Z
                                };

                                positionList.Add(pos);
                            }
                            break;
                        case PassiveDirection.NW:
                            {
                                var pos = new Vector3
                                {
                                    X = position.X + 200,
                                    Y = position.Y,
                                    Z = target.ServerPosition.Z
                                };

                                positionList.Add(pos);
                            }
                            break;
                        case PassiveDirection.SE:
                            {
                                var pos = new Vector3
                                {
                                    X = position.X - 200,
                                    Y = position.Y,
                                    Z = target.ServerPosition.Z
                                };

                                positionList.Add(pos);
                            }
                            break;
                        case PassiveDirection.SW:
                            {
                                var pos = new Vector3
                                {
                                    X = position.X,
                                    Y = position.Y - 250,
                                    Z = target.ServerPosition.Z
                                };

                                positionList.Add(pos);
                            }
                            break;
                    }
                }
                else
                {
                    switch (x.Direction)
                    {
                        case PassiveDirection.NE:
                            {
                                var pos = new Vector3
                                {
                                    X = position.X,
                                    Y = position.Y + 150,
                                    Z = target.ServerPosition.Z
                                };

                                positionList.Add(pos);
                            }
                            break;
                        case PassiveDirection.NW:
                            {
                                var pos = new Vector3
                                {
                                    X = position.X + 100,
                                    Y = position.Y,
                                    Z = target.ServerPosition.Z
                                };

                                positionList.Add(pos);
                            }
                            break;
                        case PassiveDirection.SE:
                            {
                                var pos = new Vector3
                                {
                                    X = position.X - 100,
                                    Y = position.Y,
                                    Z = target.ServerPosition.Z
                                };

                                positionList.Add(pos);
                            }
                            break;
                        case PassiveDirection.SW:
                            {
                                var pos = new Vector3
                                {
                                    X = position.X,
                                    Y = position.Y - 150,
                                    Z = target.ServerPosition.Z
                                };

                                positionList.Add(pos);
                            }
                            break;
                    }
                }
            }

            return positionList;
        }

        public static Vector3 CastQPosition(Obj_AI_Hero target)
        {
            var list = GetPassivePosList(target).ToArray();

            return
                list.Where(
                        x =>
                            x.Distance(ObjectManager.GetLocalPlayer().ServerPosition) > 80 &&
                            x.Distance(target.ServerPosition) > 50)
                    .OrderBy(x => x.Distance(target.ServerPosition))
                    .ThenByDescending(x => x.Distance(ObjectManager.GetLocalPlayer().ServerPosition))
                    .FirstOrDefault();
        }

        public static Vector3 OrbwalkerPosition(Obj_AI_Hero target)
        {
            var pos =
                GetPassivePosList(target).Where(
                        x =>
                            x.Distance(ObjectManager.GetLocalPlayer().ServerPosition) > 30 &&
                            x.Distance(target.ServerPosition) > 30)
                    .OrderBy(x => x.Distance(target.ServerPosition))
                    .ThenByDescending(x => x.Distance(ObjectManager.GetLocalPlayer().ServerPosition))
                    .FirstOrDefault();

            if ((!MyLogic.Q.Ready || MyLogic.Q.Ready && target.ServerPosition.Distance(ObjectManager.GetLocalPlayer().ServerPosition) <= 200) &&
                ObjectManager.GetLocalPlayer().ServerPosition.Distance(pos) <= ObjectManager.GetLocalPlayer().BoundingRadius + target.BoundingRadius + 150)
            {
                return target.ServerPosition.Extend(pos, 200);
            }

            return Vector3.Zero;
        }

        private static PassiveDirection GetDirection(GameObject obj)
        {
            switch (obj.Name)
            {
                // 人物右边
                case "Fiora_Base_Passive_NW_Warning.troy":
                case "Fiora_Base_Passive_NW.troy":
                case "Fiora_Base_Passive_NW_Timeout.troy":
                case "Fiora_Base_R_Mark_NW_FioraOnly.troy":
                case "Fiora_Base_R_NW_Timeout_FioraOnly.troy":
                    return PassiveDirection.NW;

                // 人物左边
                case "Fiora_Base_Passive_SE_Warning.troy":
                case "Fiora_Base_Passive_SE.troy":
                case "Fiora_Base_Passive_SE_Timeout.troy":
                case "Fiora_Base_R_Mark_SE_FioraOnly.troy":
                case "Fiora_Base_R_SE_Timeout_FioraOnly.troy":
                    return PassiveDirection.SE;

                // 人物北边
                case "Fiora_Base_Passive_NE_Warning.troy":
                case "Fiora_Base_Passive_NE.troy":
                case "Fiora_Base_Passive_NE_Timeout.troy":
                case "Fiora_Base_R_Mark_NE_FioraOnly.troy":
                case "Fiora_Base_R_NE_Timeout_FioraOnly.troy":
                    return PassiveDirection.NE;

                // 人物南边
                case "Fiora_Base_Passive_SW_Warning.troy":
                case "Fiora_Base_Passive_SW.troy":
                case "Fiora_Base_Passive_SW_Timeout.troy":
                case "Fiora_Base_R_Mark_SW_FioraOnly.troy":
                case "Fiora_Base_R_SW_Timeout_FioraOnly.troy":
                    return PassiveDirection.SW;

                default:
                    return PassiveDirection.None;
            }
        }

        private static PassiveType GetType(GameObject obj)
        {
            switch (obj.Name)
            {
                // Ult
                case "Fiora_Base_R_Mark_NW_FioraOnly.troy":  
                case "Fiora_Base_R_Mark_SE_FioraOnly.troy":
                case "Fiora_Base_R_Mark_NE_FioraOnly.troy":
                case "Fiora_Base_R_Mark_SW_FioraOnly.troy":
                    return PassiveType.Ult;

                // Warning
                case "Fiora_Base_Passive_NW_Warning.troy":
                case "Fiora_Base_Passive_SE_Warning.troy":
                case "Fiora_Base_Passive_NE_Warning.troy":
                case "Fiora_Base_Passive_SW_Warning.troy":
                    return PassiveType.Prepassive;

                // Already
                case "Fiora_Base_Passive_NW.troy":
                case "Fiora_Base_Passive_SE.troy":
                case "Fiora_Base_Passive_NE.troy":
                case "Fiora_Base_Passive_SW.troy":
                    return PassiveType.Passive;

                // TimeOut
                case "Fiora_Base_R_NW_Timeout_FioraOnly.troy":
                case "Fiora_Base_R_SE_Timeout_FioraOnly.troy":
                case "Fiora_Base_R_NE_Timeout_FioraOnly.troy":
                case "Fiora_Base_R_SW_Timeout_FioraOnly.troy":
                case "Fiora_Base_Passive_NW_Timeout.troy":
                case "Fiora_Base_Passive_SE_Timeout.troy":
                case "Fiora_Base_Passive_NE_Timeout.troy":
                case "Fiora_Base_Passive_SW_Timeout.troy":
                    return PassiveType.TimeOut;

                default:
                    return PassiveType.None;
            }
        }

        internal static void Initializer()
        {
            Game.OnUpdate += OnUpdate;
        }

        private static void OnUpdate()
        {
            AllPassive.Clear();

            var emitterList = ObjectManager.Get<GameObject>().Where(x => x != null && x.IsValid && x.IsPassive()).ToArray();

            foreach (var passive in emitterList)
            {
                switch (passive.Name)
                {
                    // 人物右边
                    case "Fiora_Base_Passive_NW_Warning.troy":
                    case "Fiora_Base_Passive_NW.troy":
                    case "Fiora_Base_Passive_NW_Timeout.troy":
                    case "Fiora_Base_R_Mark_NW_FioraOnly.troy":
                    case "Fiora_Base_R_NW_Timeout_FioraOnly.troy":

                    // 人物左边
                    case "Fiora_Base_Passive_SE_Warning.troy":
                    case "Fiora_Base_Passive_SE.troy":
                    case "Fiora_Base_Passive_SE_Timeout.troy":
                    case "Fiora_Base_R_Mark_SE_FioraOnly.troy":
                    case "Fiora_Base_R_SE_Timeout_FioraOnly.troy":

                    // 人物北边
                    case "Fiora_Base_Passive_NE_Warning.troy":
                    case "Fiora_Base_Passive_NE.troy":
                    case "Fiora_Base_Passive_NE_Timeout.troy":
                    case "Fiora_Base_R_Mark_NE_FioraOnly.troy":
                    case "Fiora_Base_R_NE_Timeout_FioraOnly.troy":

                    // 人物南边
                    case "Fiora_Base_Passive_SW_Warning.troy":
                    case "Fiora_Base_Passive_SW.troy":
                    case "Fiora_Base_Passive_SW_Timeout.troy":
                    case "Fiora_Base_R_Mark_SW_FioraOnly.troy":
                    case "Fiora_Base_R_SW_Timeout_FioraOnly.troy":
                        {
                            AllPassive.Add(new PassiveList(passive, GetType(passive), GetDirection(passive)));
                        }
                        break;
                }
            }
        }
    }
}
