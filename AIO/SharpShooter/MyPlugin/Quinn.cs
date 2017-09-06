namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Library;
    using Flowers_Library.Prediction;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Quinn : MyLogic
    {
        public Quinn()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1000f);
            Q.SetSkillshot(0.25f, 90f, 1550f, true, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 2000f);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 700f) { Delay = 0.25f };

            R = new Aimtec.SDK.Spell(SpellSlot.R, 550f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= ", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Auto E| AntiGapcloser");
            MiscOption.AddR();
            MiscOption.AddBool("R", "AutoR", "Auto R");
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "Forcus", "Forcus Attack Passive Target");

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddE(E);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, false, true, false, true);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PreAttack += PreAttack;
            Orbwalker.PostAttack += PostAttack;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Game.TickCount - LastForcusTime > Orbwalker.WindUpTime)
            {
                if (Orbwalker.Mode != OrbwalkingMode.None)
                {
                    Orbwalker.ForceTarget(null);
                }
            }

            KillSteal();

            if (Orbwalker.Mode == OrbwalkingMode.None)
            {
                AutoR();
            }

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
                            Q.Cast(qPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(Q.Range);

            if (target.IsValidTarget(Q.Range))
            {
                if (ComboOption.UseE && E.Ready && Me.HasBuff("QuinnR") && target.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(target);
                }

                if (ComboOption.UseQ && Q.Ready && !Me.HasBuff("QuinnR"))
                {
                    if (target.DistanceToPlayer() <= Me.AttackRange + Me.BoundingRadius + target.BoundingRadius - 50 && HavePassive(target))
                    {
                        return;
                    }

                    var qPred = Q.GetPrediction(target);

                    if (qPred.HitChance >= HitChance.High)
                    {
                        Q.Cast(qPred.UnitPosition);
                    }
                }

                if (ComboOption.UseW && W.Ready)
                {
                    if (NavMesh.WorldToCell(target.ServerPosition).Flags == NavCellFlags.Grass)
                    {
                        W.Cast();
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseQ)
                {
                    var target = HarassOption.GetTarget(Q.Range);

                    if (target.IsValidTarget(Q.Range))
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
                    var minions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        var QFarm = Q.GetSpellFarmPosition(minions);

                        if (QFarm.HitCount >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                        {
                            Q.Cast(QFarm.CastPosition);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob()).ToArray();

                if (mobs.Any())
                {
                    if (JungleClearOption.UseQ && Q.Ready)
                    {
                        var QFarm = Q.GetSpellFarmPosition(mobs);

                        if (QFarm.HitCount >= 1)
                        {
                            Q.Cast(QFarm.CastPosition);
                        }
                    }

                    if (JungleClearOption.UseE && E.Ready)
                    {
                        var mob =
                            mobs.FirstOrDefault(
                                x => !x.Name.ToLower().Contains("mini") && x.Health >= Me.GetSpellDamage(x, SpellSlot.E));

                        if (mob != null && mob.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(mob);
                        }
                    }
                }
            }
        }

        private static void AutoR()
        {
            if (MiscOption.GetBool("R", "AutoR").Enabled && R.Ready && R.GetBasicSpell().Name == "QuinnR")
            {
                if (!Me.IsDead && Me.IsInFountainRange())
                {
                    R.Cast();
                }
            }
        }

        private static void PreAttack(object sender, PreAttackEventArgs Args)
        {
            if (MiscOption.GetBool("Forcus", "Forcus").Enabled)
            {
                if (Orbwalker.Mode == OrbwalkingMode.Combo || Orbwalker.Mode == OrbwalkingMode.Mixed)
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => !x.IsDead && HavePassive(x)))
                    {
                        if (enemy.IsValidAutoRange())
                        {
                            Orbwalker.ForceTarget(enemy);
                            LastForcusTime = Game.TickCount;
                        }
                    }
                }
                else if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                {
                    var all =
                        GameObjects.EnemyMinions.Where(x => x.IsValidAutoRange() && HavePassive(x))
                            .OrderBy(x => x.MaxHealth)
                            .FirstOrDefault();

                    if (all.IsValidAutoRange())
                    {
                        Orbwalker.ForceTarget(all);
                        LastForcusTime = Game.TickCount;
                    }
                }
            }
        }

        private static void PostAttack(object sender, PostAttackEventArgs Args)
        {
            Orbwalker.ForceTarget(null);

            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() ||
                Args.Target.Health <= 0 || Orbwalker.Mode == OrbwalkingMode.None)
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

                            if (target != null && target.IsValidTarget())
                            {
                                if (ComboOption.UseE && E.Ready)
                                {
                                    E.CastOnUnit(target);
                                }
                                else if (ComboOption.UseQ && Q.Ready && !Me.HasBuff("QuinnR"))
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
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                        {
                            if (Args.Target.IsMob() && JungleClearOption.HasEnouguMana())
                            {
                                var mob = Args.Target as Obj_AI_Minion;

                                if (JungleClearOption.UseE && E.Ready && mob.IsValidTarget(E.Range))
                                {
                                    E.CastOnUnit(mob);
                                }
                                else if (JungleClearOption.UseQ && Q.Ready && mob.IsValidTarget(Q.Range) &&
                                         !Me.HasBuff("QuinnR"))
                                {
                                    Q.Cast(mob);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (MiscOption.GetBool("E", "AutoE").Enabled && E.Ready && target != null && target.IsValidTarget(E.Range))
            {
                switch (Args.Type)
                {
                    case SpellType.Melee:
                        if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
                        {
                            E.CastOnUnit(target);
                        }
                        break;
                    case SpellType.Dash:
                    case SpellType.SkillShot:
                    case SpellType.Targeted:
                        {
                            E.CastOnUnit(target);
                        }
                        break;
                }
            }
        }

        private static bool HavePassive(Obj_AI_Base target)
        {
            return target.HasBuff("quinnw");
        }
    }
}