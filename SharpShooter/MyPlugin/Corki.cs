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

    using Flowers_Library.Prediction;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Corki : MyLogic
    {
        private static float rRange => Me.HasBuff("CorkiMissileBarrageCounterBig") ? 1500f : 1300f;

        public Corki()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 825f);
            Q.SetSkillshot(0.30f, 200f, 1000f, false, SkillshotType.Circle);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 800f);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 600f);

            R = new Aimtec.SDK.Spell(SpellSlot.R, rRange);
            R.SetSkillshot(0.20f, 50f, 2000f, true, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRLimit", "Use R|Limit Stack >= x", 0, 0, 7);
            ComboOption.AddSlider("ComboRHP", "Use R|Target HealthPercent <= x%", 100, 1, 101);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddR();
            HarassOption.AddSlider("HarassRLimit", "Use R|Limit Stack >= x", 4, 0, 7);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddR();
            LaneClearOption.AddSlider("LaneClearRCount", "Use R|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddSlider("LaneClearRLimit", "Use R|Limit Stack >= x", 4, 0, 7);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddR();
            JungleClearOption.AddSlider("JungleClearRLimit", "Use R|Limit Stack >= x", 0, 0, 7);
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddR();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", KeyCode.T, KeybindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, false, true, true, true);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PostAttack += PostAttack;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (R.GetBasicSpell().Level > 0)
            {
                R.Range = rRange;
            }

            if (MiscOption.GetKey("R", "SemiR").Enabled)
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
            if (R.Ready && R.GetBasicSpell().Ammo > 0)
            {
                var target = TargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range))
                {
                    var rPred = R.GetPrediction(target);

                    if (rPred.HitChance >= HitChance.High)
                    {
                        R.Cast(rPred.UnitPosition);
                    }
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
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }

            if (KillStealOption.UseR && R.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(R.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.R)))
                {
                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.HitChance >= HitChance.High)
                        {
                            R.Cast(rPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(R.Range);

            if (target.IsValidTarget(R.Range) && !target.IsUnKillable() && (!target.IsValidAutoRange() || !Orbwalker.CanAttack()))
            {
                if (ComboOption.UseR && R.Ready &&
                    R.GetBasicSpell().Ammo >= ComboOption.GetSlider("ComboRLimit").Value &&
                    target.IsValidTarget(R.Range) && target.HealthPercent() <= ComboOption.GetSlider("ComboRHP").Value)
                {
                    var rPred = R.GetPrediction(target);

                    if (rPred.HitChance >= HitChance.High)
                    {
                        R.Cast(rPred.UnitPosition);
                    }
                    else if (rPred.HitChance == HitChance.Collision)
                    {
                        foreach (var collsion in rPred.CollisionObjects.Where(x => x.IsValidTarget(R.Range)))
                        {
                            if (collsion.DistanceSqr(target) <= 85 * 85)
                            {
                                R.Cast(collsion.ServerPosition);
                            }
                        }
                    }
                }

                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.HitChance >= HitChance.High)
                    {
                        Q.Cast(qPred.CastPosition);
                    }
                }

                if (ComboOption.UseE && E.Ready && target.IsValidTarget(E.Range))
                {
                    E.Cast();
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range))
                {
                    if (HarassOption.UseR && R.Ready &&
                        R.GetBasicSpell().Ammo >= HarassOption.GetSlider("HarassRLimit").Value &&
                        target.IsValidTarget(R.Range))
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.HitChance >= HitChance.High)
                        {
                            R.Cast(rPred.UnitPosition);
                        }
                    }

                    if (HarassOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }

                    if (HarassOption.UseE && E.Ready && target.IsValidAutoRange())
                    {
                        E.Cast();
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
                if (LaneClearOption.UseQ && Q.Ready)
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

                if (LaneClearOption.UseE && E.Ready)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToArray();

                    if (minions.Any() && minions.Length >= LaneClearOption.GetSlider("LaneClearECount").Value)
                    {
                        E.Cast();
                    }
                }

                if (LaneClearOption.UseR && R.Ready &&
                    R.GetBasicSpell().Ammo >= LaneClearOption.GetSlider("LaneClearRLimit").Value)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        var rFarm = R.GetSpellFarmPosition(minions);

                        if (rFarm.HitCount >= LaneClearOption.GetSlider("LaneClearRCount").Value)
                        {
                            R.Cast(rFarm.CastPosition);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs =
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(R.Range) && x.IsMob() && !x.IsValidAutoRange())
                        .ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseR && R.Ready &&
                        R.GetBasicSpell().Ammo >= JungleClearOption.GetSlider("JungleClearRLimit").Value &&
                        mob.IsValidTarget(R.Range) && !mob.IsValidAutoRange())
                    {
                        R.Cast(mob);
                    }

                    if (JungleClearOption.UseQ && Q.Ready &&
                        mob.IsValidTarget(Q.Range) && !mob.IsValidAutoRange())
                    {
                        Q.Cast(mob);
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
                        if (Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            var target = Args.Target as Obj_AI_Hero;

                            if (target != null && target.IsValidTarget() && !target.IsUnKillable())
                            {
                                if (ComboOption.UseR && R.Ready &&
                                    R.GetBasicSpell().Ammo >= ComboOption.GetSlider("ComboRLimit").Value &&
                                    target.IsValidTarget(R.Range) &&
                                    target.HealthPercent() <= ComboOption.GetSlider("ComboRHP").Value)
                                {
                                    var rPred = R.GetPrediction(target);

                                    if (rPred.HitChance >= HitChance.High)
                                    {
                                        R.Cast(rPred.UnitPosition);
                                    }
                                }
                                else if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                                else if (ComboOption.UseE && E.Ready && target.IsValidAutoRange())
                                {
                                    E.Cast();
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                        {
                            if (Args.Target.IsMob())
                            {
                                if (JungleClearOption.HasEnouguMana())
                                {
                                    var mob = Args.Target as Obj_AI_Minion;
                                    if (mob != null && mob.IsValidTarget())
                                    {
                                        if (JungleClearOption.UseR && R.Ready &&
                                            R.GetBasicSpell().Ammo >=
                                            JungleClearOption.GetSlider("JungleClearRLimit").Value)
                                        {
                                            R.Cast(mob);
                                        }
                                        else if (JungleClearOption.UseQ && Q.Ready && mob.IsValidTarget(Q.Range))
                                        {
                                            Q.Cast(mob);
                                        }
                                        else if (JungleClearOption.UseE && E.Ready && mob.IsValidAutoRange())
                                        {
                                            E.Cast();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}