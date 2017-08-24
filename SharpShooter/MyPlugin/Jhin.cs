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

    internal class Jhin : MyLogic
    {
        private static Obj_AI_Hero rShotTarget { get; set; }
        private static int lastETime { get; set; }
        private static bool isAttacking { get; set; }

        public Jhin()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 600f);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 2500f);
            W.SetSkillshot(0.75f, 40f, 5000f, false, SkillshotType.Line);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 750f);
            E.SetSkillshot(0.50f, 120f, 1600f, false, SkillshotType.Circle);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 3500f);
            R.SetSkillshot(0.21f, 80f, 5000f, true, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQMinion", "Use Q| On Minion", false);
            ComboOption.AddW();
            ComboOption.AddBool("ComboWAA", "Use W| After Attack");
            ComboOption.AddBool("ComboWOnly", "Use W| Only Use to MarkTarget");
            ComboOption.AddE();
            ComboOption.AddR();

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddBool("HarassQMinion", "Use Q| On Minion");
            HarassOption.AddW();
            HarassOption.AddBool("HarassWOnly", "Use W| Only Use to MarkTarget");
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddW();
            LaneClearOption.AddMana();
            LaneClearOption.AddBool("LaneClearReload", "Use Spell Clear| Only Jhin Reloading");

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddMenu();
            LastHitOption.AddQ();
            LastHitOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddW();
            KillStealOption.AddBool("KillStealWInAttackRange", "Use W| Target In Attack Range");

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddW();
            MiscOption.AddBool("W", "AutoW", "Auto W| CC");
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Auto E| CC");
            MiscOption.AddR();
            MiscOption.AddBool("R", "rMenuAuto", "Auto R");
            MiscOption.AddKey("R", "rMenuSemi", "Semi-manual R Key", KeyCode.T, KeybindType.Press);
            MiscOption.AddBool("R", "rMenuCheck", "Use R| Check is Safe?");
            MiscOption.AddSlider("R", "rMenuMin", "Use R| Min Range >= x", 1000, 500, 2500);
            MiscOption.AddSlider("R", "rMenuMax", "Use R| Max Range <= x", 3000, 1500, 3500);
            MiscOption.AddSlider("R", "rMenuKill", "Use R| Min Shot Can Kill >= x (0 = off)", 3, 0, 4);

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, true, false, true, true);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PostAttack += PostAttack;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            RLogic();

            if (R.GetBasicSpell().Name == "JhinRShot")
            {
                Orbwalker.AttackingEnabled = false;
                Orbwalker.MovingEnabled = false;
                return;
            }

            Orbwalker.AttackingEnabled = true;
            Orbwalker.MovingEnabled = true;

            KillSteal();
            Auto();

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

        private static void RLogic()
        {
            Obj_AI_Hero target = null;

            if (TargetSelector.GetSelectedTarget() != null &&
                TargetSelector.GetSelectedTarget().DistanceToPlayer() <= MiscOption.GetSlider("R", "rMenuMax").Value)
            {
                target = TargetSelector.GetSelectedTarget();
            }
            else
            {
                target = TargetSelector.GetTarget(R.Range);
            }

            if (R.Ready)
            {
                switch (R.GetBasicSpell().Name)
                {
                    case "JhinR":
                        {
                            if (target.IsValidTarget(R.Range))
                            {
                                var rPred = R.GetPrediction(target);

                                if (MiscOption.GetKey("R", "rMenuSemi").Value)
                                {
                                    if (R.Cast(rPred.UnitPosition))
                                    {
                                        rShotTarget = target;
                                        return;
                                    }
                                }

                                if (!MiscOption.GetBool("R", "rMenuAuto").Enabled)
                                {
                                    return;
                                }

                                if (MiscOption.GetBool("R", "rMenuCheck").Enabled && Me.CountEnemyHeroesInRange(800f) > 0)
                                {
                                    return;
                                }

                                if (target.DistanceToPlayer() <= MiscOption.GetSlider("R", "rMenuMin").Value)
                                {
                                    return;
                                }

                                if (target.DistanceToPlayer() > MiscOption.GetSlider("R", "rMenuMax").Value)
                                {
                                    return;
                                }

                                if (MiscOption.GetSlider("R", "rMenuKill").Value == 0 ||
                                    target.Health > Me.GetSpellDamage(target, SpellSlot.R) * MiscOption.GetSlider("R", "rMenuKill").Value)
                                {
                                    return;
                                }

                                if (IsSpellHeroCollision(target, R))
                                {
                                    return;
                                }

                                if (R.Cast(rPred.UnitPosition))
                                {
                                    rShotTarget = target;
                                }
                            }
                        }
                        break;
                    case "JhinRShot":
                        {
                            var selectTarget = TargetSelector.GetSelectedTarget();

                            if (selectTarget != null && selectTarget.IsValidTarget(R.Range) && InRCone(selectTarget))
                            {
                                var rPred = R.GetPrediction(selectTarget);

                                if (MiscOption.GetKey("R", "rMenuSemi").Enabled)
                                {
                                    AutoUse(rShotTarget);

                                    if (rPred.HitChance >= HitChance.High)
                                    {
                                        R.Cast(rPred.UnitPosition);
                                    }

                                    return;
                                }

                                if (ComboOption.UseR && Orbwalker.Mode == OrbwalkingMode.Combo)
                                {
                                    AutoUse(rShotTarget);

                                    if (rPred.HitChance >= HitChance.High)
                                    {
                                        R.Cast(rPred.UnitPosition);
                                    }

                                    return;
                                }

                                if (!MiscOption.GetBool("R", "rMenuAuto").Enabled)
                                {
                                    return;
                                }

                                AutoUse(rShotTarget);

                                if (rPred.HitChance >= HitChance.High)
                                {
                                    R.Cast(rPred.UnitPosition);
                                }

                                return;
                            }

                            foreach (
                                var t in
                                GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && InRCone(x))
                                    .OrderBy(x => x.Health).ThenByDescending(x => Me.GetSpellDamage(x, SpellSlot.R)))
                            {
                                if (t.IsValidTarget(R.Range) && !target.IsUnKillable())
                                {
                                    var rPred = R.GetPrediction(t);

                                    if (MiscOption.GetKey("R", "rMenuSemi").Enabled)
                                    {
                                        AutoUse(t);

                                        if (rPred.HitChance >= HitChance.High)
                                        {
                                            R.Cast(rPred.UnitPosition);
                                        }

                                        return;
                                    }

                                    if (ComboOption.UseR && Orbwalker.Mode == OrbwalkingMode.Combo)
                                    {
                                        AutoUse(t);

                                        if (rPred.HitChance >= HitChance.High)
                                        {
                                            R.Cast(rPred.UnitPosition);
                                        }

                                        return;
                                    }

                                    if (!MiscOption.GetBool("R", "rMenuAuto").Enabled)
                                    {
                                        return;
                                    }

                                    AutoUse(t);

                                    if (rPred.HitChance >= HitChance.High)
                                    {
                                        R.Cast(rPred.UnitPosition);
                                    }

                                    return;
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static void KillSteal()
        {
            if (R.GetBasicSpell().Name == "JhinRShot")
            {
                return;
            }

            if (KillStealOption.UseQ && Q.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        Q.CastOnUnit(target);
                    }
                }
            }

            if (KillStealOption.UseW && W.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(W.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                {
                    if (target.IsValidTarget(W.Range) && !target.IsUnKillable())
                    {
                        var wPred = W.GetPrediction(target);

                        if (target.Health < Me.GetSpellDamage(target, SpellSlot.Q) && Q.Ready &&
                            target.IsValidTarget(Q.Range))
                        {
                            return;
                        }

                        if (KillStealOption.GetBool("KillStealWInAttackRange").Enabled && target.IsValidAutoRange())
                        {
                            if (wPred.HitChance >= HitChance.High)
                            {
                                W.Cast(wPred.UnitPosition);
                            }
                            return;
                        }

                        if (target.IsValidAutoRange() &&
                            target.Health <= Me.GetAutoAttackDamage(target))
                        {
                            return;
                        }

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Auto()
        {
            if (R.GetBasicSpell().Name == "JhinRShot")
            {
                return;
            }

            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(W.Range) && !x.CanMoveMent()))
            {
                if (MiscOption.GetBool("W", "AutoW").Enabled && W.Ready && target.IsValidTarget(W.Range))
                {
                    W.Cast(target.ServerPosition);
                }

                if (MiscOption.GetBool("E", "AutoE").Enabled && E.Ready &&
                    target.IsValidTarget(E.Range) && Game.TickCount - lastETime > 2500 && !isAttacking)
                {
                    E.Cast(target.ServerPosition);
                }
            }
        }

        private static void Combo()
        {
            if (R.GetBasicSpell().Name == "JhinRShot")
            {
                return;
            }

            if (ComboOption.UseW && W.Ready)
            {
                var target = TargetSelector.GetTarget(W.Range);

                if (target != null && target.IsValidTarget(W.Range))
                {
                    if (ComboOption.GetBool("ComboWOnly").Enabled)
                    {
                        if (HasPassive(target))
                        {
                            var wPred = W.GetPrediction(target);

                            if (wPred.HitChance >= HitChance.High)
                            {
                                W.Cast(wPred.UnitPosition);
                            }
                        }
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

            if (ComboOption.UseQ && Q.Ready)
            {
                var target = TargetSelector.GetTarget(Q.Range + 300);
                var qTarget = TargetSelector.GetTarget(Q.Range);

                if (qTarget.IsValidTarget(Q.Range) && !Orbwalker.CanAttack())
                {
                    Q.Cast(qTarget);
                }
                else if (target.IsValidTarget(Q.Range + 300) && ComboOption.GetBool("ComboQMinion").Enabled)
                {
                    if (Me.HasBuff("JhinPassiveReload") || !Me.HasBuff("JhinPassiveReload") &&
                        Me.CountEnemyHeroesInRange(Me.AttackRange + Me.BoundingRadius) == 0)
                    {
                        var qPred =
                            Prediction.Instance.GetPrediction(new PredictionInput { Unit = target, Delay = 0.25f });
                        var bestQMinion =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(300, false, false, qPred.CastPosition) && x.MaxHealth > 5)
                                .Where(x => x.IsValidTarget(Q.Range))
                                .OrderBy(x => x.Distance(target))
                                .ThenBy(x => x.Health)
                                .FirstOrDefault();

                        if (bestQMinion != null && bestQMinion.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(bestQMinion);
                        }
                    }
                }
            }

            if (ComboOption.UseE && E.Ready && Game.TickCount - lastETime > 2500 && !isAttacking)
            {
                var target = TargetSelector.GetTarget(E.Range);

                if (target != null && target.IsValidTarget(E.Range))
                {
                    if (!target.CanMoveMent())
                    {
                        E.Cast(target.ServerPosition);
                    }
                    else
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.HitChance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
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
                    var target = HarassOption.GetTarget(Q.Range + 300);

                    if (target.IsValidTarget(Q.Range))
                    {
                        Q.Cast(target);
                    }
                    else if (target.IsValidTarget(Q.Range + 300) && HarassOption.GetBool("HarassQMinion").Enabled)
                    {
                        if (Me.HasBuff("JhinPassiveReload") || !Me.HasBuff("JhinPassiveReload") &&
                            Me.CountEnemyHeroesInRange(Me.AttackRange + Me.BoundingRadius) == 0)
                        {
                            var qPred =
                                Prediction.Instance.GetPrediction(new PredictionInput { Unit = target, Delay = 0.25f });
                            var bestQMinion =
                                GameObjects.EnemyMinions.Where(x => x.IsValidTarget(300, false, false, qPred.CastPosition) && x.MaxHealth > 5)
                                    .Where(x => x.IsValidTarget(Q.Range))
                                    .OrderBy(x => x.Distance(target))
                                    .ThenBy(x => x.Health)
                                    .FirstOrDefault();

                            if (bestQMinion != null && bestQMinion.IsValidTarget(Q.Range))
                            {
                                Q.CastOnUnit(bestQMinion);
                            }
                        }
                    }
                }

                if (HarassOption.UseE && E.Ready && Game.TickCount - lastETime > 2500 && !isAttacking)
                {
                    var target = HarassOption.GetTarget(E.Range);

                    if (target.IsValidTarget(E.Range))
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.HitChance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                }

                if (HarassOption.UseW && W.Ready)
                {
                    var target = HarassOption.GetTarget(1500);

                    if (target.IsValidTarget(W.Range))
                    {
                        if (HarassOption.GetBool("HarassWOnly").Enabled && !HasPassive(target))
                        {
                            return;
                        }

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
            if (LaneClearOption.GetBool("LaneClearReload").Enabled && !Me.HasBuff("JhinPassiveReload"))
            {
                return;
            }

            if (LaneClearOption.HasEnouguMana())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range + 300) && x.IsMinion()).ToArray();

                if (minions.Any())
                {
                    var minion = minions.MinOrDefault(x => x.Health);

                    if (LaneClearOption.UseQ && Q.Ready)
                    {
                        if (minion != null && minion.IsValidTarget(Q.Range) && minions.Length >= 2 &&
                            minion.Health < Me.GetSpellDamage(minion, SpellSlot.Q))
                        {
                            Q.Cast(minion);
                        }
                    }

                    if (LaneClearOption.UseW && W.Ready)
                    {
                        var wFarm = W.GetSpellFarmPosition(minions);

                        if (wFarm.HitCount >= 3)
                        {
                            W.Cast(wFarm.CastPosition);
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
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob())
                        .OrderBy(x => x.Health)
                        .ToArray();

                if (mobs.Any())
                {
                    var bigmob = mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini"));

                    if (bigmob != null && bigmob.IsValidTarget(E.Range) && (!bigmob.IsValidAutoRange() || !Orbwalker.CanAttack()))
                    {
                        if (JungleClearOption.UseE && E.Ready && bigmob.IsValidTarget(E.Range))
                        {
                            E.Cast(bigmob.ServerPosition);
                        }

                        if (JungleClearOption.UseQ && Q.Ready && bigmob.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(bigmob);
                        }

                        if (JungleClearOption.UseW && W.Ready && bigmob.IsValidTarget(W.Range))
                        {
                            W.Cast(bigmob.ServerPosition);
                        }
                    }
                    else
                    {
                        var farmMobs = mobs.Where(x => x.IsValidTarget(E.Range)).ToArray();

                        if (JungleClearOption.UseE && E.Ready)
                        {
                            var eFarm = E.GetSpellFarmPosition(farmMobs);

                            if (eFarm.HitCount >= 2)
                            {
                                E.Cast(eFarm.CastPosition);
                            }
                        }

                        if (JungleClearOption.UseQ && Q.Ready)
                        {
                            if (farmMobs.Length >= 2)
                            {
                                Q.CastOnUnit(farmMobs.FirstOrDefault());
                            }
                        }

                        if (JungleClearOption.UseW && W.Ready)
                        {
                            var wFarm = W.GetSpellFarmPosition(farmMobs);

                            if (wFarm.HitCount >= 2)
                            {
                                W.Cast(wFarm.CastPosition);
                            }
                        }
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
                    var minion =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                            .OrderBy(x => x.Health)
                            .FirstOrDefault(x => x.Health < Me.GetSpellDamage(x, SpellSlot.Q));

                    if (minion != null && minion.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(minion);
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
                                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                                {
                                    Q.CastOnUnit(target);
                                }
                                else if (ComboOption.UseW && ComboOption.GetBool("ComboWAA").Enabled && W.Ready &&
                                         target.IsValidTarget(W.Range) && HasPassive(target))
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
                                if (HarassOption.HasEnouguMana())
                                {
                                    if (HarassOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                                    {
                                        Q.CastOnUnit(target);
                                    }
                                    else if (HarassOption.UseW && W.Ready && target.IsValidTarget(W.Range) &&
                                             HasPassive(target))
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
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            var spellslot = Me.GetSpellSlotFromName(Args.SpellData.Name);

            if (spellslot == SpellSlot.E)
            {
                lastETime = Game.TickCount;
            }

            if (Args.SpellData.Name.Equals("attack", StringComparison.CurrentCultureIgnoreCase) ||
                Args.SpellData.Name.Equals("crit", StringComparison.CurrentCultureIgnoreCase))
            {
                isAttacking = true;
                DelayAction.Queue(450 + Game.Ping, () => { isAttacking = false; });
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (R.GetBasicSpell().Name == "JhinRShot")
            {
                return;
            }

            if (target != null && target.IsValidTarget(E.Range) && !Args.HaveShield)
            {
                switch (Args.Type)
                {
                    case SpellType.SkillShot:
                        {
                            if (target.IsValidTarget(300))
                            {
                                if (E.Ready && Game.TickCount - lastETime > 2500 && !isAttacking)
                                {
                                    var ePred = E.GetPrediction(target);

                                    E.Cast(ePred.CastPosition);
                                }

                                if (W.Ready && HasPassive(target))
                                {
                                    var wPred = W.GetPrediction(target);

                                    W.Cast(wPred.UnitPosition);
                                }
                            }
                        }
                        break;
                    case SpellType.Melee:
                    case SpellType.Dash:
                    case SpellType.Targeted:
                        {
                            if (target.IsValidTarget(400))
                            {
                                if (E.Ready && Game.TickCount - lastETime > 2500 && !isAttacking)
                                {
                                    var ePred = E.GetPrediction(target);

                                    E.Cast(ePred.CastPosition);
                                }

                                if (W.Ready && HasPassive(target))
                                {
                                    var wPred = W.GetPrediction(target);

                                    W.Cast(wPred.UnitPosition);
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static void AutoUse(Obj_AI_Base target)
        {
            var item = new Item(3363);

            if (item.IsMine && item.Ready)
            {
                item.CastOnPosition(target.ServerPosition);
            }
        }

        private static bool HasPassive(Obj_AI_Base target)
        {
            return target.HasBuff("jhinespotteddebuff");
        }

        private static bool InRCone(GameObject target)
        {
            // Asuvril
            // https://github.com/VivianGit/LeagueSharp/blob/master/Jhin%20As%20The%20Virtuoso/Jhin%20As%20The%20Virtuoso/Extensions.cs#L67-L79
            var range = R.Range;
            const float angle = 70f * (float)Math.PI / 180;
            var end2 = target.Position.To2D() - Me.Position.To2D();
            var edge1 = end2.Rotated(-angle / 2);
            var edge2 = edge1.Rotated(angle);
            var point = target.Position.To2D() - Me.Position.To2D();

            return point.DistanceSquared(new Vector2()) < range * range && edge1.CrossProduct(point) > 0 &&
                   point.CrossProduct(edge2) > 0;
        }

        private static bool IsSpellHeroCollision(Obj_AI_Hero t, Aimtec.SDK.Spell r, int extraWith = 50)
        {
            foreach (
                var hero in
                GameObjects.EnemyHeroes.Where(
                    hero =>
                        hero.IsValidTarget(r.Range + r.Width, false, false, r.From) &&
                        t.NetworkId != hero.NetworkId))
            {
                var prediction = r.GetPrediction(hero);
                var powCalc = Math.Pow(r.Width + extraWith + hero.BoundingRadius, 2);

                if (
                    prediction.UnitPosition.To2D()
                        .Distance(Me.ServerPosition.To2D(), r.GetPrediction(t).CastPosition.To2D(), true, true) <=
                    powCalc)
                {
                    return true;
                }

                if (
                    prediction.UnitPosition.To2D()
                        .Distance(Me.ServerPosition.To2D(), t.ServerPosition.To2D(), true, true) <= powCalc)
                {
                    return true;
                }
            }

            return false;
        }
    }
}