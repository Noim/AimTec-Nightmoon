namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Collision;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util;
    using Aimtec.SDK.Util.Cache;

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
        private static int lastQTime, lastWTime, lastETime, lastPressTime;

        private static bool isWActive => W.GetBasicSpell().Name.ToLower() == "urgotwcancel";
        private static bool isRActive => R.GetBasicSpell().Name.ToLower() == "urgotrrecast";

        private static bool HaveEBuff(Obj_AI_Hero target)
        {
            return target.Buffs.Any(x => x.IsActive && x.Name.ToLower().Contains("urgotr") && x.Name.ToLower().Contains("stun"));
        }

        private static bool HaveRBuff(Obj_AI_Hero target)
        {
            return target.Buffs.Any(x => x.IsActive && x.Name.ToLower() == "urgotr");
        }

        private static bool CanbeRKillAble(Obj_AI_Hero target)
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
            Q.SetSkillshot(0.41f, 180f, float.MaxValue, false, SkillshotType.Circle);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 550f);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 550f);
            E.SetSkillshot(0.40f, 65f, 580f, false, SkillshotType.Line);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 1600f);
            R.SetSkillshot(0.20f, 80f, 2150f, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQAfterE", "Use Q| Only After E or E is CoolDown");
            ComboOption.AddW();
            ComboOption.AddBool("ComboWCancel", "Use W| Auto Cancel");
            ComboOption.AddE();
            ComboOption.AddBool("ComboRSolo", "Use R| Solo Mode");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddSliderBool("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5, true);
            LaneClearOption.AddSliderBool("LaneClearWCount", "Use W| Min Hit Count >= x", 4, 1, 10, true);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddR();
            KillStealOption.AddSlider("KillStealRDistance", "Use R| When target Distance Player >= x", 600, 0, 1600);
            KillStealOption.AddTargetList();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            //MiscOption.AddW(); TODO
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key(only work for select target)", KeyCode.T, KeybindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddE(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, true, true, true, true);

            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Game.OnUpdate += OnUpdate;
            Orbwalker.PostAttack += PostAttack;

            //W Auto Shield (Use Zlib)
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (sender.IsMe)
            {
                switch (Args.SpellSlot)
                {
                    case SpellSlot.Q:
                        lastQTime = Game.TickCount;
                        break;
                    case SpellSlot.W:
                        lastWTime = Game.TickCount;
                        break;
                    case SpellSlot.E:
                        lastETime = Game.TickCount;
                        break;
                }
            }
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (isWActive)
            {
                Orbwalker.AttackingEnabled = false;
                Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
            }
            else
            {
                Orbwalker.AttackingEnabled = true;
            }

            if (MiscOption.GetKey("R", "SemiR").Enabled && R.Ready)
            {
                SemiRLogic();
            }

            AutoR2Logic();
            KillSteal();

            if (Orbwalker.Mode == OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Mixed)
            {
                Harass();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
            {
                Clear();
            }
        }

        private static void SemiRLogic()
        {
            var target = TargetSelector.GetSelectedTarget();

            if (target != null && target.IsValidTarget())
            {
                if (!isRActive)
                {
                    if (target.IsValidTarget(R.Range))
                    {
                        var rPos = GetRCastPosition(target);

                        if (rPos != Vector3.Zero)
                        {
                            R.Cast(rPos);
                            lastPressTime = Game.TickCount;
                        }
                    }
                }

                if (isRActive && HaveRBuff(target))
                {
                    R.Cast();
                }
            }
        }

        private static void AutoR2Logic()
        {
            if (Game.TickCount - lastPressTime <= 2000)
            {
                if (!isRActive)
                {
                    return;
                }

                if (GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && HaveRBuff(x) && !x.HaveShiledBuff())
                        .Any(target => target != null && target.IsValidTarget()))
                {
                    R.Cast();
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q) && !HaveRBuff(x)))
                {
                    if (target.IsValidTarget(Q.Range) && !HaveRBuff(target) && !target.IsUnKillable())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                            return;
                        }
                    }
                }
            }

            if (KillStealOption.UseR && R.Ready && Game.TickCount - lastQTime > 3000)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(R.Range + 500) &&
                            !x.IsValidTarget(KillStealOption.GetSlider("KillStealRDistance").Value) &&
                            x.Health < GetRDamage(x, true) && KillStealOption.GetKillStealTarget(x.ChampionName)))
                {
                    if (target.IsValidTarget())
                    {
                        if (CanbeRKillAble(target))
                        {
                            R.Cast();
                            return;
                        }

                        if (!isRActive && target.IsValidTarget(R.Range))
                        {
                            var rPos = GetRCastPosition(target);

                            if (rPos != Vector3.Zero)
                            {
                                R.Cast(rPos);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.GetBool("ComboWCancel").Enabled && W.Ready && isWActive)
            {
                CancelW();
            }

            var target = MyTargetSelector.GetTarget(Q.Range);

            if (target != null && target.IsValidTarget(Q.Range))
            {
                if (ComboOption.UseE && E.Ready)
                {
                    if (target.IsValidTarget(E.Range))
                    {
                        if (!target.IsValidTarget(350) || !Q.Ready)
                        {
                            var ePred = E.GetPrediction(target);

                            if (ePred.HitChance >= HitChance.High)
                            {
                                E.Cast(ePred.CastPosition);
                            }
                        }
                    }
                }

                if (ComboOption.UseQ && Q.Ready && Game.TickCount - lastETime > 800 + Game.Ping)
                {
                    if (target.IsValidTarget(Q.Range))
                    {
                        if (ComboOption.GetBool("ComboQAfterE").Enabled)
                        {
                            if (E.Ready)
                            {
                                return;
                            }

                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                        else
                        {
                            if (target.IsValidTarget(E.Range) && E.Ready)
                            {
                                return;
                            }

                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                    }
                }

                if (ComboOption.UseW && W.Ready && !isWActive)
                {
                    if (GameObjects.EnemyHeroes.Any(x => x.IsValidTarget(W.Range)))
                    {
                        if (target.IsValidTarget(E.Range) && HaveEBuff(target))
                        {
                            W.Cast();
                        }
                        else if (!E.Ready && target.IsValidTarget(350) && Game.TickCount - lastETime < 1500 + Game.Ping)
                        {
                            W.Cast();
                        }
                        else if (!Q.Ready && target.IsValidTarget(450) && Game.TickCount - lastQTime < 1300 + Game.Ping)
                        {
                            W.Cast();
                        }
                        else if (!Q.Ready && !E.Ready && target.IsValidTarget(W.Range - 65) && Game.TickCount - lastQTime > 1300 &&
                                 Game.TickCount - lastETime > 1300)
                        {
                            var minions =
                                GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range - 65) && (x.IsMinion() || x.IsMob()))
                                    .ToArray();

                            if (!minions.Any() || minions.Length <= 3 && target.IsValidTarget(410))
                            {
                                W.Cast();
                            }
                        }
                    }
                }

                if (ComboOption.GetBool("ComboRSolo").Enabled && R.Ready && !target.IsUnKillable() && Me.CountEnemyHeroesInRange(1200) == 1)
                {
                    if (target.Health > GetRDamage(target, true) &&
                        target.Health <
                        GetRDamage(target, true) + GetWDamage(target, 2) + GetPassiveDamage(target) +
                        Me.GetAutoAttackDamage(target) + (Q.Ready ? Me.GetSpellDamage(target, SpellSlot.Q) : 0) +
                        (E.Ready ? Me.GetSpellDamage(target, SpellSlot.E) : 0))
                    {
                        if (isRActive)
                        {
                            if (target.IsValidTarget())
                            {
                                R.Cast();
                            }
                        }
                        else
                        {
                            if (target.IsValidTarget(R.Range))
                            {
                                var rPos = GetRCastPosition(target);

                                if (rPos != Vector3.Zero)
                                {
                                    R.Cast(rPos);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(Q.Range);

                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (HarassOption.UseQ && Q.Ready)
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Clear()
        {
            if (MyManaManager.SpellHarass)
            {
                Harass();
            }

            if (MyManaManager.SpellFarm)
            {
                LaneClear();
                JungleClear();
            }
        }

        private static void LaneClear()
        {
            if (LaneClearOption.HasEnouguMana())
            {
                if (LaneClearOption.GetSliderBool("LaneClearQCount").Enabled && Q.Ready)
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        var qFarm = Q.GetSpellFarmPosition(minions);

                        if (qFarm.HitCount >= LaneClearOption.GetSliderBool("LaneClearQCount").Value)
                        {
                            Q.Cast(qFarm.CastPosition);
                        }
                    }
                }

                if (LaneClearOption.GetSliderBool("LaneClearWCount").Enabled && W.Ready && !isWActive)
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        if (minions.Length >= LaneClearOption.GetSliderBool("LaneClearWCount").Value)
                        {
                            W.Cast();
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range) && x.IsMob()).ToArray();

                if (mobs.Any())
                {
                    var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                    if (JungleClearOption.UseQ && Q.Ready)
                    {
                        if (bigmob != null && bigmob.IsValidTarget(Q.Range) && (!bigmob.IsValidAutoRange() || !Orbwalker.CanAttack()))
                        {
                            Q.Cast(bigmob.ServerPosition);
                        }
                        else
                        {
                            var qMobs = mobs.Where(x => x.IsValidTarget(Q.Range)).ToArray();
                            var qFarm = Q.GetSpellFarmPosition(qMobs);

                            if (qFarm.HitCount >= 2)
                            {
                                Q.Cast(qFarm.CastPosition);
                            }
                        }
                    }

                    if (JungleClearOption.UseE && E.Ready)
                    {
                        if (bigmob != null && bigmob.IsValidTarget(E.Range) && (!bigmob.IsValidAutoRange() || !Orbwalker.CanAttack()))
                        {
                            E.Cast(bigmob.ServerPosition);
                        }
                    }

                    if (JungleClearOption.UseW && W.Ready && !isWActive)
                    {
                        if (bigmob != null && bigmob.IsValidTarget(W.Range) && (!bigmob.IsValidAutoRange() || !Orbwalker.CanAttack()))
                        {
                            W.Cast();
                        }
                        else
                        {
                            var wMobs = mobs.Where(x => x.IsValidTarget(W.Range)).ToArray();

                            if (wMobs.Length >= 2)
                            {
                                W.Cast();
                            }
                        }
                    }
                }
            }
        }

        private static void PostAttack(object sender, PostAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || Args.Target.Health <= 0 || Me.IsDead || !Q.Ready)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.obj_AI_Hero:
                    {
                        var target = Args.Target as Obj_AI_Hero;

                        if (target != null && target.IsValidTarget())
                        {
                            if (Orbwalker.Mode == OrbwalkingMode.Combo)
                            {
                                if (ComboOption.UseW && W.Ready && !isWActive)
                                {
                                    if (GameObjects.EnemyHeroes.Any(x => x.IsValidTarget(W.Range)))
                                    {
                                        if (target.IsValidTarget(E.Range) && HaveEBuff(target))
                                        {
                                            W.Cast();
                                        }
                                        else if (!E.Ready && target.IsValidTarget(350) && Game.TickCount - lastETime < 1500 + Game.Ping)
                                        {
                                            W.Cast();
                                        }
                                    }
                                }
                                else if (ComboOption.UseQ && Q.Ready)
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.UnitPosition);
                                    }
                                }
                            }
                            else if (Orbwalker.Mode == OrbwalkingMode.Mixed ||
                                     Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana() && HarassOption.GetHarassTargetEnabled(target.ChampionName))
                                {
                                    if (HarassOption.UseQ && Q.Ready)
                                    {
                                        var qPred = Q.GetPrediction(target);

                                        if (qPred.HitChance >= HitChance.High)
                                        {
                                            Q.Cast(qPred.UnitPosition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        var mob = Args.Target as Obj_AI_Minion;

                        if (mob != null && mob.IsMob() && mob.isBigMob())
                        {
                            if (Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellFarm && JungleClearOption.HasEnouguMana())
                            {
                                if (JungleClearOption.UseQ && Q.Ready && mob.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(mob.ServerPosition);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void CancelW(bool ignoreCheck = false)
        {
            if (isWActive)
            {
                if (ignoreCheck || GameObjects.EnemyHeroes.All(x => !x.IsValidTarget(W.Range)))
                {
                    W.Cast();
                }
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

            if (rPredOutput.HitChance < HitChance.High/* ||
                Collision.GetCollision(new List<Vector3> { target.ServerPosition }, rPredInput)
                    .Any(x => x.NetworkId != target.NetworkId)*/)
            {
                return Vector3.Zero;
            }

            return rPredOutput.CastPosition;
        }

        internal static double GetWDamage(Obj_AI_Base target, int time = 1)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0d;
            }

            var wDMG = 12 + new[] { 0.20, 0.24, 0.28, 0.32, 0.36 }[W.GetBasicSpell().Level - 1] * Me.FlatPhysicalDamageMod * 3 * time;

            return Me.CalculateDamage(target, DamageType.Physical, wDMG);
        }

        internal static double GetRDamage(Obj_AI_Hero target, bool calculate25 = false)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0d;
            }

            var rDMG = new double[] { 50, 175, 300 }[R.GetBasicSpell().Level - 1] + 0.5 * Me.FlatPhysicalDamageMod;

            return Me.CalculateDamage(target, DamageType.Physical, rDMG) + (calculate25 ? target.MaxHealth * 0.249 : 0);
        }

        private static double GetBasicDamage
        {
            get
            {
                if (Me.Level >= 16)
                {
                    return 1.00 * Me.FlatPhysicalDamageMod;
                }

                if (Me.Level >= 13)
                {
                    return 0.90 * Me.FlatPhysicalDamageMod;
                }

                if (Me.Level >= 11)
                {
                    return 0.80 * Me.FlatPhysicalDamageMod;
                }

                if (Me.Level >= 9)
                {
                    return 0.65 * Me.FlatPhysicalDamageMod;
                }

                if (Me.Level >= 6)
                {
                    return 0.50 * Me.FlatPhysicalDamageMod;
                }

                return 0.40 * Me.FlatPhysicalDamageMod;
            }
        }

        private static double GetBasicValueForHP
        {
            get
            {
                if (Me.Level >= 15)
                {
                    return 0.08;
                }

                if (Me.Level >= 13)
                {
                    return 0.07;
                }

                if (Me.Level >= 6)
                {
                    return 0.525;
                }

                return 0.045;
            }
        }

        private static double GetPassiveDamage(Obj_AI_Hero target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return 0d;
            }

            var basicDamage = GetBasicDamage + GetBasicValueForHP * target.MaxHealth;

            return Me.CalculateDamage(target, DamageType.Physical, basicDamage);
        }
    }
}
