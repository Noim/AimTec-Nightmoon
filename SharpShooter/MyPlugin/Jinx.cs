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

    internal class Jinx : MyLogic
    {
        private static float bigGunRange { get; set; }
        private static float rCoolDown { get; set; }

        public Jinx()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 525f);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 1500f);
            W.SetSkillshot(0.60f, 60f, 3300f, true, SkillshotType.Line);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 900f);
            E.SetSkillshot(1.20f, 100f, 1750f, false, SkillshotType.Circle);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 3000f);
            R.SetSkillshot(0.70f, 140f, 1500f, true, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R| Solo Mode");
            ComboOption.AddBool("ComboRTeam", "Use R| Team Fight");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddW();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddW();
            MiscOption.AddBool("W", "AutoW", "Auto W| CC");
            MiscOption.AddE();
            MiscOption.AddBool("E", "AutoE", "Auto E| CC");
            MiscOption.AddBool("E", "AutoETP", "Auto E| Teleport");
            MiscOption.AddR();
            MiscOption.AddKey("R", "rMenuSemi", "Semi-manual R Key", KeyCode.T, KeybindType.Press);
            MiscOption.AddSlider("R", "rMenuMin", "Use R| Min Range >= x", 1000, 500, 2500);
            MiscOption.AddSlider("R", "rMenuMax", "Use R| Max Range <= x", 3000, 1500, 3500);

            DrawOption.AddMenu();
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(false, true, false, true, true);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PreAttack += PreAttack;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Q.GetBasicSpell().Level > 0)
            {
                bigGunRange = Q.Range + new[] { 75, 100, 125, 150, 175 }[Q.GetBasicSpell().Level - 1];
            }

            if (R.GetBasicSpell().Level > 0)
            {
                R.Range = MiscOption.GetSlider("R", "rMenuMax").Value;
            }

            rCoolDown = R.GetBasicSpell().Level > 0
                ? (Me.SpellBook.GetSpell(SpellSlot.R).CooldownEnd - Game.ClockTime < 0
                    ? 0
                    : Me.SpellBook.GetSpell(SpellSlot.R).CooldownEnd - Game.ClockTime)
                : -1;

            if (MiscOption.GetKey("R", "rMenuSemi").Enabled && R.Ready)
            {
                SemiRLogic();
            }

            AutoLogic();
            KillSteal();

            if (Orbwalker.Mode == OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Combo)
            {
                Harass();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
            {
                FarmHarass();
            }
        }

        private static void AutoLogic()
        {
            if (MiscOption.GetBool("W", "AutoW").Enabled && W.Ready)
            {
                foreach (
                    var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(W.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget(W.Range))
                    {
                        W.Cast(target);
                    }
                }
            }

            if (MiscOption.GetBool("E", "AutoE").Enabled && E.Ready)
            {
                foreach (
                    var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && !x.CanMoveMent()))
                {
                    if (target.IsValidTarget(E.Range))
                    {
                        E.Cast(target.ServerPosition);
                    }
                }
            }

            if (MiscOption.GetBool("E", "AutoETP").Enabled && E.Ready)
            {
                foreach (
                    var obj in
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(
                            x =>
                                x.IsEnemy && x.DistanceToPlayer() < E.Range &&
                                (x.HasBuff("teleport_target") || x.HasBuff("Pantheon_GrandSkyfall_Jump"))))
                {
                    if (obj.IsValidTarget(E.Range))
                    {
                        E.Cast(obj.ServerPosition);
                    }
                }
            }
        }

        private static void SemiRLogic()
        {
            var target = TargetSelector.GetTarget(R.Range);

            if (target.IsValidTarget(R.Range))
            {
                var rPred = R.GetPrediction(target);

                if (rPred.HitChance >= HitChance.High)
                {
                    R.Cast(rPred.CastPosition);
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseW && W.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(W.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                {
                    if (target.IsValidAutoRange() && target.Health <= Me.GetAutoAttackDamage(target) * 2)
                    {
                        continue;
                    }

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

            if (KillStealOption.UseR && R.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(R.Range) && x.DistanceToPlayer() > MiscOption.GetSlider("R", "rMenuMin").Value &&
                            KillStealOption.GetKillStealTarget(x.ChampionName) &&
                            x.Health < Me.GetSpellDamage(x, SpellSlot.R)))
                {
                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.HitChance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseW && W.Ready)
            {
                var target = TargetSelector.GetTarget(W.Range);

                if (target.IsValidTarget(W.Range) && target.DistanceToPlayer() > Q.Range
                    && Me.CountEnemyHeroesInRange(W.Range - 300) <= 2)
                {
                    var wPred = W.GetPrediction(target);

                    if (wPred.HitChance >= HitChance.High)
                    {
                        W.Cast(wPred.UnitPosition);
                    }
                }
            }

            if (ComboOption.UseE && E.Ready)
            {
                var target = TargetSelector.GetTarget(E.Range);

                if (target.IsValidTarget(E.Range))
                {
                    if (!target.CanMoveMent())
                    {
                        E.Cast(target);
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

            if (ComboOption.UseQ && Q.Ready)
            {
                var target = TargetSelector.GetTarget(bigGunRange);

                if (Me.HasBuff("JinxQ"))
                {
                    if (Me.Mana < (rCoolDown == -1 ? 100 : (rCoolDown > 10 ? 130 : 150)))
                    {
                        if (Orbwalker.CanAttack())
                        {
                            Q.Cast();
                        }
                    }

                    if (Me.CountEnemyHeroesInRange(1500) == 0)
                    {
                        Q.Cast();
                    }

                    if (target == null)
                    {
                        if (Orbwalker.CanAttack())
                        {
                            Q.Cast();
                        }
                    }
                    else if (target.IsValidTarget(bigGunRange))
                    {
                        if (target.Health < Me.GetAutoAttackDamage(target) * 3 &&
                            target.DistanceToPlayer() <= Q.Range + 60)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
                else
                {
                    if (target.IsValidTarget(bigGunRange))
                    {
                        if (Me.CountEnemyHeroesInRange(Q.Range) == 0 &&
                            Me.CountEnemyHeroesInRange(bigGunRange) > 0 &&
                            Me.Mana > R.GetBasicSpell().Cost + W.GetBasicSpell().Cost + Q.GetBasicSpell().Cost * 2)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }

                        if (target.CountEnemyHeroesInRange(150) >= 2 &&
                            Me.Mana > R.GetBasicSpell().Cost + Q.GetBasicSpell().Cost * 2 + W.GetBasicSpell().Cost &&
                            target.DistanceToPlayer() > Q.Range)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }

            if (ComboOption.UseR && R.Ready)
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(1200)))
                {
                    if (ComboOption.GetBool("ComboRTeam").Enabled && target.IsValidTarget(600) &&
                        Me.CountEnemyHeroesInRange(600) >= 2 &&
                        target.CountAllyHeroesInRange(200) <= 3 && target.HealthPercent() < 50)
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.HitChance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }

                    if (ComboOption.GetBool("ComboRSolo").Enabled && Me.CountEnemyHeroesInRange(1500) <= 2 &&
                        target.DistanceToPlayer() > Q.Range &&
                        target.DistanceToPlayer() < bigGunRange && target.Health > Me.GetAutoAttackDamage(target) &&
                        target.Health < Me.GetSpellDamage(target, SpellSlot.R) + Me.GetAutoAttackDamage(target) * 3)
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.HitChance >= HitChance.High)
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
                if (HarassOption.UseW && W.Ready)
                {
                    var target = HarassOption.GetTarget(W.Range);

                    if (target.IsValidTarget(W.Range) && target.DistanceToPlayer() > Q.Range)
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }

                if (HarassOption.UseQ && Q.Ready)
                {
                    var target = HarassOption.GetTarget(bigGunRange);

                    if (target.IsValidTarget(bigGunRange) && Orbwalker.CanAttack())
                    {
                        if (target.CountEnemyHeroesInRange(150) >= 2 &&
                            Me.Mana > R.GetBasicSpell().Cost + Q.GetBasicSpell().Cost * 2 + W.GetBasicSpell().Cost &&
                            target.DistanceToPlayer() > Q.Range)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }

                        if (target.DistanceToPlayer() > Q.Range &&
                            Me.Mana > R.GetBasicSpell().Cost + Q.GetBasicSpell().Cost * 2 + W.GetBasicSpell().Cost)
                        {
                            if (Orbwalker.CanAttack())
                            {
                                Q.Cast();
                            }
                        }
                    }
                    else
                    {
                        if (Me.HasBuff("JinxQ") && Q.Ready)
                        {
                            Q.Cast();
                        }
                    }
                }
                else if (Me.HasBuff("JinxQ") && Q.Ready)
                {
                    Q.Cast();
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
                var mobs =
                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(bigGunRange) && x.IsMob())
                        .OrderBy(x => x.MaxHealth)
                        .ToArray();

                if (mobs.Any())
                {
                    if (JungleClearOption.UseW && W.Ready &&
                        mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini")) != null)
                    {
                        W.Cast(mobs.FirstOrDefault(x => !x.Name.ToLower().Contains("mini")));
                    }

                    if (JungleClearOption.UseQ && Q.Ready)
                    {
                        if (Me.HasBuff("JinxQ"))
                        {
                            foreach (var mob in mobs)
                            {
                                var count = ObjectManager.Get<Obj_AI_Minion>().Count(x => x.Distance(mob) <= 150);

                                if (mob.DistanceToPlayer() <= bigGunRange)
                                {
                                    if (count < 2)
                                    {
                                        if (Orbwalker.CanAttack())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                    else if (mob.Health > Me.GetAutoAttackDamage(mob) * 1.1f)
                                    {
                                        if (Orbwalker.CanAttack())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                            }

                            if (mobs.Length < 2)
                            {
                                if (Orbwalker.CanAttack())
                                {
                                    Q.Cast();
                                }
                            }
                        }
                        else
                        {
                            foreach (var mob in mobs)
                            {
                                var count = ObjectManager.Get<Obj_AI_Minion>().Count(x => x.Distance(mob) <= 150);

                                if (mob.DistanceToPlayer() <= bigGunRange)
                                {
                                    if (count >= 2)
                                    {
                                        if (Orbwalker.CanAttack())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                    else if (mob.Health < Me.GetAutoAttackDamage(mob) * 1.1f &&
                                             mob.DistanceToPlayer() > Q.Range)
                                    {
                                        if (Orbwalker.CanAttack())
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (Me.HasBuff("JinxQ") && Q.Ready)
                    {
                        Q.Cast();
                    }
                }
            }
            else
            {
                if (Me.HasBuff("JinxQ") && Q.Ready)
                {
                    Q.Cast();
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
                                if (ComboOption.UseQ && Q.Ready)
                                {
                                    if (Me.HasBuff("JinxQ"))
                                    {
                                        if (target.Health < Me.GetAutoAttackDamage(target) * 3 &&
                                            target.DistanceToPlayer() <= Q.Range + 60)
                                        {
                                            Q.Cast();
                                        }
                                        else if (Me.Mana < (rCoolDown == -1 ? 100 : (rCoolDown > 10 ? 130 : 150)))
                                        {
                                            Q.Cast();
                                        }
                                        else if (target.IsValidTarget(Q.Range))
                                        {
                                            Q.Cast();
                                        }
                                    }
                                    else
                                    {
                                        if (target.CountEnemyHeroesInRange(150) >= 2 &&
                                            Me.Mana > R.GetBasicSpell().Cost + Q.GetBasicSpell().Cost * 2 + W.GetBasicSpell().Cost &&
                                            target.DistanceToPlayer() > Q.Range)
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                            }
                            else if (Orbwalker.Mode == OrbwalkingMode.Mixed ||
                                     Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana())
                                {
                                    if (HarassOption.UseQ && Q.Ready)
                                    {
                                        if (Me.HasBuff("JinxQ"))
                                        {
                                            if (target.DistanceToPlayer() >= bigGunRange)
                                            {
                                                Q.Cast();
                                            }
                                        }
                                        else
                                        {
                                            if (target.CountEnemyHeroesInRange(150) >= 2 &&
                                                Me.Mana > R.GetBasicSpell().Cost + Q.GetBasicSpell().Cost * 2 + W.GetBasicSpell().Cost &&
                                                target.DistanceToPlayer() > Q.Range)
                                            {
                                                Q.Cast();
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (Me.HasBuff("JinxQ") && Q.Ready)
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        if (Args.Target.IsMinion())
                        {
                            if (LaneClearOption.HasEnouguMana() && Q.Ready && LaneClearOption.UseQ)
                            {
                                var min = Args.Target as Obj_AI_Base;
                                var minions =
                                    GameObjects.EnemyMinions.Where(x => x.IsValidTarget(bigGunRange) && x.IsMinion())
                                        .ToArray();

                                if (minions.Any() && min != null)
                                {
                                    foreach (var minion in minions.Where(x => x.NetworkId != min.NetworkId))
                                    {
                                        var count =
                                            ObjectManager.Get<Obj_AI_Minion>().Count(x => x.Distance(minion) <= 150);

                                        if (minion.DistanceToPlayer() <= bigGunRange)
                                            if (Me.HasBuff("JinxQ"))
                                            {
                                                if (LaneClearOption.GetSlider("LaneClearQCount").Value > count)
                                                {
                                                    Q.Cast();
                                                }
                                                else if (min.Health > Me.GetAutoAttackDamage(min) * 1.1f)
                                                {
                                                    Q.Cast();
                                                }
                                            }
                                            else if (!Me.HasBuff("JinxQ"))
                                            {
                                                if (LaneClearOption.GetSlider("LaneClearQCount").Value <= count)
                                                {
                                                    Q.Cast();
                                                }
                                                else if (min.Health < Me.GetAutoAttackDamage(min) * 1.1f &&
                                                         min.DistanceToPlayer() > Q.Range)
                                                {
                                                    Q.Cast();
                                                }
                                            }
                                    }

                                    if (minions.Length <= 2 && Me.HasBuff("JinxQ"))
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                            else
                            {
                                if (Me.HasBuff("JinxQ") && Q.Ready)
                                {
                                    Q.Cast();
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (target != null && target.IsValidTarget())
            {
                if (E.Ready && target.IsValidTarget(E.Range))
                {
                    switch (Args.Type)
                    {
                        case SpellType.Melee:
                            if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100) && !Args.HaveShield)
                            {
                                E.Cast(Me.ServerPosition);
                            }
                            break;
                        case SpellType.Dash:
                        case SpellType.SkillShot:
                        case SpellType.Targeted:
                            {
                                if (target.IsValidAutoRange() && !Args.HaveShield)
                                {
                                    var ePred = E.GetPrediction(target);
                                    E.Cast(ePred.UnitPosition);
                                }
                            }
                            break;
                    }
                }
            }

        }
    }
}