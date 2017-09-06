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

    using System.Collections.Generic;
    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Caitlyn : MyLogic
    {
        private static int lastQTime, lastWTime;

        private static float rRange => 500f * Me.GetSpell(SpellSlot.R).Level + 1500f;

        public Caitlyn()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1250f);
            Q.SetSkillshot(0.50f, 50f, 2000f, false, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 800f);
            W.SetSkillshot(0.80f, 80f, 2000f, false, SkillshotType.Circle);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 750f);
            E.SetSkillshot(0.25f, 60f, 1600f, true, SkillshotType.Line);

            R = new Aimtec.SDK.Spell(SpellSlot.R, rRange) {Delay = 1.5f};

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddSlider("ComboQCount", "Use Q |Min Hit Count >= x(0 = Off)", 3, 0, 5);
            ComboOption.AddSlider("ComboQRange", "UseQ |Min Cast Range >= x", 800, 500, 1100);
            ComboOption.AddW();
            ComboOption.AddSlider("ComboWCount", "Use W|Min Stack >= x", 1, 1, 3);
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSafe", "Use R|Safe Check");
            ComboOption.AddSlider("ComboRRange", "Use R|Min Cast Range >= x", 900, 500, 1500);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddQ();
            MiscOption.AddBool("Q", "AutoQ", "Use Q| CC");
            MiscOption.AddW();
            MiscOption.AddBool("W", "AutoWCC", "Use W| CC");
            MiscOption.AddBool("W", "AutoWTP", "Use W| TP");
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Use E| Anti Gapcloser");
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", KeyCode.T, KeybindType.Press);
            //MiscOption.AddSetting("EQ");
            //MiscOption.AddKey("EQKey", "Semi-manual EQ Key", KeyCode.G, KeybindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, false, true, true, true);

            Game.OnUpdate += OnUpdate;
            Gapcloser.OnGapcloser += OnGapcloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            R.Range = rRange;

            //if (MiscOption.GetKey("EQKey").Enabled)
            //{
            //    OneKeyEQ();
            //}

            if (MiscOption.GetKey("R", "SemiR").Enabled && R.Ready)
            {
                OneKeyCastR();
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


        private static void OneKeyCastR()
        {
            var target = MyTargetSelector.GetTarget(R.Range);

            if (target != null && target.IsValidTarget(R.Range))
            {
                R.CastOnUnit(target);
            }
        }

        private static void Auto()
        {
            if (MiscOption.GetBool("Q", "AutoQ").Enabled && Q.Ready &&
                Orbwalker.Mode != OrbwalkingMode.Combo && Orbwalker.Mode != OrbwalkingMode.Mixed)
            {
                var target = MyTargetSelector.GetTarget(Q.Range - 50);

                if (target.IsValidTarget(Q.Range) && !target.CanMoveMent())
                {
                    Q.Cast(target.ServerPosition);
                }
            }

            if (W.Ready)
            {
                if (MiscOption.GetBool("W", "AutoWCC").Enabled)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x => x.IsValidTarget(W.Range) && !x.CanMoveMent() && !x.HasBuff("caitlynyordletrapinternal")))
                    {
                        if (Game.TickCount - lastWTime > 1500)
                        {
                            W.Cast(target.ServerPosition);
                        }
                    }
                }

                if (MiscOption.GetBool("W", "AutoWTP").Enabled)
                {
                    var obj =
                        ObjectManager
                            .Get<Obj_AI_Base>()
                            .FirstOrDefault(x => !x.IsAlly && !x.IsMe && x.DistanceToPlayer() <= W.Range &&
                                                 x.Buffs.Any(
                                                     a =>
                                                         a.Name.ToLower().Contains("teleport") ||
                                                         a.Name.ToLower().Contains("gate")) &&
                                                 !ObjectManager.Get<Obj_AI_Base>()
                                                     .Any(b => b.Name.ToLower().Contains("trap") && b.Distance(x) <= 150));

                    if (obj != null)
                    {
                        if (Game.TickCount - lastWTime > 1500)
                        {
                            W.Cast(obj.ServerPosition);
                        }
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
                    if (target.IsValidAutoRange() && target.Health <= Me.GetAutoAttackDamage(target) * 2)
                    {
                        continue;
                    }

                    if (!target.IsUnKillable())
                    {
                        Q.Cast(target.ServerPosition);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(R.Range);

            if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
            {
                if (ComboOption.UseE && E.Ready && target.IsValidTarget(700))
                {
                    var ePred = E.GetPrediction(target);

                    if (!ePred.CollisionObjects.Any() || ePred.HitChance >= HitChance.High)
                    {
                        if (ComboOption.UseQ && Q.Ready)
                        {
                            if (E.Cast(ePred.CastPosition))
                            {
                                //Q.Cast(target.ServerPosition);
                                System.Console.WriteLine("1");
                            }
                        }
                        else
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                    else
                    {
                        if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range) && !Me.IsDashing())
                        {
                            if (Me.CountEnemyHeroesInRange(ComboOption.GetSlider("ComboQRange").Value) < 0)
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.HitChance >= HitChance.High)
                                {
                                    Q.Cast(target.ServerPosition);
                                    System.Console.WriteLine("2");
                                }

                                if (ComboOption.GetSlider("ComboQCount").Value != 0 &&
                                    Me.CountEnemyHeroesInRange(Q.Range) >= ComboOption.GetSlider("ComboQCount").Value)
                                {
                                    Q.CastIfWillHit(target, ComboOption.GetSlider("ComboQCount").Value);
                                }
                            }
                        }
                    }
                }

                if (ComboOption.UseQ && Q.Ready && !E.Ready && target.IsValidTarget(Q.Range) && !Me.IsDashing())
                {
                    if (Me.CountEnemyHeroesInRange(ComboOption.GetSlider("ComboQRange").Value) < 0)
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                            System.Console.WriteLine("3");
                        }

                        if (ComboOption.GetSlider("ComboQCount").Value != 0 &&
                            Me.CountEnemyHeroesInRange(Q.Range) >= ComboOption.GetSlider("ComboQCount").Value)
                        {
                            Q.CastIfWillHit(target, ComboOption.GetSlider("ComboQCount").Value);
                        }
                    }
                }

                if (ComboOption.UseW && W.Ready && target.IsValidTarget(W.Range) &&
                    W.GetBasicSpell().Ammo >= ComboOption.GetSlider("ComboWCount").Value)
                {
                    if (Game.TickCount - lastWTime > 1800 + Game.Ping * 2)
                    {
                        if (target.CanMoveMent())
                        {
                            if (target.IsFacing(Me))
                            {
                                if (target.IsMelee && target.DistanceToPlayer() < target.AttackRange + 100)
                                {
                                    CastW(Me.ServerPosition);
                                }
                                else
                                {
                                    var wPred = W.GetPrediction(target);

                                    if (wPred.HitChance >= HitChance.High && target.IsValidTarget(W.Range))
                                    {
                                        CastW(wPred.CastPosition);
                                    }
                                }
                            }
                            else
                            {
                                var wPred = W.GetPrediction(target);

                                if (wPred.HitChance >= HitChance.High && target.IsValidTarget(W.Range))
                                {
                                    CastW(wPred.CastPosition +
                                          Vector3.Normalize(target.ServerPosition - Me.ServerPosition) * 100);
                                }
                            }
                        }
                        else
                        {
                            if (target.IsValidTarget(W.Range))
                            {
                                CastW(target.ServerPosition);
                            }
                        }
                    }
                }

                if (ComboOption.UseR && R.Ready && Game.TickCount - lastQTime > 2500)
                {
                    if (ComboOption.GetBool("ComboRSafe").Enabled &&
                        (Me.IsUnderEnemyTurret() || Me.CountEnemyHeroesInRange(1000) > 2))
                    {
                        return;
                    }

                    if (!target.IsValidTarget(R.Range))
                    {
                        return;
                    }

                    if (target.DistanceToPlayer() < ComboOption.GetSlider("ComboRRange").Value)
                    {
                        return;
                    }

                    if (target.Health + target.HPRegenRate * 3 > Me.GetSpellDamage(target, SpellSlot.R))
                    {
                        return;
                    }

                    var RCollision =
                        Collision.GetCollision(new List<Vector3> {target.ServerPosition},
                                new PredictionInput
                                {
                                    Delay = R.Delay,
                                    Radius = 500,
                                    Speed = 1500,
                                    From = ObjectManager.GetLocalPlayer().ServerPosition,
                                    Unit = target,
                                    CollisionObjects = CollisionableObjects.YasuoWall | CollisionableObjects.Heroes
                                })
                            .Any(x => x.NetworkId != target.NetworkId);

                    if (RCollision)
                    {
                        return;
                    }

                    R.CastOnUnit(target);
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseQ && Q.Ready)
                {
                    var target = HarassOption.GetTarget(Q.Range);

                    if (target.IsValidTarget(Q.Range))
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
                    var mobs =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob())
                            .OrderBy(x => x.MaxHealth)
                            .ToArray();

                    if (mobs.Any())
                    {
                        Q.Cast(mobs[0]);
                    }
                }
            }
        }

        private static void OneKeyEQ()
        {
            Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);

            if (E.Ready && Q.Ready)
            {
                var target = MyTargetSelector.GetTarget(E.Range);

                if (target.IsValidTarget(E.Range))
                {
                    var ePred = E.GetPrediction(target);
                    if (ePred.CollisionObjects.Count == 0)
                    {
                        E.Cast(target);
                        Q.Cast(target);
                    }
                }
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (MiscOption.GetBool("E", "AutoE").Enabled && target != null && target.IsValidTarget() && E.Ready)
            {
                if (E.Ready && target.IsValidTarget(E.Range))
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
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (Args.SpellData.Name == Q.GetBasicSpell().Name)
            {
                lastQTime = Game.TickCount;
            }

            if (Args.SpellData.Name == W.GetBasicSpell().Name)
            {
                lastWTime = Game.TickCount;
            }
        }

        private static void CastW(Vector3 position)
        {
            if (
                ObjectManager.Get<GameObject>()
                    .Any(
                        x =>
                            x.IsValid && x.ServerPosition.Distance(position) <= 120 &&
                            x.Name.Equals("cupcake trap",
                                System.StringComparison.CurrentCultureIgnoreCase)))
            {
                return;
            }

            W.Cast(position);
        }
    }
}