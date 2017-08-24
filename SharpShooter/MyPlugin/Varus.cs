namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Damage.JSON;
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

    internal class Varus : MyLogic
    {
        private static int lastQTime, lastETime;

        public Varus()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 925f);
            Q.SetSkillshot(0.25f, 70f, 1650f, false, SkillshotType.Line);
            Q.SetCharged("VarusQ", "VarusQ", 925, 1600, 1.5f);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 0f);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 975f);
            E.SetSkillshot(0.35f, 120f, 1500f, false, SkillshotType.Circle);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 1050f);
            R.SetSkillshot(0.25f, 120f, 1950f, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddSlider("ComboQPassive", "Use Q |Target Stack Count >= x", 3, 0, 3);
            ComboOption.AddBool("ComboQFast", "Use Q |Fast Cast");
            ComboOption.AddE();
            ComboOption.AddSlider("ComboEPassive", "Use E |Target Stack Count >= x", 3, 0, 3);
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R |Solo Mode");
            ComboOption.AddSlider("ComboRCount", "Use R |Min Hit Count >= x", 3, 1, 5);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE(false);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q |Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E |Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
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
            MiscOption.AddBool("R", "AutoR", "Auto R |Anti Gapcloser");

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, true, true, true, true);

            Game.OnUpdate += OnUpdate;
            Gapcloser.OnGapcloser += OnGapcloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            SpellBook.OnCastSpell += OnCastSpell;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Me.HasBuff("VarusQLaunch") || Me.HasBuff("VarusQ"))
            {
                Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
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
            if (R.Ready)
            {
                if (Q.IsCharging)
                {
                    return;
                }

                var target = TargetSelector.GetOrderedTargets(R.Range).FirstOrDefault(x => !x.HaveShiledBuff());

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
            if (KillStealOption.UseQ && Q.Ready && Game.TickCount - lastETime > 1000)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(1600f) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q) + GetWDamage(x)))
                {
                    if (target.IsUnKillable())
                    {
                        return;
                    }

                    if (Q.IsCharging)
                    {
                        if (target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                        else
                        {
                            foreach (
                                var t in
                                GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(Q.ChargedMaxRange))
                                    .OrderBy(x => x.Health))
                            {
                                if (t.IsValidTarget(Q.ChargedMaxRange))
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
                    else
                    {
                        if (target.IsValidTarget(Q.ChargedMinRange))
                        {
                            Q.Cast(target);
                        }
                        else
                        {
                            Q.StartCharging(Game.CursorPos);
                        }
                    }
                    return;
                }
            }

            if (Q.IsCharging)
            {
                return;
            }

            if (KillStealOption.UseE && E.Ready && Game.TickCount - lastQTime > 1000)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(E.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.E) + GetWDamage(x)))
                {
                    if (target.IsUnKillable())
                    {
                        return;
                    }

                    var ePred = E.GetPrediction(target);

                    if (ePred.HitChance >= HitChance.High)
                    {
                        E.Cast(ePred.UnitPosition);
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseE && E.Ready && !Q.IsCharging && Game.TickCount - lastQTime > 750 + Game.Ping)
            {
                var target = TargetSelector.GetTarget(E.Range);

                if (target != null && target.IsValidTarget(E.Range) && (GetBuffCount(target) >= ComboOption.GetSlider("ComboEPassive").Value ||
                    W.GetBasicSpell().Level == 0 || target.Health < Me.GetSpellDamage(target, SpellSlot.E) + GetWDamage(target) ||
                    !target.IsValidAutoRange() && !Q.Ready))
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.HitChance >= HitChance.High)
                    {
                        E.Cast(ePred.UnitPosition);
                    }
                }
            }

            if (ComboOption.UseQ && Q.Ready && Game.TickCount - lastETime > 750 + Game.Ping)
            {
                var target = TargetSelector.GetTarget(1600f);

                if (target != null && target.IsValidTarget(1600f))
                {
                    if (Q.IsCharging)
                    {
                        if (ComboOption.GetBool("ComboQFast").Enabled && target.IsValidTarget(800))
                        {
                            Q.Cast(target);
                        }
                        else if (target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                    }
                    else
                    {
                        if (GetBuffCount(target) >= ComboOption.GetSlider("ComboQPassive").Value || W.GetBasicSpell().Level == 0 ||
                            target.Health < Me.GetSpellDamage(target, SpellSlot.Q) + GetWDamage(target))
                        {
                            Q.StartCharging(Game.CursorPos);
                        }
                    }
                }
                else
                {
                    foreach (var t in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(1600)))
                    {
                        if (t.IsValidTarget(1600))
                        {
                            if (Q.IsCharging)
                            {
                                if (ComboOption.GetBool("ComboQFast").Enabled && target.IsValidTarget(800))
                                {
                                    Q.Cast(target);
                                }
                                else if (t.IsValidTarget(Q.ChargedMaxRange))
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                            }
                            else
                            {
                                if (GetBuffCount(t) >= ComboOption.GetSlider("ComboQPassive").Value || W.GetBasicSpell().Level == 0 ||
                                    t.Health < Me.GetSpellDamage(t, SpellSlot.Q) + GetWDamage(t))
                                {
                                    Q.StartCharging(Game.CursorPos);
                                }
                            }
                        }
                    }
                }
            }

            if (ComboOption.UseR && R.Ready)
            {
                var target = TargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range) && ComboOption.GetBool("ComboRSolo").Value &&
                    Me.CountEnemyHeroesInRange(1000) <= 2)
                {
                    if (target.Health + target.HPRegenRate * 2 <
                         Me.GetSpellDamage(target, SpellSlot.R) + GetWDamage(target) +
                        (E.Ready ? Me.GetSpellDamage(target, SpellSlot.E) : 0) +
                        (Q.Ready ? Me.GetSpellDamage(target, SpellSlot.Q) : 0) + Me.GetAutoAttackDamage(target) * 3)
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.HitChance >= HitChance.High)
                        {
                            R.Cast(rPred.UnitPosition);
                        }
                    }
                }

                foreach (var rTarget in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && !x.HaveShiledBuff()))
                {
                    var rPred = R.GetPrediction(rTarget);

                    if (rPred.AoeTargetsHitCount >= ComboOption.GetSlider("ComboRCount").Value &&
                        Me.CountEnemyHeroesInRange(R.Range) >= ComboOption.GetSlider("ComboRCount").Value)
                    {
                        R.Cast(rPred.CastPosition);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(1600f);

                if (target.IsValidTarget(1600f))
                {
                    if (HarassOption.UseQ && Q.Ready && target.IsValidTarget(1600))
                    {
                        if (Q.IsCharging)
                        {
                            if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                            {
                                if (target.IsValidTarget(800))
                                {
                                    Q.Cast(target);
                                }
                                else
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                            }
                            else
                            {
                                foreach (
                                    var t in
                                    GameObjects.EnemyHeroes.Where(
                                            x => !x.IsDead && x.IsValidTarget(Q.ChargedMaxRange))
                                        .OrderBy(x => x.Health))
                                {
                                    if (t.IsValidTarget(800))
                                    {
                                        Q.Cast(target);
                                    }
                                    else if (t.IsValidTarget(Q.ChargedMinRange))
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
                        else
                        {
                            Q.StartCharging(Game.CursorPos);
                        }
                    }

                    if (Q.IsCharging)
                    {
                        return;
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
                if (LaneClearOption.UseQ && Q.Ready)
                {
                    var qMinions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(1600f) && x.IsMinion()).ToArray();

                    if (qMinions.Any())
                    {
                        var qFarm = Q.GetSpellFarmPosition(qMinions);

                        if (qFarm.HitCount >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                        {
                            if (Q.IsCharging)
                            {
                                if (qFarm.CastPosition.DistanceToPlayer() <= Q.ChargedMaxRange)
                                {
                                    Q.Cast(qFarm.CastPosition);
                                }
                            }
                            else
                            {
                                Q.StartCharging(Game.CursorPos);
                            }
                        }
                    }
                }

                if (Q.IsCharging)
                {
                    return;
                }

                if (LaneClearOption.UseE && E.Ready)
                {
                    var eMinions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToArray();

                    if (eMinions.Any())
                    {
                        var eFarm = E.GetSpellFarmPosition(eMinions);

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
                var mobs =
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(1200) && x.IsMob())
                        .Where(x => !x.Name.Contains("mini"))
                        .ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (mob != null)
                    {
                        if (JungleClearOption.UseQ && Q.Ready && mob.IsValidTarget(1600f))
                        {
                            if (Q.IsCharging)
                            {
                                if (mob.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(mob.ServerPosition);
                                }
                            }
                            else
                            {
                                Q.StartCharging(mob.ServerPosition);
                            }
                        }

                        if (Q.IsCharging)
                        {
                            return;
                        }

                        if (JungleClearOption.UseE && E.Ready && mob.IsValidTarget(E.Range))
                        {
                            E.Cast(mob.ServerPosition);
                        }
                    }
                }
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (MiscOption.GetBool("R", "AutoR").Enabled && R.Ready && target != null && target.IsValidTarget(R.Range))
            {
                switch (Args.Type)
                {
                    case SpellType.Melee:
                        if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
                        {
                            var rPred = R.GetPrediction(target);
                            R.Cast(rPred.UnitPosition);
                        }
                        break;
                    case SpellType.Dash:
                        if (Args.EndPosition.DistanceToPlayer() <= 350)
                        {
                            var rPred = R.GetPrediction(target);
                            R.Cast(rPred.UnitPosition);
                        }
                        break;
                    case SpellType.SkillShot:
                    case SpellType.Targeted:
                        {
                            var rPred = R.GetPrediction(target);
                            R.Cast(rPred.UnitPosition);
                        }
                        break;
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.SpellSlot == SpellSlot.Q)
                {
                    lastQTime = Game.TickCount;
                }

                if (Args.SpellSlot == SpellSlot.E)
                {
                    lastETime = Game.TickCount;
                }
            }
        }

        private static void OnCastSpell(Obj_AI_Base sender, SpellBookCastSpellEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.Slot == SpellSlot.Q)
                {
                    Args.Process = Game.TickCount - lastETime >= 750 + Game.Ping;
                }

                if (Args.Slot == SpellSlot.E)
                {
                    Args.Process = Game.TickCount - lastQTime >= 750 + Game.Ping;
                }
            }
        }

        private static int GetBuffCount(Obj_AI_Base target)
        {
            return target.HasBuff("VarusWDebuff") ? target.GetBuffCount("VarusWDebuff") : 0;
        }

        private static double GetWDamage(Obj_AI_Base target)
        {
            var dmg = GetBuffCount(target) * Me.GetSpellDamage(target, SpellSlot.W, DamageStage.Detonation);

            if (target.Type == GameObjectType.obj_AI_Minion)
            {
                return dmg >= 360 ? 360 : dmg;
            }

            return dmg;
        }
    }
}
