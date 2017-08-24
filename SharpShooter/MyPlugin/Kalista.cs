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
    using Aimtec.SDK.Util.Cache;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Kalista : MyLogic
    {
        private static int lastWTime, lastETime;

        public Kalista()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1150f);
            Q.SetSkillshot(0.35f, 40f, 2400f, true, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 5000f);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 950f);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 1500f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddE();
            ComboOption.AddBool("ComboESlow", "Use E| When Enemy Have Buff and Minion Can KillAble");
            ComboOption.AddBool("ComboGapcloser", "Auto Attack Minion To Gapcloser Target");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddBool("HarassESlow", "Use E| When Enemy Have Buff and Minion Can KillAble");
            HarassOption.AddSliderBool("HarassELeave", "Use E| When Enemy Will Leave E Range And Buff Count >= x", 3, 1, 10);
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddSliderBool("LaneClearE", "Use E| Min KillAble Count >= x", 3, 1, 5, true);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            LastHitOption.AddMenu();
            LastHitOption.AddE();
            LastHitOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddE();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoESteal", "Auto E Steal Mob (Only Buff&Dragon&Baron)");
            MiscOption.AddSliderBool("E", "EToler", "Enabled E Toler DMG", 0, -100, 110, true);
            MiscOption.AddR();
            MiscOption.AddSliderBool("R", "AutoRAlly", "Auto R| My Allies HealthPercent <= x%", 30, 1, 99, true);
            MiscOption.AddBool("R", "Balista", "Auto Balista");
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "ForcusAttack", "Forcus Attack Passive Target");

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(false, false, true, false, false);

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalker.OnNonKillableMinion += OnNonKillableMinion;
            Orbwalker.PreAttack += OnPreAttack;
            Orbwalker.PostAttack += OnPostAttack;
        }

        private static void OnUpdate()
        {
            if (Game.TickCount - LastForcusTime > Orbwalker.WindUpTime)
            {
                if (Orbwalker.Mode != OrbwalkingMode.None)
                {
                    Orbwalker.ForceTarget(null);
                }
            }

            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            KillStealEvent();
            AutoUltEvent();
            AutoEStealEvent();

            if (Orbwalker.Mode == OrbwalkingMode.Combo)
            {
                ComboEvent();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Mixed)
            {
                HarassEvent();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
            {
                ClearEvent();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Lasthit)
            {
                LastHitEvent();
            }
        }

        private static void KillStealEvent()
        {
            if (KillStealOption.UseE && E.Ready)
            {
                if (
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(E.Range) &&
                            x.Health <
                            E.GetKalistaRealDamage(x,
                                MiscOption.GetSliderBool("E", "EToler").Enabled,
                                MiscOption.GetSliderBool("E", "EToler").Value) &&
                            !x.IsUnKillable()).Any(target => target.IsValidTarget(E.Range)))
                {
                    E.Cast();
                }
            }

            if (KillStealOption.UseQ && Q.Ready && Game.TickCount - lastETime > 1000)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q) && !x.IsUnKillable()))
                {
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

        private static void AutoUltEvent()
        {
            if (Me.SpellBook.GetSpell(SpellSlot.R).Level > 0 && R.Ready)
            {
                var ally = GameObjects.AllyHeroes.FirstOrDefault(
                    x => !x.IsMe && !x.IsDead && x.Buffs.Any(a => a.Name.ToLower().Contains("kalistacoopstrikeally")));

                if (ally != null && ally.IsVisible && ally.DistanceToPlayer() <= R.Range)
                {
                    if (MiscOption.GetSliderBool("R", "AutoRAlly").Enabled && Me.CountEnemyHeroesInRange(R.Range) > 0 &&
                        ally.CountEnemyHeroesInRange(R.Range) > 0 &&
                        ally.HealthPercent() <= MiscOption.GetSliderBool("R", "AutoRAlly").Value)
                    {
                        R.Cast();
                    }

                    if (MiscOption.GetBool("R", "Balista").Enabled && ally.ChampionName == "Blitzcrank")
                    {
                        if (
                            GameObjects.EnemyHeroes.Any(
                                x =>
                                    !x.IsDead && x.IsValidTarget() &&
                                    x.Buffs.Any(a => a.Name.ToLower().Contains("rocketgrab"))))
                        {
                            R.Cast();
                        }
                    }
                }
            }
        }

        private static void AutoEStealEvent()
        {
            if (MiscOption.GetBool("E", "AutoESteal").Enabled && E.Ready)
            {
                foreach (
                    var mob in
                    GameObjects.EnemyMinions.Where(
                        x =>
                            x != null && x.IsValidTarget(E.Range) && x.MaxHealth > 5 && x.isBigMob()))
                {
                    if (mob.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) && mob.IsValidTarget(E.Range))
                    {
                        if (mob.Health < E.GetKalistaRealDamage(mob))
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void ComboEvent()
        {
            if (ComboOption.GetBool("ComboGapcloser").Enabled)
            {
                ForcusAttack();
            }

            var target = TargetSelector.GetTarget(Q.Range);

            if (target != null && target.IsValidTarget(Q.Range))
            {
                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range) && !target.IsValidAutoRange())
                {
                    var qPred = Q.GetPrediction(target);

                    if (qPred.HitChance >= HitChance.High)
                    {
                        Q.Cast(qPred.CastPosition);
                    }
                }

                if (ComboOption.UseE && E.Ready && target.IsValidTarget(E.Range) &&
                    Game.TickCount - lastETime > 500 + Game.Ping)
                {
                    if (target.Health < E.GetKalistaRealDamage(target,
                            MiscOption.GetSliderBool("E", "EToler").Enabled,
                            MiscOption.GetSliderBool("E", "EToler").Value) &&
                        !target.IsUnKillable())
                    {
                        E.Cast();
                    }

                    if (ComboOption.GetBool("ComboESlow").Enabled &&
                        target.DistanceToPlayer() > Me.AttackRange + Me.BoundingRadius + 100 &&
                        target.IsValidTarget(E.Range))
                    {
                        var EKillMinion = GameObjects.Minions.Where(x => x.IsValidTarget(Me.GetFullAttackRange(x)))
                            .FirstOrDefault(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) &&
                                                 x.DistanceToPlayer() <= E.Range && x.Health < E.GetKalistaRealDamage(x));

                        if (EKillMinion != null && EKillMinion.IsValidTarget(E.Range) &&
                            target.IsValidTarget(E.Range))
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void ForcusAttack()
        {
            if (GameObjects.EnemyHeroes.All(x => !x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + x.BoundingRadius)) &&
                GameObjects.EnemyHeroes.Any(x => x.IsValidTarget((float)(Me.AttackRange * 1.65) + x.BoundingRadius)))
            {

                var AttackUnit =
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.GetFullAttackRange(x)))
                        .OrderBy(x => x.Distance(Game.CursorPos))
                        .FirstOrDefault();

                if (AttackUnit != null && !AttackUnit.IsDead && AttackUnit.IsValidAutoRange())
                {
                    Orbwalker.ForceTarget(AttackUnit);
                    LastForcusTime = Game.TickCount;
                }
            }
            else
            {
                Orbwalker.ForceTarget(null);
            }
        }

        private static void HarassEvent()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(Q.Range);

                if (target.IsValidTarget(Q.Range))
                {
                    if (HarassOption.UseQ && Q.Ready)
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }

                    if (HarassOption.UseE && E.Ready && Game.TickCount - lastETime > 500 + Game.Ping)
                    {
                        if (HarassOption.GetBool("HarassESlow").Enabled &&
                            target.IsValidTarget(E.Range) &&
                            target.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                        {
                            var EKillMinion = GameObjects.Minions.Where(x => x.IsValidTarget(Me.GetFullAttackRange(x)))
                                .FirstOrDefault(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) &&
                                                     x.DistanceToPlayer() <= E.Range && x.Health < E.GetKalistaRealDamage(x));

                            if (EKillMinion != null && EKillMinion.IsValidTarget(E.Range) &&
                                target.IsValidTarget(E.Range))
                            {
                                E.Cast();
                            }
                        }

                        if (HarassOption.GetSliderBool("HarassELeave").Enabled &&
                            target.DistanceToPlayer() >= 800 &&
                            target.Buffs.Find(a => a.Name.ToLower().Contains("kalistaexpungemarker")).Count >=
                            HarassOption.GetSliderBool("HarassELeave").Value)
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void ClearEvent()
        {
            if (MyManaManager.SpellHarass)
            {
                HarassEvent();
            }

            if (MyManaManager.SpellFarm)
            {
                LaneClearEvent();
                JungleClearEvent();
            }
        }

        private static void LaneClearEvent()
        {
            if (LaneClearOption.HasEnouguMana())
            {
                if (LaneClearOption.GetSliderBool("LaneClearE").Enabled && E.Ready)
                {
                    var KSCount =
                        GameObjects.EnemyMinions.Where(
                                x => x.IsValidTarget(E.Range) && x.IsMinion())
                            .Where(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                            .Count(x => x.Health < E.GetKalistaRealDamage(x));

                    if (KSCount > 0 && KSCount >= LaneClearOption.GetSliderBool("LaneClearE").Value)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void JungleClearEvent()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseE && E.Ready && Game.TickCount - lastETime > 500 + Game.Ping)
                {
                    var KSCount =
                        GameObjects.EnemyMinions.Where(
                                x => x.IsValidTarget(E.Range) && x.IsMob())
                            .Where(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                            .Count(x => x.Health < E.GetKalistaRealDamage(x));

                    if (KSCount > 0)
                    {
                        E.Cast();
                    }
                }

                if (JungleClearOption.UseQ && Q.Ready)
                {
                    var qMob =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob())
                            .OrderBy(x => x.MaxHealth)
                            .FirstOrDefault();

                    if (qMob != null && qMob.IsValidTarget(Q.Range))
                    {
                        Q.Cast(qMob);
                    }
                }
            }
        }

        private static void LastHitEvent()
        {
            if (LastHitOption.HasEnouguMana && LastHitOption.UseE && E.Ready)
            {
                if (GameObjects.EnemyMinions.Any(
                        x =>
                            x.IsValidTarget(E.Range) &&
                            x.Buffs.Any(
                                a =>
                                    a.Name.ToLower().Contains("kalistaexpungemarker") &&
                                    x.Health < E.GetKalistaRealDamage(x))))
                {
                    E.Cast();
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (Me.IsDead || !sender.IsMe)
            {
                return;
            }

            switch (Args.SpellData.Name.ToLower())
            {
                case "kalistaw":
                    lastWTime = Game.TickCount;
                    break;
                case "kalistaexpunge":
                case "kalistaexpungewrapper":
                case "kalistadummyspell":
                    lastETime = Game.TickCount;
                    break;
            }
        }

        private static void OnNonKillableMinion(object sender, NonKillableMinionEventArgs Args)
        {
            if (Me.IsDead || Me.IsRecalling() || !Me.CanMoveMent())
            {
                return;
            }

            if (Orbwalker.Mode == OrbwalkingMode.Combo)
            {
                return;
            }

            if (LastHitOption.HasEnouguMana && LastHitOption.UseE && E.Ready)
            {
                var minion = Args.Target as Obj_AI_Minion;

                if (minion != null && minion.IsValidTarget(E.Range) && Me.CountEnemyHeroesInRange(600) == 0 &&
                    minion.Health < E.GetKalistaRealDamage(minion))
                {
                    E.Cast();
                }
            }
        }

        private static void OnPreAttack(object sender, PreAttackEventArgs Args)
        {
            if (MiscOption.GetBool("Forcus", "ForcusAttack").Enabled && Me.CanMoveMent() && Args.Target != null &&
                !Args.Target.IsDead && Args.Target.Health > 0)
            {
                if (Orbwalker.Mode == OrbwalkingMode.Combo || Orbwalker.Mode == OrbwalkingMode.Mixed)
                {
                    foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead &&
                                                                              x.IsValidAutoRange() &&
                                                                              x.Buffs.Any(
                                                                                  a =>
                                                                                      a.Name.ToLower()
                                                                                          .Contains(
                                                                                              "kalistacoopstrikemarkally"))))
                    {
                        if (!target.IsDead && target.IsValidTarget(Me.GetFullAttackRange(target)))
                        {
                            Orbwalker.ForceTarget(target);
                            LastForcusTime = Game.TickCount;
                        }
                    }
                }
                else if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                {
                    foreach (var target in GameObjects.Minions.Where(x => !x.IsDead && x.IsEnemy &&
                                                  x.IsValidAutoRange() &&
                                                  x.Buffs.Any(
                                                      a =>
                                                          a.Name.ToLower()
                                                              .Contains(
                                                                  "kalistacoopstrikemarkally"))))
                    {
                        if (!target.IsDead && target.IsValidTarget(Me.GetFullAttackRange(target)))
                        {
                            Orbwalker.ForceTarget(target);
                            LastForcusTime = Game.TickCount;
                        }
                    }
                }
            }
        }

        private static void OnPostAttack(object sender, PostAttackEventArgs Args)
        {
            Orbwalker.ForceTarget(null);

            if (Args.Target == null || Args.Target.IsDead || Args.Target.Health <= 0 || Me.IsDead || !Q.Ready)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.obj_AI_Hero:
                    {
                        var target = Args.Target as Obj_AI_Hero;

                        if (target != null && !target.IsDead && target.IsValidTarget(Q.Range))
                        {
                            if (Orbwalker.Mode == OrbwalkingMode.Combo)
                            {
                                if (ComboOption.UseQ)
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                            }
                            else if (HarassOption.HasEnouguMana() &&
                                     (Orbwalker.Mode == OrbwalkingMode.Mixed ||
                                      Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellHarass))
                            {
                                if (HarassOption.UseQ)
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
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        if (MyManaManager.SpellFarm && Orbwalker.Mode == OrbwalkingMode.Laneclear &&
                            JungleClearOption.HasEnouguMana())
                        {
                            var mob = Args.Target as Obj_AI_Minion;

                            if (mob != null && mob.IsValidTarget(Q.Range) && mob.IsMob())
                            {
                                if (JungleClearOption.UseQ)
                                {
                                    Q.Cast(mob);
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}
