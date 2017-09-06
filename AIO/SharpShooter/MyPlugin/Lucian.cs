namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Events;
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

    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Lucian : MyLogic
    {
        private static bool havePassive = false;
        private static int lastCastTime = 0;

        public Lucian()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 650f) { Delay = 0.35f };

            Q2 = new Aimtec.SDK.Spell(SpellSlot.Q, 900f);
            Q2.SetSkillshot(0.35f, 25f, int.MaxValue, false, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 1000f);
            W.SetSkillshot(0.30f, 80f, 1600f, true, SkillshotType.Line);

            W2 = new Aimtec.SDK.Spell(SpellSlot.W, 1000f);
            W2.SetSkillshot(0.30f, 80f, 1600f, false, SkillshotType.Line);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 425f);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 1200f);
            R.SetSkillshot(0.10f, 110f, 2500f, true, SkillshotType.Line);

            R2 = new Aimtec.SDK.Spell(SpellSlot.R, 1200f);
            R2.SetSkillshot(0.10f, 110f, 2500f, false, SkillshotType.Line);


            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQExtend", "Use Q Extend", false);
            ComboOption.AddW();
            ComboOption.AddBool("ComboWLogic", "Use W |Logic Cast");
            ComboOption.AddBool("ComboEDash", "Use E Dash to target");
            ComboOption.AddBool("ComboEReset", "Use E Reset Auto Attack");
            ComboOption.AddBool("ComboESafe", "Use E| Safe Check");
            ComboOption.AddBool("ComboEWall", "Use E| Dont Dash to Wall");
            ComboOption.AddBool("ComboEShort", "Use E| Enabled the Short E Logic");
            ComboOption.AddR();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddBool("HarassQExtend", "Use Q Extend");
            HarassOption.AddW(false);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddW();
            LaneClearOption.AddE();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddW();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiRKey", "Semi-manual R Key", KeyCode.T, KeybindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddQExtend(Q2);
            DrawOption.AddW(W);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, true, false, true, true);

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            SpellBook.OnCastSpell += OnCastSpell;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalker.PostAttack += PostAttack;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                havePassive = false;
                return;
            }

            if (Game.TickCount - lastCastTime >= 3100)
            {
                havePassive = false;
            }

            if (Me.HasBuff("LucianR"))
            {
                Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                return;
            }

            if (Me.HasBuff("LucianR") || Me.IsDashing())
            {
                return;
            }

            if (MiscOption.GetKey("R", "SemiRKey").Enabled && R.Ready)
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
                Farm();
            }
        }

        private static void SemiRLogic()
        {
            var target = MyTargetSelector.GetTarget(R.Range);

            if (target != null && !target.HaveShiledBuff() && target.IsValidTarget(R.Range))
            {
                R2.Cast(target);
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ && Q.Ready && Me.Mana > Q.GetBasicSpell().Cost + E.GetBasicSpell().Cost)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(Q2.Range) && !x.IsUnKillable() &&
                            x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q2.Range) && !target.IsUnKillable())
                    {
                        QLogic(target);
                    }
                }
            }

            if (KillStealOption.UseW && W.Ready && Me.Mana > Q.GetBasicSpell().Cost + E.GetBasicSpell().Cost + W.GetBasicSpell().Cost)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(W.Range) && !x.IsUnKillable() &&
                            x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                {
                    if (target.IsValidTarget(W.Range) && !target.IsUnKillable())
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseR && R.Ready)
            {
                var target = MyTargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range) && !target.IsUnKillable() && !Me.IsUnderEnemyTurret() &&
                    !target.IsValidTarget(Me.GetFullAttackRange(target)))
                {
                    if (GameObjects.EnemyHeroes.Any(x => x.NetworkId != target.NetworkId && x.Distance(target) <= 550))
                    {
                        return;
                    }

                    var rAmmo = new float[] { 20, 25, 30 }[Me.GetSpell(SpellSlot.R).Level - 1];
                    var rDMG = GetRDamage(target) * rAmmo;

                    if (target.Health + target.HPRegenRate * 3 < rDMG)
                    {
                        if (target.DistanceToPlayer() <= 800 && target.Health < rDMG * 0.6)
                        {
                            R.Cast(target);
                            return;
                        }

                        if (target.DistanceToPlayer() <= 1000 && target.Health < rDMG * 0.4)
                        {
                            R.Cast(target);
                        }
                    }
                }
            }

            if (havePassive || Me.Buffs.Any(x => x.Name.ToLower() == "lucianpassivebuff") || Me.IsDashing())
            {
                return;
            }

            if (ComboOption.GetBool("ComboEDash").Enabled && E.Ready)
            {
                var target = MyTargetSelector.GetTarget(950);

                if (target.IsValidTarget(950) && !target.IsValidTarget(550))
                {
                    DashELogic(target);
                }
            }

            if (Q.Ready && !havePassive && Me.Buffs.All(x => x.Name.ToLower() != "lucianpassivebuff") && !Me.IsDashing())
            {
                var target = MyTargetSelector.GetTarget(Q2.Range);

                if (ComboOption.UseQ)
                {
                    if (target.IsValidTarget(Q2.Range))
                    {
                        QLogic(target, ComboOption.GetBool("ComboQExtend").Enabled);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseQ && Q.Ready)
                {
                    var target = HarassOption.GetTarget(Q2.Range);

                    if (target != null && target.IsValidTarget(Q2.Range))
                    {
                        QLogic(target, HarassOption.GetBool("HarassQExtend").Enabled);
                    }
                }

                if (HarassOption.UseW && W.Ready)
                {
                    var target = HarassOption.GetTarget(W.Range);

                    if (target != null && target.IsValidTarget(W.Range))
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Farm()
        {
            if (MyManaManager.SpellHarass)
            {
                Harass();
            }

            if (MyManaManager.SpellFarm)
            {
                LaneClear();
            }
        }

        private static void LaneClear()
        {
            if (Game.TickCount - lastCastTime < 600 + Game.Ping ||
                Me.Buffs.Any(x => x.Name.ToLower() == "lucianpassivebuff"))
            {
                return;
            }

            if (LaneClearOption.HasEnouguMana())
            {
                if (LaneClearOption.UseQ && Q.Ready)
                {
                    var qMinions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (qMinions.Any())
                    {
                        foreach (var minion in qMinions)
                        {
                            var q2Minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q2.Range) && x.IsMinion()).ToArray();

                            if (minion != null && minion.IsValidTarget(Q.Range) &&
                                Q2.GetHitCounts(q2Minions, Me.ServerPosition.Extend(minion.ServerPosition, 900)) >= 2)
                            {
                                Q.CastOnUnit(minion);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void OnPlayAnimation(Obj_AI_Base sender, Obj_AI_BasePlayAnimationEventArgs Args)
        {
            if (!sender.IsMe || Orbwalker.Mode == OrbwalkingMode.None)
            {
                return;
            }

            if (Args.Animation == "Spell1" || Args.Animation == "Spell2")
            {
                Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
            }
        }

        private static void OnCastSpell(Obj_AI_Base sender, SpellBookCastSpellEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.Slot == SpellSlot.Q || Args.Slot == SpellSlot.W || Args.Slot == SpellSlot.E)
                {
                    havePassive = true;
                    lastCastTime = Game.TickCount;
                }

                if (Args.Slot == SpellSlot.E && Orbwalker.Mode != OrbwalkingMode.None)
                {
                    Orbwalker.ResetAutoAttackTimer();
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.SpellSlot == SpellSlot.Q || Args.SpellSlot == SpellSlot.W || Args.SpellSlot == SpellSlot.E)
                {
                    havePassive = true;
                    lastCastTime = Game.TickCount;
                }
            }
        }

        private static void PostAttack(object sender, PostAttackEventArgs Args)
        {
            havePassive = false;

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

                            if (target != null && target.IsValidTarget())
                            {
                                if (ComboOption.GetBool("ComboEReset").Enabled && E.Ready)
                                {
                                    ResetELogic(target);
                                }
                                else if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                                {
                                    Q.CastOnUnit(target);
                                }
                                else if (ComboOption.UseW && W.Ready)
                                {
                                    if (ComboOption.GetBool("ComboWLogic").Enabled)
                                    {
                                        W2.Cast(target.ServerPosition);
                                    }
                                    else
                                    {
                                        var wPred = W.GetPrediction(target);

                                        if (wPred.HitChance >= HitChance.High)
                                        {
                                            W.Cast(wPred.UnitPosition);
                                        }
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
                            if (Args.Target.IsMob() && MyManaManager.SpellFarm && JungleClearOption.HasEnouguMana())
                            {
                                var mob = Args.Target as Obj_AI_Minion;

                                if (mob != null && mob.IsValidTarget())
                                {
                                    if (JungleClearOption.UseE && E.Ready)
                                    {
                                        E.Cast(Me.ServerPosition.Extend(Game.CursorPos, 130));
                                    }
                                    else if (JungleClearOption.UseQ && Q.Ready)
                                    {
                                        Q.CastOnUnit(mob);
                                    }
                                    else if (JungleClearOption.UseW && W.Ready)
                                    {
                                        W2.Cast(mob.ServerPosition);
                                    }
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
                                if (LaneClearOption.UseE && E.Ready)
                                {
                                    E.Cast(Me.ServerPosition.Extend(Game.CursorPos, 130));
                                }
                                else if (LaneClearOption.UseW && W.Ready)
                                {
                                    W.Cast(Game.CursorPos);
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
                        {
                            if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
                            {
                                E.Cast(Me.ServerPosition.Extend(target.ServerPosition, -E.Range));
                            }
                        }
                        break;
                    case SpellType.Dash:
                        {
                            if (Args.EndPosition.DistanceToPlayer() <= 250 ||
                                target.ServerPosition.DistanceToPlayer() <= 300)
                            {
                                E.Cast(Me.ServerPosition.Extend(target.ServerPosition, -E.Range));
                            }
                        }
                        break;
                    case SpellType.SkillShot:
                    case SpellType.Targeted:
                        {
                            if (target.ServerPosition.DistanceToPlayer() <= 300)
                            {
                                E.Cast(Me.ServerPosition.Extend(target.ServerPosition, -E.Range));
                            }
                        }
                        break;
                }
            }
        }

        private static void QLogic(Obj_AI_Hero target, bool useExtendQ = true)
        {
            if (!Q.Ready || target == null || target.IsDead || target.IsUnKillable())
            {
                return;
            }

            if (target.IsValidTarget(Q.Range))
            {
                Q.CastOnUnit(target);
            }
            else if (target.IsValidTarget(Q2.Range) && useExtendQ)
            {
                var collisions =
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && (x.IsMinion() || x.IsMob()))
                        .ToArray();

                if (!collisions.Any())
                {
                    return;
                }

                foreach (var minion in collisions)
                {
                    var qPred = Q2.GetPrediction(target);
                    var qPloygon = new MyPolygon.Rectangle(Me.ServerPosition, Me.ServerPosition.Extend(minion.Position, Q2.Range), Q2.Width);

                    if (qPloygon.IsInside(qPred.UnitPosition.To2D()) && minion.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(minion);
                        break;
                    }
                }
            }
        }

        private static void DashELogic(Obj_AI_Base target)
        {
            if (target.DistanceToPlayer() <= Me.GetFullAttackRange(target) ||
                target.DistanceToPlayer() > Me.GetFullAttackRange(target) + E.Range)
            {
                return;
            }

            var dashPos = Me.ServerPosition.Extend(Game.CursorPos, E.Range);
            if (dashPos.IsWall() && ComboOption.GetBool("ComboEWall").Enabled)
            {
                return;
            }

            if (dashPos.CountEnemyHeroesInRange(500) >= 3 && dashPos.CountAllyHeroesInRange(400) < 3 &&
                ComboOption.GetBool("ComboESafe").Enabled)
            {
                return;
            }

            var realRange = Me.BoundingRadius + target.BoundingRadius + Me.AttackRange;
            if (Me.ServerPosition.DistanceToMouse() > realRange * 0.60 &&
                !target.IsValidAutoRange() &&
                target.ServerPosition.Distance(dashPos) < realRange - 45)
            {
                E.Cast(dashPos);
            }
        }

        private static void ResetELogic(Obj_AI_Base target)
        {
            var dashRange = ComboOption.GetBool("ComboEShort").Enabled
                ? (Me.ServerPosition.DistanceToMouse() > Me.GetFullAttackRange(target) ? E.Range : 130)
                : E.Range;
            var dashPos = Me.ServerPosition.Extend(Game.CursorPos, dashRange);

            if (dashPos.IsWall() && ComboOption.GetBool("ComboEWall").Enabled)
            {
                return;
            }

            if (dashPos.CountEnemyHeroesInRange(500) >= 3 && dashPos.CountAllyHeroesInRange(400) < 3 &&
                ComboOption.GetBool("ComboESafe").Enabled)
            {
                return;
            }

            E.Cast(dashPos);
        }

        private static double GetRDamage(Obj_AI_Base target)
        {
            if (Me.GetSpell(SpellSlot.R).Level == 0 || Me.GetSpell(SpellSlot.R).State != SpellState.Ready)
            {
                return 0f;
            }

            var rDMG = new double[] { 20, 35, 50 }[Me.GetSpell(SpellSlot.R).Level - 1] +
                0.1 * Me.TotalAbilityDamage + 0.2 * Me.FlatPhysicalDamageMod;

            return Me.CalculateDamage(target, DamageType.Magical, rDMG);
        }
    }
}
