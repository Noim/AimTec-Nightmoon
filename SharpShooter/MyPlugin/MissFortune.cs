namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Library;
    using Flowers_Library.Prediction;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System;
    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class MissFortune : MyLogic
    {
        public MissFortune()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 700f) { Delay = 0.25f, Speed = 1400f };

            Q2 = new Aimtec.SDK.Spell(SpellSlot.Q, 1300f);
            Q2.SetSkillshot(0.25f, 70f, 1500f, true, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 1000f);
            E.SetSkillshot(0.5f, 200f, float.MaxValue, false, SkillshotType.Circle);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 1350f);
            R.SetSkillshot(0.25f, 50f, 3000f, false, SkillshotType.Cone);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQ1", "Use Q Extend");
            ComboOption.AddW();
            ComboOption.AddE();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddBool("HarassQ1", "Use Q Extend");
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min Hit Counts >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddE();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", KeyCode.T, KeybindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, false, true, true, true);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PostAttack += PostAttack;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.HasBuff("missfortunebulletsound"))
            {
                Orbwalker.AttackingEnabled = false;
                Orbwalker.MovingEnabled = false;
                return;
            }

            Orbwalker.AttackingEnabled = true;
            Orbwalker.MovingEnabled = true;

            if (MiscOption.GetKey("R", "SemiR").Enabled && R.Ready)
            {
                SemiRLogic();
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

        private static void SemiRLogic()
        {
            var target = TargetSelector.GetTarget(R.Range - 150);

            if (target.IsValidTarget(R.Range))
            {
                var rPred = R.GetPrediction(target);

                if (rPred.HitChance >= HitChance.High)
                {
                    R.Cast(rPred.UnitPosition);
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q2.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        QLogic(target, true);
                    }
                }
            }

            if (KillStealOption.UseE && E.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.E)))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.HitChance >= HitChance.High)
                        {
                            E.Cast(ePred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q2.Range);

            if (target.IsValidTarget(Q2.Range))
            {
                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q2.Range))
                {
                    QLogic(target, ComboOption.GetBool("ComboQ1").Enabled);
                }

                if (ComboOption.UseE && E.Ready && target.IsValidTarget(E.Range))
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.HitChance >= HitChance.High)
                    {
                        E.Cast(ePred.UnitPosition);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(Q2.Range);

                if (target.IsValidTarget(Q2.Range))
                {
                    if (HarassOption.UseQ && Q.Ready && target.IsValidTarget(Q2.Range))
                    {
                        QLogic(target, HarassOption.GetBool("HarassQ1").Enabled);
                    }

                    if (HarassOption.UseE && E.Ready && target.IsValidTarget(E.Range))
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.HitChance >= HitChance.High)
                        {
                            E.Cast(ePred.UnitPosition);
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
            if (LaneClearOption.HasEnouguMana())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToArray();

                if (minions.Any())
                {
                    if (LaneClearOption.UseE && E.Ready)
                    {
                        var eFarm = E.GetSpellFarmPosition(minions);

                        if (eFarm.HitCount >= LaneClearOption.GetSlider("LaneClearECount").Value)
                        {
                            E.Cast(eFarm.CastPosition);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseE && E.Ready)
                {
                    var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMob()).ToArray();

                    if (mobs.Any())
                    {
                        var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                        if (bigmob != null && bigmob.IsValidTarget(E.Range) && (!bigmob.IsValidAutoRange() || !Orbwalker.CanAttack()))
                        {
                            E.Cast(bigmob);
                        }
                        else
                        {
                            var eMobs = mobs.Where(x => x.IsValidTarget(E.Range)).ToArray();
                            var eFarm = E.GetSpellFarmPosition(eMobs);

                            if (eFarm.HitCount >= 2)
                            {
                                E.Cast(eFarm.CastPosition);
                            }
                        }
                    }
                }
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
                                    Q.CastOnUnit(target);
                                }
                                else if (ComboOption.UseW && W.Ready)
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        var mob = Args.Target as Obj_AI_Minion;

                        if (mob != null && mob.IsValidTarget() && mob.IsMob())
                        {
                            if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                            {
                                if (JungleClearOption.HasEnouguMana())
                                {
                                    if (JungleClearOption.UseQ && Q.Ready)
                                    {
                                        Q.CastOnUnit(mob);
                                    }
                                    else if (JungleClearOption.UseW && W.Ready)
                                    {
                                        W.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (E.Ready && target != null && target.IsValidTarget(E.Range))
            {
                switch (Args.Type)
                {
                    case SpellType.Melee:
                        if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
                        {
                            var ePred = E.GetPrediction(target);
                            E.Cast(ePred.UnitPosition);
                        }
                        break;
                    case SpellType.Dash:
                    case SpellType.SkillShot:
                    case SpellType.Targeted:
                        {
                            var ePred = E.GetPrediction(target);
                            E.Cast(ePred.UnitPosition);
                        }
                        break;
                }
            }
        }

        private static void QLogic(Obj_AI_Hero target, bool UseQ1 = false)// SFX Challenger MissFortune QLogic (im so lazy, kappa)
        {
            if (target != null)
            {
                if (target.IsValidTarget(Q.Range))
                {
                    Q.CastOnUnit(target);
                }
                else if (UseQ1 && target.IsValidTarget(Q2.Range) && target.DistanceToPlayer() > Q.Range)
                {
                    var heroPositions = (from t in GameObjects.EnemyHeroes
                                         where t.IsValidTarget(Q2.Range)
                                         let prediction = Q.GetPrediction(t)
                                         select new CPrediction.Position(t, prediction.UnitPosition)).Where(
                        t => t.UnitPosition.Distance(Me.Position) < Q2.Range).ToList();

                    if (heroPositions.Any())
                    {
                        var minions =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q2.Range) && (x.IsMob() || x.IsMinion()))
                                .ToArray();

                        if (minions.Any(m => m.IsMoving) && !heroPositions.Any(h => h.Hero.HasBuff("missfortunepassive")))
                        {
                            return;
                        }

                        var outerMinions = minions.Where(m => m.Distance(Me) > Q.Range).ToList();
                        var innerPositions = minions.Where(m => m.Distance(Me) < Q.Range).ToList();

                        foreach (var minion in innerPositions)
                        {
                            var lMinion = minion;
                            var coneBuff = new MyPolygon.Sector(
                                minion.Position,
                                Me.Position.Extend(minion.Position, Me.Distance(minion) + Q.Range * 0.5f),
                                (float)(40 * Math.PI / 180), Q2.Range - Q.Range);
                            var coneNormal = new MyPolygon.Sector(
                                minion.Position,
                                Me.Position.Extend(minion.Position, Me.Distance(minion) + Q.Range * 0.5f),
                                (float)(60 * Math.PI / 180), Q2.Range - Q.Range);

                            foreach (var enemy in
                                heroPositions.Where(
                                    m => m.UnitPosition.Distance(lMinion.Position) < Q2.Range - Q.Range))
                            {
                                if (coneBuff.IsInside(enemy.Hero) && enemy.Hero.HasBuff("missfortunepassive"))
                                {
                                    Q.CastOnUnit(minion);
                                    return;
                                }
                                if (coneNormal.IsInside(enemy.UnitPosition))
                                {
                                    var insideCone =
                                        outerMinions.Where(m => coneNormal.IsInside(m.Position)).ToList();

                                    if (!insideCone.Any() ||
                                        enemy.UnitPosition.Distance(minion.Position) <
                                        insideCone.Select(
                                                m => m.Position.Distance(minion.Position) - m.BoundingRadius)
                                            .DefaultIfEmpty(float.MaxValue)
                                            .Min())
                                    {
                                        Q.CastOnUnit(minion);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}