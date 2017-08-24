namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Events;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Collision;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Library;
    using Flowers_Library.Prediction;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Urgot : MyLogic
    {
        private static bool isWActive => W.GetBasicSpell().Name.ToLower() == "urgotwcancel";
        private static bool isRActive => R.GetBasicSpell().Name.ToLower() == "urgotrrecast";

        private static bool HaveRBuff(Obj_AI_Hero target)
        {
            return target.Buffs.Any(x => x.IsActive && x.Name.ToLower() == "urgotr");
        }

        private static bool CanBeRKillAble(Obj_AI_Hero target)
        {
            return target != null && target.IsValidTarget() && isRActive && HaveRBuff(target) && target.HealthPercent() < 25 && R.Ready;
        }

        public Urgot()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 800f);
            Q.SetSkillshot(0.41f, 180f, float.MaxValue, false, SkillshotType.Circle); // untposition

            W = new Aimtec.SDK.Spell(SpellSlot.W, 550f);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 550f);
            E.SetSkillshot(0.40f, 65f, 580f, false, SkillshotType.Line); // castposition

            R = new Aimtec.SDK.Spell(SpellSlot.R, 1600f);
            R.SetSkillshot(0.20f, 80f, 2150f, false, SkillshotType.Line);
   

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddE(R);

            //BuffManager.OnAddBuff += BuffManager_OnAddBuff;
            //GameObject.OnCreate += GameObject_OnCreate;
            //GameObject.OnDestroy += GameObject_OnDestroy;
            //Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Render.OnRender += Render_OnRender;
            Game.OnUpdate += Game_OnUpdate;
        }

        private static void Game_OnUpdate()
        {

        }

        private static void BuffManager_OnAddBuff(Obj_AI_Base sender, Buff buff)
        {
            //Console.WriteLine(sender.UnitSkinName + "_" + buff.Name);
        }

        private static void Render_OnRender()
        {
            //Render.Circle(Game.CursorPos, 180, 30, System.Drawing.Color.Red);
        }

        private static void GameObject_OnDestroy(GameObject sender)
        {
            if (sender != null)
            {
              //  Console.WriteLine("OnDelect: " + sender.Name + "_" + Game.TickCount);
            }
        }

        private static void GameObject_OnCreate(GameObject sender)
        {
            if (sender != null && !sender.Name.Contains("missile"))
            {
                Console.WriteLine("OnCreate: " + sender.Name + "_" + Game.TickCount);
            }

        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SpellData.Name.ToLower().Contains("attack"))
                {
                    return;
                }


                //if (args.SpellSlot == SpellSlot.Q)
                //{
                //    Console.WriteLine("MissileSpeed: " + args.SpellData.MissileSpeed);
                //    Console.WriteLine("AISpeed: " + args.SpellData.AISpeed);
                //    Console.WriteLine("LineWidth: " + args.SpellData.LineWidth);
                //    Console.WriteLine("DelayCastOffsetPercent: " + args.SpellData.DelayCastOffsetPercent);
                //    Console.WriteLine("DelayTotalTimePercent: " + args.SpellData.DelayTotalTimePercent);
                //}
            }
        }

        private static Vector3 GetRCastPosition(Obj_AI_Hero target)
        {
            if (target == null || !target.IsValidTarget(R.Range) || target.IsUnKillable())
            {
                return Vector3.Zero;
            }

            var rPredInput = new PredictionInput
            {
                Unit = target,
                Radius = R.Width,
                Speed = R.Speed,
                Range = R.Range,
                Delay = R.Delay,
                AoE = false,
                UseBoundingRadius = true,
                From = Me.ServerPosition,
                RangeCheckFrom = Me.ServerPosition,
                Type = SkillshotType.Line,
                CollisionObjects = CollisionableObjects.Heroes | CollisionableObjects.YasuoWall
            };

            var rPredOutput = Prediction.Instance.GetPrediction(rPredInput);

            if (rPredOutput.HitChance <= HitChance.High ||
                Collision.GetCollision(new List<Vector3> {target.ServerPosition}, rPredInput)
                    .Any(x => x.NetworkId != target.NetworkId))
            {
                return Vector3.Zero;
            }

            return rPredOutput.CastPosition;
        }
    }
}
