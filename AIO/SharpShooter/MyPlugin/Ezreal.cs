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

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Ezreal : MyLogic
    {
        public Ezreal()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1150f);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.Line);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 475f) { Delay = 0.65f };

            R = new Aimtec.SDK.Spell(SpellSlot.R, 5000f);
            R.SetSkillshot(1.05f, 160f, 2200f, false, SkillshotType.Line);

            EQ = new Aimtec.SDK.Spell(SpellSlot.Q, 1625f);
            EQ.SetSkillshot(0.90f, 60f, 1350f, true, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboECheck", "Use E |Safe Check");
            ComboOption.AddBool("ComboEWall", "Use E |Wall Check");
            ComboOption.AddR();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddBool("LaneClearQLH", "Use Q| Only LastHit", false);
            LaneClearOption.AddW();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddMana();

            LastHitOption.AddMenu();
            LastHitOption.AddQ();
            LastHitOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddW();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddBool("R", "AutoR", "Auto R");
            MiscOption.AddSlider("R", "RRange", "Auto R |Min Cast Range >= x", 800, 0, 1500);
            MiscOption.AddSlider("R", "RMaxRange", "Auto R |Max Cast Range >= x", 3000, 1500, 5000);
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", KeyCode.T, KeybindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, true, true, true, true);

            Game.OnUpdate += OnUpdate;
            Gapcloser.OnGapcloser += OnGapcloser;
            Orbwalker.PreAttack += PreAttack;
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
                R.Range = MiscOption.GetSlider("R", "RMaxRange").Value;
            }

            if (MiscOption.GetKey("R", "SemiR").Enabled && R.Ready)
            {
                OneKeyCastR();
            }

            if (MiscOption.GetBool("R", "AutoR").Enabled && R.Ready && Me.CountEnemyHeroesInRange(1000) == 0)
            {
                AutoRLogic();
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

            if (Orbwalker.Mode == OrbwalkingMode.Lasthit)
            {
                LastHit();
            }
        }

        private static void OneKeyCastR()
        {
            var target = MyTargetSelector.GetTarget(R.Range);

            if (target.IsValidTarget(R.Range) && !target.IsValidTarget(MiscOption.GetSlider("R", "RRange").Value))
            {
                var rPred = R.GetPrediction(target);

                if (rPred.HitChance >= HitChance.High)
                {
                    R.Cast(rPred.CastPosition);
                }
            }
        }

        private static void AutoRLogic()
        {
            foreach (
                var target in
                GameObjects.EnemyHeroes.Where(
                    x =>
                        x.IsValidTarget(R.Range) && x.DistanceToPlayer() >= MiscOption.GetSlider("R", "RRange").Value))
            {
                if (!target.CanMoveMent() && target.IsValidTarget(EQ.Range) &&
                    Me.GetSpellDamage(target, SpellSlot.R) + Me.GetSpellDamage(target, SpellSlot.Q) * 3 >=
                    target.Health + target.HPRegenRate * 2)
                {
                    R.Cast(target);
                }

                if (Me.GetSpellDamage(target, SpellSlot.R) > target.Health + target.HPRegenRate * 2 &&
                    target.Path.Length < 2 &&
                    R.GetPrediction(target).HitChance >= HitChance.High)
                {
                    R.Cast(target);
                }
            }
        }

        private static void KillSteal()
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
            {
                if (KillStealOption.UseQ && Me.GetSpellDamage(target, SpellSlot.Q) > target.Health &&
                    target.IsValidTarget(Q.Range))
                {
                    if (!target.IsUnKillable())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }

                if (KillStealOption.UseW && Me.GetSpellDamage(target, SpellSlot.W) > target.Health &&
                    target.IsValidTarget(W.Range))
                {
                    if (!target.IsUnKillable())
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }

                if (KillStealOption.UseQ && KillStealOption.UseW &&
                    target.Health < Me.GetSpellDamage(target, SpellSlot.Q) + Me.GetSpellDamage(target, SpellSlot.W) &&
                    target.IsValidTarget(W.Range))
                {
                    if (!target.IsUnKillable())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            W.Cast(qPred.CastPosition);
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(EQ.Range);

            if (target.IsValidTarget(EQ.Range))
            {
                if (ComboOption.UseE && E.Ready && target.IsValidTarget(EQ.Range))
                {
                    ComboELogic(target);
                }

                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.HitChance >= HitChance.High)
                    {
                        Q.Cast(qPred.CastPosition);
                    }
                }

                if (ComboOption.UseW && W.Ready && target.IsValidTarget(W.Range))
                {
                    var wPred = W.GetPrediction(target);

                    if (wPred.HitChance >= HitChance.High)
                    {
                        W.Cast(wPred.UnitPosition);
                    }
                }

                if (ComboOption.UseR && R.Ready)
                {
                    if (Me.IsUnderEnemyTurret() || Me.CountEnemyHeroesInRange(800) > 1)
                    {
                        return;
                    }

                    foreach (var rTarget in GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(R.Range) &&
                            x.DistanceToPlayer() >= MiscOption.GetSlider("R", "RRange").Value))
                    {
                        if (rTarget.Health < Me.GetSpellDamage(rTarget, SpellSlot.R) &&
                            R.GetPrediction(rTarget).HitChance >= HitChance.High &&
                            rTarget.DistanceToPlayer() > Q.Range + E.Range / 2)
                        {
                            R.Cast(target);
                        }

                        if (rTarget.IsValidTarget(Q.Range + E.Range) &&
                            Me.GetSpellDamage(rTarget, SpellSlot.R) +
                            (Q.Ready ? Me.GetSpellDamage(rTarget, SpellSlot.Q) : 0) +
                            (W.Ready ? Me.GetSpellDamage(rTarget, SpellSlot.W) : 0) >
                            rTarget.Health + rTarget.HPRegenRate * 2)
                        {
                            R.Cast(rTarget);
                        }
                    }
                }
            }
        }

        private static void ComboELogic(Obj_AI_Hero target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (ComboOption.GetBool("ComboECheck").Enabled && !Me.IsUnderEnemyTurret() && Me.CountEnemyHeroesInRange(1200f) <= 2)
            {
                if (target.DistanceToPlayer() > Me.GetFullAttackRange(target) && target.IsValidTarget())
                {
                    if (target.Health < Me.GetSpellDamage(target, SpellSlot.E) + Me.GetAutoAttackDamage(target) &&
                        target.ServerPosition.Distance(Game.CursorPos) < Me.ServerPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Me.ServerPosition.Extend(target.ServerPosition, 475f);

                        if (ComboOption.GetBool("ComboEWall").Enabled)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Me.ServerPosition.Extend(target.ServerPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Me.ServerPosition.Extend(target.ServerPosition, 475f));
                        }
                        return;
                    }

                    if (target.Health <
                        Me.GetSpellDamage(target, SpellSlot.E) + Me.GetSpellDamage(target, SpellSlot.W) &&
                        W.Ready &&
                        target.ServerPosition.Distance(Game.CursorPos) + 350 < Me.ServerPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Me.ServerPosition.Extend(target.ServerPosition, 475f);

                        if (ComboOption.GetBool("ComboEWall").Enabled)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Me.ServerPosition.Extend(target.ServerPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Me.ServerPosition.Extend(target.ServerPosition, 475f));
                        }
                        return;
                    }

                    if (target.Health <
                        Me.GetSpellDamage(target, SpellSlot.E) + Me.GetSpellDamage(target, SpellSlot.Q) &&
                        Q.Ready &&
                        target.ServerPosition.Distance(Game.CursorPos) + 300 < Me.ServerPosition.Distance(Game.CursorPos))
                    {
                        var CastEPos = Me.ServerPosition.Extend(target.ServerPosition, 475f);

                        if (ComboOption.GetBool("ComboEWall").Enabled)
                        {
                            if (!CastEPos.IsWall())
                            {
                                E.Cast(Me.ServerPosition.Extend(target.ServerPosition, 475f));
                            }
                        }
                        else
                        {
                            E.Cast(Me.ServerPosition.Extend(target.ServerPosition, 475f));
                        }
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
                    var target = HarassOption.GetTarget(Q.Range);

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
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
                        foreach (var minion in minions.Where(x => !x.IsDead && x.Health > 0))
                        {
                            if (LaneClearOption.GetBool("LaneClearQLH").Enabled)
                            {
                                if (minion.Health < Me.GetSpellDamage(minion, SpellSlot.Q))
                                {
                                    Q.Cast(minion);
                                }
                            }
                            else
                            {
                                Q.Cast(minion);
                            }
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
                        Q.Cast(mobs[0]);
                    }
                }
            }
        }

        private static void LastHit()
        {
            if (LastHitOption.HasEnouguMana)
            {
                if (LastHitOption.UseQ && Q.Ready)
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                            .Where(
                                x =>
                                    x.DistanceToPlayer() <= Q.Range &&
                                    x.DistanceToPlayer() > Me.GetFullAttackRange(x) &&
                                    x.Health < Me.GetSpellDamage(x, SpellSlot.Q)).ToArray();

                    if (minions.Any())
                    {
                        Q.Cast(minions[0]);
                    }
                }
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

        private static void PreAttack(object sender, PreAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
            {
                return;
            }

            switch (Args.Target.Type)
            {
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
                                    var ally =
                                        GameObjects.AllyHeroes.FirstOrDefault(
                                            x => !x.IsMe && x.IsValidTarget(W.Range, true));

                                    if (ally != null && ally.IsValidTarget(W.Range, true))
                                    {
                                        W.Cast(ally.ServerPosition);
                                    }
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
                                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                                else if (ComboOption.UseW && W.Ready && target.IsValidTarget(W.Range))
                                {
                                    var wPred = W.GetPrediction(target);

                                    if (wPred.HitChance >= HitChance.High)
                                    {
                                        W.Cast(wPred.UnitPosition);
                                    }
                                }
                            }
                            else if (Orbwalker.Mode == OrbwalkingMode.Mixed ||
                                     Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellHarass)
                            {
                                if (!HarassOption.HasEnouguMana() ||
                                    !HarassOption.GetHarassTargetEnabled(target.ChampionName))
                                {
                                    return;
                                }

                                if (HarassOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                                else if (HarassOption.UseW && W.Ready && target.IsValidTarget(W.Range))
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
                    break;
            }
        }
    }
}