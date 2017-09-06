namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Events;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Library.Prediction;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Xayah : MyLogic
    {
        private static List<MyFeather> FeatherList = new List<MyFeather>();

        private static int GetPassiveCount => Me.HasBuff("XayahPassiveActive") ? Me.GetBuffCount("XayahPassiveActive") : 0;

        private static bool isWActive => Me.HasBuff("XayahW");

        public Xayah()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1100f);
            Q.SetSkillshot(0.25f, 60f, 4000f, false, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W);

            E = new Aimtec.SDK.Spell(SpellSlot.E);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 1100f);
            R.SetSkillshot(1.0f, 60f, float.MaxValue, false, SkillshotType.Cone);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboERoot", "Use E| If Target Can imprison", false);
            ComboOption.AddBool("ComboELogic", "Use E| Logic Cast(1AA + 1Q + 1E DMG)");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddSlider("HarassECount", "Use E|Min Passive Hit Count >= x", 3, 1, 10);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddW();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddE();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddSubMenu("Block", "Block Spell Settings");
            MyEvadeManager.Attach(MiscMenu["SharpShooter.MiscSettings.Block"].As<Menu>());

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(false, false, true, false, false);

            CPrediction.BoundingRadiusMultiplicator = 1.15f;

            Game.OnUpdate += OnUpdate;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDestroy += OnDestroy;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalker.PreAttack += PreAttack;
            Orbwalker.PostAttack += PostAttack;
        }

        private static void OnUpdate()
        {
            if (FeatherList.Any())
            {
                FeatherList.RemoveAll(f => f.EndTime - Game.TickCount <= 0);
            }

            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.HasBuff("XayahR"))
            {
                Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                return;
            }

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
                FarmHarass();
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.Ready)
            {
                if (
                    GameObjects.EnemyHeroes.Where(x => x.IsValidTarget() && FeatherList.Count > 0)
                        .Any(
                            target =>
                                target != null && !target.IsUnKillable() && !target.IsDashing() &&
                                target.Health <= GetEDamage(target)))
                {
                    E.Cast();
                    return;
                }
            }

            if (KillStealOption.UseQ && Q.Ready)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
                {
                    if (!target.IsValidTarget(Q.Range) || !(target.Health < Me.GetSpellDamage(target, SpellSlot.Q)))
                    {
                        continue;
                    }

                    if (!target.IsUnKillable())
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

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(1500);

            if (target != null && target.IsValidTarget(1500))
            {
                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                {
                    if (target.DistanceToPlayer() > Me.GetFullAttackRange(target) + 150 || !Orbwalker.CanAttack())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }

                if (!E.Ready || target.IsDashing())
                {
                    return;
                }

                if (ComboOption.UseE && target.Health < GetEDamage(target))
                {
                    E.Cast();
                }

                if (ComboOption.GetBool("ComboERoot").Enabled && HitECount(target) >= 3 &&
                    !target.HasBuffOfType(BuffType.SpellShield))
                {
                    E.Cast();
                }

                if (ComboOption.GetBool("ComboELogic").Enabled && Me.Level >= 5 &&
                    target.Health + target.HPRegenRate * 2 <
                    GetEDamage(target) + Me.GetSpellDamage(target, SpellSlot.Q) + Me.GetAutoAttackDamage(target))
                {
                    E.Cast();
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(1500);

                if (target.IsValidTarget(1500))
                {
                    if (HarassOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }

                    if (HarassOption.UseE && E.Ready && target.IsValidTarget() && !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        if (HitECount(target) >= HarassOption.GetSlider("HarassECount").Value)
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void FarmHarass()
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
            if (LaneClearOption.HasEnouguMana() && LaneClearOption.UseQ && Q.Ready)
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                if (minions.Any())
                {
                    var qFarm = Q.GetSpellFarmPosition(minions);

                    if (qFarm.HitCount >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                    {
                        Q.Cast(qFarm.CastPosition);
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs =
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                        .OrderBy(x => x.MaxHealth)
                        .ToArray();

                if (mobs.Any())
                {
                    var bigMob = mobs.First(x => !x.Name.Contains("mini") && !x.Name.Contains("Crap"));

                    if (bigMob != null && bigMob.IsValidTarget())
                    {
                        if (JungleClearOption.UseQ && Q.Ready && bigMob.IsValidTarget(Q.Range) &&
                            (bigMob.DistanceToPlayer() > Me.GetFullAttackRange(bigMob) ||
                             !Orbwalker.CanAttack()))
                        {
                            Q.Cast(bigMob);
                        }

                        if (JungleClearOption.UseE && E.Ready && bigMob.IsValidTarget())
                        {
                            if (GetEDamageForMinion(bigMob) > bigMob.Health)
                            {
                                E.Cast();
                            }
                        }
                    }
                    else
                    {
                        if (JungleClearOption.UseQ && Q.Ready)
                        {
                            var qFarm = Q.GetSpellFarmPosition(mobs);

                            if (qFarm.HitCount >= 2)
                            {
                                Q.Cast(qFarm.CastPosition);
                            }
                        }
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender)
        {
            if (sender == null || sender.Type != GameObjectType.obj_GeneralParticleEmitter)
            {
                return;
            }

            if (sender.Name.ToLower() == "xayah_base_passive_dagger_indicator8s.troy")
            {
                FeatherList.Add(new MyFeather(sender.NetworkId, sender.ServerPosition,
                    Game.TickCount + 8000 - Game.Ping));
            }
        }

        private static void OnDestroy(GameObject sender)
        {
            if (sender == null || sender.Type != GameObjectType.obj_GeneralParticleEmitter)
            {
                return;
            }

            FeatherList.RemoveAll(f => f.NetWorkId == sender.NetworkId);
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.SpellSlot == SpellSlot.E)
                {
                    FeatherList.Clear();
                }

                if (Orbwalker.Mode == OrbwalkingMode.Combo)
                {
                    if (Args.SpellSlot == SpellSlot.Q && Me.CountEnemyHeroesInRange(600) > 0 && ComboOption.UseW && W.Ready)
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void PreAttack(object sender, PreAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
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
                                if (ComboOption.UseW && W.Ready)
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                        {
                            if (JungleClearOption.HasEnouguMana() && Args.Target.IsMob() && GetPassiveCount < 3)
                            {
                                if (JungleClearOption.UseW && W.Ready)
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Turret:
                case GameObjectType.obj_HQ:
                case GameObjectType.obj_Barracks:
                case GameObjectType.obj_BarracksDampener:
                case GameObjectType.obj_Building:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellFarm && LaneClearOption.HasEnouguMana(true))
                        {
                            if (Me.CountEnemyHeroesInRange(800) == 0)
                            {
                                if (LaneClearOption.UseW && W.Ready)
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void PostAttack(object sender, PostAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
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
                                if (ComboOption.UseQ && Q.Ready)
                                {
                                    if (!isWActive)
                                    {
                                        var qPred = Q.GetPrediction(target);

                                        if (qPred.HitChance >= HitChance.High)
                                        {
                                            Q.Cast(qPred.CastPosition);
                                        }
                                    }
                                }

                                if (ComboOption.UseW && W.Ready)
                                {
                                    W.Cast();
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
                                            Q.Cast(qPred.CastPosition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        internal static double GetEDamageForMinion(Obj_AI_Base target)
        {
            if (HitECount(target) == 0)
            {
                return 0;
            }
            return GetEDMG(target, HitECount(target)) * 0.5;
        }

        internal static double GetEDMG(Obj_AI_Base target, int eCount)
        {
            if (eCount == 0)
            {
                return 0;
            }

            double damage = 0;
            double multiplier = 1;
            var basicDMG = new double[] { 50, 60, 70, 80, 90 }[E.GetBasicSpell().Level - 1] +
                           0.6 * Me.FlatPhysicalDamageMod;
            var realBasicDMG = basicDMG + basicDMG * 0.5 * Me.Crit;

            for (var cycle = 0; cycle <= eCount; cycle++)
            {
                multiplier -= 0.1 * cycle;
                damage += Me.CalculateDamage(target, DamageType.Physical, realBasicDMG) * Math.Max(0.1, multiplier);
            }

            return (float)damage;
        }

        private static double GetEDamage(Obj_AI_Base target)
        {
            if (HitECount(target) == 0)
            {
                return 0;
            }
            var eDMG = GetEDMG(target, HitECount(target));
            return MyExtraManager.GetRealDamage(eDMG, target);
        }

        internal static int HitECount(Obj_AI_Base target)
        {
            return
                FeatherList.Select(
                    f =>
                        CPrediction.GetLineAoeCanHit(Me.ServerPosition.Distance(f.ServerPosition), 55, target, 
                            HitChance.High, f.ServerPosition)).Count(pred => pred);
        }

        public class MyFeather
        {
            public int NetWorkId { get; set; }
            public Vector3 ServerPosition { get; set; }
            public int EndTime { get; set; }

            public MyFeather(int NetWorkId, Vector3 ServerPosition, int EndTime)
            {
                this.NetWorkId = NetWorkId;
                this.ServerPosition = ServerPosition;
                this.EndTime = EndTime;
            }
        }
    }
}