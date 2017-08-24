namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Library;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;
    using System;
    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Vayne : MyLogic
    {
        public Vayne()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 300f);
            W = new Aimtec.SDK.Spell(SpellSlot.W);
            E = new Aimtec.SDK.Spell(SpellSlot.E, 650f) { Delay = 0.25f };
            R = new Aimtec.SDK.Spell(SpellSlot.R);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboAQA", "Use Q| After Attack");
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddSlider("ComboRCount", "Use R| Enemies Count >= x", 2, 1, 5);
            ComboOption.AddSlider("ComboRHp", "Use R| And Player HealthPercent <= x%", 40, 0, 100);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddBool("HarassQ2Passive", "Use Q| Only target have 2 Stack");
            HarassOption.AddE();
            HarassOption.AddBool("HarassE2Passive", "Use E| Only target have 2 Stack");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddE();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddSubMenu("Stealth", "Stealth Settings");
            MiscOption.AddList("Stealth", "HideSelect", "Enabled Mode: ", new[] { "Always Max Stealth Time", "Config", "Off" }, 1);
            MiscOption.AddBool("Stealth", "Hideinsolo", "Enabled Solo Stealth Config");
            MiscOption.AddSlider("Stealth", "Hideinsolomyhp", "When Player HealthPercent <= x%", 30);
            MiscOption.AddSlider("Stealth", "Hideinsolotargethp", "And Enemy HealthPercent => x%", 60);
            MiscOption.AddBool("Stealth", "Hideinmulti", "Enabled Team Fight Stealth Config");
            MiscOption.AddSlider("Stealth", "Hideinmultimyhp", "When Player HealthPercent <= x%", 70);
            MiscOption.AddSlider("Stealth", "HideinmultiallyCount", "And Allies Count <= x", 2, 0, 4);
            MiscOption.AddSlider("Stealth", "HideinmultienemyCount", "And Enemies Count >= x", 3, 2, 5);
            MiscOption.AddBasic();
            MiscOption.AddQ();
            MiscOption.AddBool("Q", "QCheck", "Auto Q| Safe Check");
            MiscOption.AddList("Q", "QTurret", "Auto Q| Disable Dash to Enemy Turret",
                new[] { "Only Dash Q", "Only After Attack Q", "Both", "Off" });
            MiscOption.AddBool("Q", "QMelee", "Auto Q| Anti Melee");
            MiscOption.AddE();
            MiscOption.AddBool("E", "AntiGapcloserE", "Auto E| Anti Gapcloser");
            MiscOption.AddR();
            MiscOption.AddBool("R", "AutoR", "Auto R");
            MiscOption.AddSlider("R", "AutoRCount", "Auto R| Enemies Count >= x", 3, 1, 5);
            MiscOption.AddSlider("R", "AutoRRange", "Auto R| Search Enemies Range", 600, 500, 1200);
            MiscOption.AddSetting("Forcus");
            MiscOption.AddBool("Forcus", "ForcusAttack", "Forcus Attack 2 Passive Target");

            DrawOption.AddMenu();
            DrawOption.AddE(E);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(false, true, true, false, true);

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

            if (R.GetBasicSpell().Level > 0 && R.Ready)
            {
                RLogic();
            }

            KillSteal();

            HideSettings(MiscOption.GetList("Stealth", "HideSelect").Value);

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

        private static void HideSettings(int settings)
        {
            if (Me.HasBuff("VayneInquisition") && Me.HasBuff("vaynetumblefade"))
            {
                switch (settings)
                {
                    case 0:
                        Orbwalker.AttackingEnabled = false;
                        break;
                    case 1:
                        if (MiscOption.GetBool("Stealth", "Hideinsolo").Enabled && Me.CountEnemyHeroesInRange(900) == 1)
                        {
                            SoloHideMode();
                        }

                        if (MiscOption.GetBool("Stealth", "Hideinmulti").Enabled && Me.CountEnemyHeroesInRange(900) > 1)
                        {
                            MultiHideMode();
                        }
                        break;
                    default:
                        Orbwalker.AttackingEnabled = true;
                        break;
                }
            }
            else
            {
                Orbwalker.AttackingEnabled = true;
            }
        }

        private static void MultiHideMode()
        {
            if (Me.HealthPercent() <= MiscOption.GetSlider("Stealth", "Hideinmultimyhp").Value &&
                Me.CountAllyHeroesInRange(900) <= MiscOption.GetSlider("Stealth", "HideinmultiallyCount").Value &&
                Me.CountEnemyHeroesInRange(900) >= MiscOption.GetSlider("Stealth", "HideinmultienemyCount").Value)
            {
                Orbwalker.AttackingEnabled = false;
            }
            else
            {
                Orbwalker.AttackingEnabled = true;
            }
        }

        private static void SoloHideMode()
        {
            var target = GameObjects.EnemyHeroes.First(x => x.IsValidTarget(900));

            if (target != null && target.IsValidTarget(900) &&
                Me.HealthPercent() <= MiscOption.GetSlider("Stealth", "Hideinsolomyhp").Value &&
                target.HealthPercent() >= MiscOption.GetSlider("Stealth", "Hideinsolotargethp").Value)
            {
                Orbwalker.AttackingEnabled = false;
            }
            else
            {
                Orbwalker.AttackingEnabled = true;
            }
        }

        private static void RLogic()
        {
            if (!R.Ready || Me.Mana < R.GetBasicSpell().Cost || R.GetBasicSpell().Level == 0)
            {
                return;
            }

            if (MiscOption.GetBool("R", "AutoR").Enabled && R.Ready &&
                GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(MiscOption.GetSlider("R", "AutoRRange").Value)) >=
                MiscOption.GetSlider("R", "AutoRCount").Value)
            {
                R.Cast();
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(E.Range) &&
                            x.Health < GetEDamage(x) + GetWDamage(x)))
                {
                    if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                    {
                        E.CastOnUnit(target);
                        return;
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseR && R.Ready &&
                GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(650)) >= ComboOption.GetSlider("ComboRCount").Value &&
                Me.HealthPercent() <= ComboOption.GetSlider("ComboRHp").Value)
            {
                R.Cast();
            }

            if (ComboOption.UseE && E.Ready)
            {
                ELogic();
            }

            if (ComboOption.UseQ && Q.Ready)
            {
                if (Me.HasBuff("VayneInquisition") && Me.CountEnemyHeroesInRange(1200) > 0 &&
                    Me.CountEnemyHeroesInRange(700) >= 2)
                {
                    var dashPos = GetDashQPos();

                    if (dashPos != Vector3.Zero)
                    {
                        if (Me.CanMoveMent())
                        {
                            Q.Cast(dashPos);
                        }
                    }
                }

                if (Me.CountEnemyHeroesInRange(Me.AttackRange) == 0 && Me.CountEnemyHeroesInRange(900) > 0)
                {
                    var target = TargetSelector.GetTarget(900);

                    if (target.IsValidTarget())
                    {
                        if (!target.IsValidAutoRange() &&
                            target.Position.DistanceToMouse() < target.Position.DistanceToPlayer())
                        {
                            var dashPos = GetDashQPos();

                            if (dashPos != Vector3.Zero)
                            {
                                if (Me.CanMoveMent())
                                {
                                    Q.Cast(dashPos);
                                }
                            }
                        }

                        if (ComboOption.UseE && E.Ready)
                        {
                            var dashPos = GetDashQPos();

                            if (dashPos != Vector3.Zero && CondemnCheck(dashPos, target))
                            {
                                if (Me.CanMoveMent())
                                {
                                    Q.Cast(dashPos);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseE && E.Ready)
                {
                    var target = HarassOption.GetTarget(E.Range);

                    if (target.IsValidTarget(E.Range))
                    {
                        if (HarassOption.GetBool("HarassE2Passive").Enabled)
                        {
                            if (target.IsValidTarget(E.Range) && Has2WStacks(target))
                            {
                                E.CastOnUnit(target);
                            }
                        }
                        else
                        {
                            if (CondemnCheck(Me.ServerPosition, target))
                            {
                                E.CastOnUnit(target);
                            }
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
                      GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius) && x.IsMinion())
                            .Where(m => m.Health <= Me.GetAutoAttackDamage(m) + Me.GetSpellDamage(m, SpellSlot.Q))
                            .ToArray();

                    if (minions.Any() && minions.Length > 1)
                    {
                        var minion = minions.OrderBy(m => m.Health).FirstOrDefault();
                        var afterQPosition = Me.ServerPosition.Extend(Game.CursorPos, Q.Range);

                        if (minion != null && afterQPosition.Distance(minion.ServerPosition) <= Me.AttackRange + Me.BoundingRadius)
                        {
                            Q.Cast(Game.CursorPos);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseE && E.Ready)
            {
                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMob()).ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault(
                        x =>
                            !x.Name.ToLower().Contains("mini") && !x.Name.ToLower().Contains("baron") &&
                            !x.Name.ToLower().Contains("dragon") && !x.Name.ToLower().Contains("crab") &&
                            !x.Name.ToLower().Contains("herald"));

                    if (mob != null && mob.IsValidTarget(E.Range))
                    {
                        if (CondemnCheck(Me.ServerPosition, mob))
                        {
                            E.CastOnUnit(mob);
                        }
                    }
                }
            }
        }


        private static void PreAttack(object sender, PreAttackEventArgs Args)
        {
            if (Orbwalker.Mode == OrbwalkingMode.Combo || Orbwalker.Mode == OrbwalkingMode.Mixed)
            {
                var ForcusTarget =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + x.BoundingRadius + 50) && Has2WStacks(x));

                if (MiscOption.GetBool("Forcus", "ForcusAttack").Enabled && ForcusTarget != null &&
                    ForcusTarget.IsValidTarget(Me.AttackRange + Me.BoundingRadius - ForcusTarget.BoundingRadius + 15))
                {
                    Orbwalker.ForceTarget(ForcusTarget);
                    LastForcusTime = Game.TickCount;
                }
                else
                {
                    Orbwalker.ForceTarget(null);
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
                            if (ComboOption.GetBool("ComboAQA").Enabled)
                            {
                                var target = Args.Target as Obj_AI_Hero;

                                if (target != null && !target.IsDead && Q.Ready)
                                {
                                    AfterQLogic(target);
                                }
                            }
                        }
                        else if (Orbwalker.Mode == OrbwalkingMode.Mixed || Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellHarass)
                        {
                            if (HarassOption.HasEnouguMana() && HarassOption.UseQ)
                            {
                                var target = Args.Target as Obj_AI_Hero;

                                if (target != null && !target.IsDead && Q.Ready &&
                                    HarassOption.GetHarassTargetEnabled(target.ChampionName))
                                {
                                    if (HarassOption.GetBool("HarassQ2Passive").Enabled && !Has2WStacks(target))
                                    {
                                        return;
                                    }

                                    AfterQLogic(target);
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
                                if (LaneClearOption.HasEnouguMana() && LaneClearOption.UseQ)
                                {
                                    var minions =
                                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius) && x.IsMinion())
                                            .Where(m => m.Health <= Me.GetAutoAttackDamage(m) + Me.GetSpellDamage(m, SpellSlot.Q))
                                            .ToArray();

                                    if (minions.Any() && minions.Length >= 1)
                                    {
                                        var minion = minions.OrderBy(m => m.Health).FirstOrDefault();
                                        var afterQPosition = Me.ServerPosition.Extend(Game.CursorPos, Q.Range);

                                        if (minion != null &&
                                            afterQPosition.Distance(minion.ServerPosition) <= Me.AttackRange + Me.BoundingRadius)
                                        {
                                            Q.Cast(Game.CursorPos);
                                        }
                                    }
                                }
                            }
                            else if (Args.Target.IsMob())
                            {
                                if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseQ)
                                {
                                    Q.Cast(Game.CursorPos);
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
                            if (LaneClearOption.HasEnouguMana(true) && LaneClearOption.UseQ)
                            {
                                if (Me.CountEnemyHeroesInRange(850) == 0)
                                {
                                    if (Me.CanMoveMent())
                                    {
                                        Q.Cast(Game.CursorPos);
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (target != null && target.IsValidTarget(E.Range))
            {
                switch (Args.Type)
                {
                    case SpellType.Melee:
                        {
                            if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100) &&
                                MiscOption.GetBool("Q", "QMelee").Enabled && Q.Ready)
                            {
                                Q.Cast(Me.ServerPosition.Extend(target.ServerPosition, -Q.Range));
                            }
                        }
                        break;
                    case SpellType.SkillShot:
                        {
                            if (MiscOption.GetBool("E", "AntiGapcloserE").Enabled && E.Ready && target.IsValidTarget(250) && !Args.HaveShield)
                            {
                                E.CastOnUnit(target);
                            }
                        }
                        break;
                    case SpellType.Dash:
                    case SpellType.Targeted:
                        {
                            if (MiscOption.GetBool("E", "AntiGapcloserE").Enabled && E.Ready && target.IsValidTarget(E.Range) && !Args.HaveShield)
                            {
                                E.CastOnUnit(target);
                            }
                        }
                        break;
                }
            }
        }

        private static Vector3 GetDashQPos()
        {
            var firstQPos = Me.ServerPosition.Extend(Game.CursorPos, Q.Range);
            var allPoint = MyExtraManager.GetCirclePoints(Q.Range).ToArray();

            foreach (var point in allPoint)
            {
                var mousecount = firstQPos.CountEnemyHeroesInRange(300);
                var count = point.CountEnemyHeroesInRange(300);

                if (!HaveEnemiesInRange(point))
                {
                    continue;
                }

                if (mousecount == count)
                {
                    if (point.DistanceToMouse() < firstQPos.DistanceToMouse())
                    {
                        firstQPos = point;
                    }
                }

                if (count < mousecount)
                {
                    firstQPos = point;
                }
            }

            if (MiscOption.GetList("Q", "QTurret").Value == 0 || MiscOption.GetList("Q", "QTurret").Value == 2)
            {
                if (firstQPos.PointUnderEnemyTurret())
                {
                    return Vector3.Zero;
                }
            }

            if (MiscOption.GetBool("Q", "QCheck").Enabled)
            {
                if (Me.CountEnemyHeroesInRange(Q.Range + Me.BoundingRadius - 30) <
                    firstQPos.CountEnemyHeroesInRange(Q.Range * 2 - Me.BoundingRadius))
                {
                    return Vector3.Zero;
                }

                if (firstQPos.CountEnemyHeroesInRange(Q.Range * 2 - Me.BoundingRadius) > 3)
                {
                    return Vector3.Zero;
                }
            }

            return HaveEnemiesInRange(firstQPos) ? firstQPos : Vector3.Zero;
        }

        private static void AfterQLogic(Obj_AI_Base target)
        {
            if (!Q.Ready || target == null || !target.IsValidTarget())
            {
                return;
            }

            var qPosition = Me.Position.Extend(Game.CursorPos, Q.Range);
            var targetDisQ = target.Position.Distance(qPosition);

            if (MiscOption.GetList("Q", "QTurret").Value == 1 || MiscOption.GetList("Q", "QTurret").Value == 2)
            {
                if (qPosition.PointUnderEnemyTurret())
                {
                    return;
                }
            }

            if (MiscOption.GetBool("Q", "QCheck").Enabled)
            {
                if (GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(300f, false, false, qPosition)) >= 3)
                {
                    return;
                }

                //Catilyn W
                if (ObjectManager
                        .Get<GameObject>()
                        .FirstOrDefault(
                            x =>
                                x != null && x.IsValid &&
                                x.Name.ToLower().Contains("yordletrap_idle_red.troy") &&
                                x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }

                //Jinx E
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid && x.IsEnemy && x.Name == "k" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }

                //Teemo R
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid && x.IsEnemy && x.Name == "Noxious Trap" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    return;
                }
            }

            if (targetDisQ <= Me.AttackRange + Me.BoundingRadius)
            {
                if (Me.CanMoveMent())
                {
                    Q.Cast(Game.CursorPos);
                }
            }
        }

        private static void ELogic()
        {
            if (!E.Ready)
            {
                return;
            }

            foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(E.Range)))
            {
                if (target.IsValidTarget(E.Range) && !target.HaveShiledBuff())
                {
                    if (CondemnCheck(Me.ServerPosition, target))
                    {
                        E.CastOnUnit(target);
                        return;
                    }
                }
            }
        }

        private static bool CondemnCheck(Vector3 startPosition, Obj_AI_Base target)
        {
            var targetPosition = target.ServerPosition;
            var predPosition = E.GetPrediction(target).UnitPosition;
            var pushDistance = startPosition == Me.ServerPosition ? 420 : 410;

            for (var i = 0; i <= pushDistance; i += 20)
            {
                var targetPoint = targetPosition.Extend(startPosition, -i);
                var predPoint = predPosition.Extend(startPosition, -i);

                if (predPoint.IsWall() && targetPoint.IsWall())
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HaveEnemiesInRange(Vector3 position)
        {
            return position.CountEnemyHeroesInRange(Me.AttackRange + Me.BoundingRadius) > 0;
        }

        private static bool Has2WStacks(Obj_AI_Base target)
        {
            return W.GetBasicSpell().Level > 0 && target.Buffs.Any(x => x.Name.ToLower() == "vaynesilvereddebuff" && x.Count == 2);
        }

        internal static double GetWDamage(Obj_AI_Base target)
        {
            if (target == null || target.IsDead || !target.IsValidTarget() || 
                !target.Buffs.Any(x => x.Name.ToLower() == "vaynesilvereddebuff" && x.Count == 2))
            {
                return 0;
            }

            var DMG = target.Type == GameObjectType.obj_AI_Minion
                ? Math.Min(200, new[] { 6, 7.5, 9, 10.5, 12 }[Me.GetSpell(SpellSlot.W).Level - 1] / 100 * target.MaxHealth)
                : new[] { 6, 7.5, 9, 10.5, 12 }[Me.GetSpell(SpellSlot.W).Level - 1] / 100 * target.MaxHealth;

            return Me.CalculateDamage(target, DamageType.True, DMG);
        }

        private static double GetEDamage(Obj_AI_Base target)
        {
            if (target == null || target.IsDead || !target.IsValidTarget())
            {
                return 0;
            }

            var DMG = new double[] { 45, 80, 115, 150, 185 }[Me.GetSpell(SpellSlot.E).Level - 1] + 0.5 * Me.FlatPhysicalDamageMod;

            return Me.CalculateDamage(target, DamageType.Magical, DMG);
        }
    }
}