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

    internal class Tristana : MyLogic
    {
        public Tristana()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.50f, 250f, 1400f, false, SkillshotType.Circle);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 700f);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 700f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboQAlways", "Use Q| Always Cast it(Off = Logic Cast)", false);
            ComboOption.AddE();
            ComboOption.AddBool("ComboEOnlyAfterAA", "Use E| Only After Attack Cast it");
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRHp", "Use R| Player HealthPercent <= x%(Save mySelf)", 25, 0, 100);

            HarassOption.AddMenu();
            HarassOption.AddE(false);
            HarassOption.AddBool("HarassEToMinion", "Use E| Cast Low Hp Minion");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddE();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddE();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddE();
            MiscOption.AddKey("E", "SemiE", "Semi-manual E Key", KeyCode.T, KeybindType.Press);
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "Forcustarget", "Forcus Attack Passive Target");

            DrawOption.AddMenu();
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(false, false, true, true, true);

            Game.OnUpdate += OnUpdate;
            Gapcloser.OnGapcloser += OnGapcloser;
            Orbwalker.PreAttack += PreAttack;
            Orbwalker.PostAttack += PostAttack;
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

            if (E.GetBasicSpell().Level > 0)
            {
                E.Range = 630 + 7 * (Me.Level - 1);
            }

            if (R.GetBasicSpell().Level > 0)
            {
                R.Range = 630 + 7 * (Me.Level - 1);
            }

            if (MiscOption.GetKey("E", "SemiE").Enabled && E.Ready)
            {
                OneKeyCastE();
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

        private static void OneKeyCastE()
        {
            var target = MyTargetSelector.GetTarget(E.Range);

            if (target.IsValidTarget(E.Range))
            {
                if (target.Health <
                    Me.GetSpellDamage(target, SpellSlot.E) * (target.GetBuffCount("TristanaECharge") * 0.30) +
                    Me.GetSpellDamage(target, SpellSlot.E))
                {
                    E.CastOnUnit(target);
                }

                if (Me.CountEnemyHeroesInRange(1200) == 1)
                {
                    if (Me.HealthPercent() >= target.HealthPercent() && Me.Level + 1 >= target.Level)
                    {
                        E.CastOnUnit(target);
                    }
                    else if (Me.HealthPercent() + 20 >= target.HealthPercent() &&
                        Me.HealthPercent() >= 40 && Me.Level + 2 >= target.Level)
                    {
                        E.CastOnUnit(target);
                    }
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.Health <
                                                   Me.GetSpellDamage(x, SpellSlot.E) *
                                                   (x.GetBuffCount("TristanaECharge") * 0.30) +
                                                   Me.GetSpellDamage(x, SpellSlot.E)))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        E.CastOnUnit(target);
                    }
                }
            }

            if (KillStealOption.UseR && R.Ready)
            {
                if (KillStealOption.UseE && E.Ready)
                {
                    foreach (
                        var target in
                        from x in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && KillStealOption.GetKillStealTarget(x.ChampionName))
                        let etargetstacks = x.Buffs.Find(buff => buff.Name == "TristanaECharge")
                        where Me.GetSpellDamage(x, SpellSlot.R) + Me.GetSpellDamage(x, SpellSlot.E) + etargetstacks?.Count * 0.30 * Me.GetSpellDamage(x, SpellSlot.E) >= x.Health
                        select x)
                    {
                        if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                        {
                            R.CastOnUnit(target);
                            return;
                        }
                    }
                }
                else
                {
                    var target = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && KillStealOption.GetKillStealTarget(x.ChampionName))
                        .OrderByDescending(x => x.Health).FirstOrDefault(x => x.Health < Me.GetSpellDamage(x, SpellSlot.R));

                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(E.Range);

            if (target.IsValidTarget(E.Range))
            {
                if (ComboOption.UseQ && Q.Ready)
                {
                    if (!ComboOption.GetBool("ComboQAlways").Enabled)
                    {
                        if (!E.Ready && target.HasBuff("TristanaECharge"))
                        {
                            Q.Cast();
                        }
                        else if (!E.Ready && !target.HasBuff("TristanaECharge") && E.GetBasicSpell().Cooldown > 4)
                        {
                            Q.Cast();
                        }
                    }
                    else
                    {
                        Q.Cast();
                    }
                }

                if (ComboOption.UseE && E.Ready && !ComboOption.GetBool("ComboQAlways").Enabled && target.IsValidTarget(E.Range))
                {
                    E.CastOnUnit(target);
                }

                if (ComboOption.UseR && R.Ready && Me.HealthPercent() <= ComboOption.GetSlider("ComboRHp").Value)
                {
                    var dangerenemy =
                        GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range))
                            .OrderBy(x => x.Distance(Me))
                            .FirstOrDefault();

                    if (dangerenemy != null)
                    {
                        R.CastOnUnit(dangerenemy);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (E.Ready)
                {
                    if (HarassOption.UseE)
                    {
                        var target = HarassOption.GetTarget(E.Range);

                        if (target.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(target);
                        }
                    }

                    if (HarassOption.GetBool("HarassEToMinion").Enabled)
                    {
                        foreach (
                            var minion in
                            GameObjects.EnemyMinions.Where(
                                x =>
                                    x.IsValidTarget(E.Range) && x.IsMinion() && x.Health < Me.GetAutoAttackDamage(x) &&
                                    x.CountEnemyHeroesInRange(x.BoundingRadius + 150) >= 1))
                        {
                            var target = HarassOption.GetTarget(E.Range);

                            if (target != null)
                            {
                                return;
                            }

                            E.CastOnUnit(minion);
                            Orbwalker.ForceTarget(minion);
                            LastForcusTime = Game.TickCount;
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
                JungleClear();
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMob() && !x.Name.ToLower().Contains("mini")).ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseE && E.Ready)
                    {
                        E.CastOnUnit(mob);
                    }

                    if (JungleClearOption.UseQ && Q.Ready && !E.Ready)
                    {
                        Q.Cast();
                    }
                }
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (R.Ready && target != null && target.IsValidTarget(R.Range))
            {
                switch (Args.Type)
                {
                    case SpellType.Melee:
                        if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
                        {
                            R.CastOnUnit(target);
                        }
                        break;
                    case SpellType.Dash:
                    case SpellType.SkillShot:
                    case SpellType.Targeted:
                        {
                            R.CastOnUnit(target);
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
                case GameObjectType.obj_AI_Hero:
                    {
                        var target = Args.Target as Obj_AI_Hero;

                        if (target != null && target.IsValidTarget())
                        {
                            if (Orbwalker.Mode == OrbwalkingMode.Combo)
                            {
                                if (MiscOption.GetBool("Forcus", "Forcustarget").Enabled)
                                {
                                    foreach (
                                        var forcusTarget in
                                        GameObjects.EnemyHeroes.Where(
                                            x => x.IsValidAutoRange() && x.HasBuff("TristanaEChargeSound")))
                                    {
                                        Orbwalker.ForceTarget(forcusTarget);
                                        LastForcusTime = Game.TickCount;
                                    }
                                }

                                if (ComboOption.UseQ && Q.Ready)
                                {
                                    if (target.HasBuff("TristanaEChargeSound") || target.HasBuff("TristanaECharge"))
                                    {
                                        Q.Cast();
                                    }

                                    if (ComboOption.GetBool("ComboQAlways").Enabled)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                            else if (Orbwalker.Mode == OrbwalkingMode.Mixed ||
                                     Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellFarm)
                            {
                                if (MiscOption.GetBool("Forcus", "Forcustarget").Enabled)
                                {
                                    foreach (
                                        var forcusTarget in
                                        GameObjects.EnemyHeroes.Where(
                                            x => x.IsValidAutoRange() && x.HasBuff("TristanaEChargeSound")))
                                    {
                                        Orbwalker.ForceTarget(forcusTarget);
                                        LastForcusTime = Game.TickCount;
                                    }
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        if (Args.Target.IsMob())
                        {
                            var mob = Args.Target as Obj_AI_Minion;

                            if (mob != null && mob.IsValidTarget())
                            {
                                if (JungleClearOption.HasEnouguMana())
                                {
                                    if (JungleClearOption.UseQ && Q.Ready)
                                    {
                                        Q.Cast();
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
            Orbwalker.ForceTarget(null);

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

                            if (target != null && target.IsValidTarget(E.Range))
                            {
                                if (ComboOption.UseE && E.Ready && ComboOption.GetBool("ComboEOnlyAfterAA").Enabled)
                                {
                                    E.CastOnUnit(target);
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
                                    E.CastOnUnit(Args.Target as Obj_AI_Base);

                                    if (LaneClearOption.UseQ && Q.Ready)
                                    {
                                        Q.Cast();
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