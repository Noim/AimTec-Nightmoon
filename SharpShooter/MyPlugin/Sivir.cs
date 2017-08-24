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

    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Sivir : MyLogic
    {
        public Sivir()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1200f);
            Q.SetSkillshot(0.25f, 90f, 1350f, false, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W);

            E = new Aimtec.SDK.Spell(SpellSlot.E);

            R = new Aimtec.SDK.Spell(SpellSlot.R);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRCount", "Use R| Enemies Count >= x", 3, 1, 5);

            HarassOption.AddMenu();
            HarassOption.AddQ();
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
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddQ();
            MiscOption.AddBool("Q", "AutoQ", "Auto Q| CC");
            MiscOption.AddE();
            MiscOption.AddSubMenu("Block", "Block Spell Settings");
            MyEvadeManager.Attach(MiscMenu["SharpShooter.MiscSettings.Block"].As<Menu>());
            MiscOption.AddR();
            MiscOption.AddBool("R", "AutoR", "Auto R", false);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, false, false, false, true);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PostAttack += PostAttack;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            Auto();
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

        private static void Auto()
        {
            if (Me.IsUnderEnemyTurret())
            {
                return;
            }

            if (MiscOption.GetBool("Q", "AutoQ").Enabled && Q.Ready)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget(Q.Range))
                    {
                        Q.Cast(target.ServerPosition);
                    }
                }
            }

            if (MiscOption.GetBool("R", "AutoR").Enabled && R.Ready && Me.CountEnemyHeroesInRange(850) >= 3)
            {
                R.Cast();
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
            var target = TargetSelector.GetTarget(1500f);

            if (target.IsValidTarget(1500f))
            {
                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range) && !Me.IsDashing())
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.HitChance >= HitChance.High)
                    {
                        Q.Cast(qPred.UnitPosition);
                    }
                }

                if (ComboOption.UseR && Me.CountEnemyHeroesInRange(850) >= ComboOption.GetSlider("ComboRCount").Value &&
                    (target.Health <= Me.GetAutoAttackDamage(target) * 3 && !Q.Ready ||
                     target.Health <= Me.GetAutoAttackDamage(target) * 3 + Me.GetSpellDamage(target, SpellSlot.Q)))
                {
                    R.Cast();
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana() && HarassOption.UseQ && Q.Ready)
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
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseQ && Q.Ready)
                {
                    var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob()).ToArray();

                    if (mobs.Any())
                    {
                        var qFarm = Q.GetSpellFarmPosition(mobs);

                        if (qFarm.HitCount >= 2 || mobs.Any(x => MobsName.Contains(x.UnitSkinName.ToLower())) && qFarm.HitCount >= 1)
                        {
                            Q.Cast(qFarm.CastPosition);
                        }
                    }
                }
            }
        }

        private static void PostAttack(object sender, PostAttackEventArgs Args)
        {
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
                            if (ComboOption.UseW && W.Ready)
                            {
                                var target = (Obj_AI_Hero)Args.Target;

                                if (!target.IsDead && target.IsValidAutoRange())
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
                            if (Args.Target.IsMinion())
                            {
                                if (LaneClearOption.HasEnouguMana() && LaneClearOption.UseW && W.Ready)
                                {
                                    var minions =
                                        GameObjects.EnemyMinions.Count(
                                            x =>
                                                x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + 200) &&
                                                x.IsMinion());

                                    if (minions >= 3)
                                    {
                                        W.Cast();
                                    }
                                }
                            }
                            else if (Args.Target.IsMob())
                            {
                                if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseW && W.Ready)
                                {
                                    var mob = (Obj_AI_Minion)Args.Target;

                                    if (!mob.IsValidAutoRange() ||
                                        !(mob.Health > Me.GetAutoAttackDamage(mob) * 2) ||
                                        !MobsName.Contains(mob.UnitSkinName.ToLower()))
                                    {
                                        return;
                                    }

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
                        if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                        {
                            if (LaneClearOption.HasEnouguMana(true) && LaneClearOption.UseW && W.Ready)
                            {
                                if (Me.CountEnemyHeroesInRange(850) == 0)
                                {
                                    W.Cast();
                                }
                            }
                        }
                    }
                    break;
            }

        }
    }
}