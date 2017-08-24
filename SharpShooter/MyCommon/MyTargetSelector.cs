namespace SharpShooter.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.TargetSelector;

    using System.Collections.Generic;
    using System.Linq;

    #endregion

    internal class MyTargetSelector
    {
        public static Obj_AI_Hero GetTarget(float range, bool ForcusOrbwalkerTarget = true, bool checkKillAble = true, bool checkShield = false)
        {
            var selectTarget = TargetSelector.Implementation.GetSelectedTarget();

            if (selectTarget != null && selectTarget.IsValidTarget(range))
            {
                if (!checkKillAble || !selectTarget.IsUnKillable())
                {
                    if (!checkShield || !selectTarget.HaveShiledBuff())
                    {
                        return selectTarget;
                    }
                }
            }

            var orbTarget = Orbwalker.Implementation.GetOrbwalkingTarget() as Obj_AI_Hero;

            if (ForcusOrbwalkerTarget && orbTarget != null && orbTarget.IsValidTarget(range) && orbTarget.IsValidAutoRange())
            {
                if (!checkKillAble || !orbTarget.IsUnKillable())
                {
                    if (!checkShield || !orbTarget.HaveShiledBuff())
                    {
                        return orbTarget;
                    }
                }
            }

            var finallyTarget = TargetSelector.Implementation.GetOrderedTargets(range).FirstOrDefault();

            if (finallyTarget != null && finallyTarget.IsValidTarget(range))
            {
                if (!checkKillAble || !finallyTarget.IsUnKillable())
                {
                    if (!checkShield || !finallyTarget.HaveShiledBuff())
                    {
                        return finallyTarget;
                    }
                }
            }

            return null;
        }

        public static List<Obj_AI_Hero> GetTargets(float range, bool checkKillAble = true, bool checkShield = false)
        {
            return
                TargetSelector.Implementation.GetOrderedTargets(range)
                    .Where(x => !checkKillAble || !x.IsUnKillable())
                    .Where(x => !checkShield || !x.HaveShiledBuff())
                    .ToList();
        }
    }
}
