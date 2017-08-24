namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Damage.JSON;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
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

    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Draven : MyLogic
    {
        private static Dictionary<GameObject, int> AxeList { get; set; } = new Dictionary<GameObject, int>();

        private static Vector3 OrbwalkerPoint { get; set; } = Game.CursorPos;

        private static int AxeCount => (Me.HasBuff("dravenspinning") ? 1 : 0) + (Me.HasBuff("dravenspinningleft") ? 1 : 0) + AxeList.Count;

        private static int lastCatchTime { get; set; } = 0;

        public Draven()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q);

            W = new Aimtec.SDK.Spell(SpellSlot.W);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 950f);
            E.SetSkillshot(0.25f, 100f, 1400f, false, SkillshotType.Line);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 3000f);
            R.SetSkillshot(0.4f, 160f, 2000f, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("RSolo", "Use R | Solo Ks Mode");
            ComboOption.AddBool("RTeam", "Use R| Team Fight");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddE();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSliderBool("LaneClearECount", "Use E| Min Hit Count >= x", 4, 1, 7, true);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddE();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            AxeOption.AddMenu();
            AxeOption.AddList("CatchMode", "Catch Axe Mode: ", new[] { "All", "Only Combo", "Off" });
            AxeOption.AddSlider("CatchRange", "Catch Axe Range(Cursor center)", 2000, 180, 3000);
            AxeOption.AddSlider("CatchCount", "Max Axe Count <= x", 2, 1, 3);
            AxeOption.AddBool("CatchWSpeed", "Use W| When Axe Too Far");
            AxeOption.AddBool("NotCatchKS", "Dont Catch| If Target Can KillAble(1-3 AA)");
            AxeOption.AddBool("NotCatchTurret", "Dont Catch| If Axe Under Enemy Turret");
            AxeOption.AddSliderBool("NotCatchMoreEnemy", "Dont Catch| If Enemy Count >= x", 3, 1, 5, true);
            AxeOption.AddBool("CancelCatch", "Enabled Cancel Catch Axe Key");
            AxeOption.AddKey("CancelKey1", "Cancel Catch Key 1", KeyCode.G, KeybindType.Press);
            AxeOption.AddBool("CancelKey2", "Cancel Catch Key 2(is right click)");
            AxeOption.AddBool("CancelKey3", "Cancel Catch Key 3(is mouse scroll)", false);
            AxeOption.AddSeperator("Set Orbwalker->Misc->Hold Radius to 0 (will better)");

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddW();
            MiscOption.AddBool("W", "WSlow", "Auto W| When Player Have Debuff(Slow)");
            MiscOption.AddR();
            MiscOption.AddSlider("R", "GlobalRMin", "Global -> Cast R Min Range", 1000, 500, 2500);
            MiscOption.AddSlider("R", "GlobalRMax", "Global -> Cast R Max Range", 3000, 1500, 3500);
            MiscOption.AddKey("R", "SemiRKey", "Semi-manual R Key", KeyCode.T, KeybindType.Press);

            DrawOption.AddMenu();
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddBool("AxeRange", "Draw Catch Axe Range");
            DrawOption.AddBool("AxePosition", "Draw Axe Position");
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, false, true, true, true);

            AxeOption.GetKey("CancelKey1").OnValueChanged += OnCancelValueChange;

            Game.OnUpdate += OnUpdate;
            Game.OnWndProc += OnWndProc;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDestroy += OnDestroy;
            Gapcloser.OnGapcloser += OnGapcloser;
            Orbwalker.PreAttack += OnPreAttack;
            Orbwalker.PreMove += OnPreMove;
            Render.OnRender += OnRender;
        }

        private static void OnCancelValueChange(MenuComponent sender, ValueChangedArgs Args)
        {
            if (AxeOption.GetBool("CancelCatch").Enabled)
            {
                if (AxeOption.GetKey("CancelKey1").Enabled)
                {
                    if (Game.TickCount - lastCatchTime > 1800)
                    {
                        lastCatchTime = Game.TickCount;
                    }
                }
            }
        }

        private static void OnUpdate()
        {
            foreach (var sender in AxeList.Where(x => x.Key.IsDead || !x.Key.IsValid).Select(x => x.Key))
            {
                AxeList.Remove(sender);
            }

            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            R.Range = MiscOption.GetSlider("R", "GlobalRMax").Value;

            CatchAxeEvent();
            KillStealEvent();
            AutoUseEvent();

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
        }

        private static void CatchAxeEvent()
        {
            if (AxeList.Count == 0)
            {
                OrbwalkerPoint = Game.CursorPos;
                return;
            }

            if (AxeOption.GetList("CatchMode").Value == 2 ||
                AxeOption.GetList("CatchMode").Value == 1 && Orbwalker.Mode != OrbwalkingMode.Combo)
            {
                OrbwalkerPoint = Game.CursorPos;
                return;
            }

            var catchRange = AxeOption.GetSlider("CatchRange").Value;

            var bestAxe =
                AxeList.Where(x => !x.Key.IsDead && x.Key.IsValid && x.Key.Position.DistanceToMouse() <= catchRange)
                    .OrderBy(x => x.Value)
                    .ThenBy(x => x.Key.Position.DistanceToPlayer())
                    .ThenBy(x => x.Key.Position.DistanceToMouse())
                    .FirstOrDefault();

            if (bestAxe.Key != null)
            {
                if (AxeOption.GetBool("NotCatchTurret").Enabled &&
                    (Me.IsUnderEnemyTurret() && bestAxe.Key.Position.PointUnderEnemyTurret() ||
                     bestAxe.Key.Position.PointUnderEnemyTurret() && !Me.IsUnderEnemyTurret()))
                {
                    return;
                }

                if (AxeOption.GetSliderBool("NotCatchMoreEnemy").Enabled &&
                    (bestAxe.Key.Position.CountEnemyHeroesInRange(350) >=
                     AxeOption.GetSliderBool("NotCatchMoreEnemy").Value ||
                     GameObjects.EnemyHeroes.Count(x => x.Distance(bestAxe.Key.Position) < 350 && x.IsMelee) >=
                     AxeOption.GetSliderBool("NotCatchMoreEnemy").Value - 1))
                {
                    return;
                }

                if (AxeOption.GetBool("NotCatchKS").Enabled && Orbwalker.Mode == OrbwalkingMode.Combo)
                {
                    var target = MyTargetSelector.GetTarget(800, true);

                    if (target != null && target.IsValidTarget(800) &&
                        target.DistanceToPlayer() > target.BoundingRadius + Me.BoundingRadius + 200 &&
                        target.Health < Me.GetAutoAttackDamage(target) * 2.5 - 80)
                    {
                        OrbwalkerPoint = Game.CursorPos;
                        return;
                    }
                }

                if (AxeOption.GetBool("CatchWSpeed").Enabled && W.Ready &&
                    bestAxe.Key.Position.DistanceToPlayer() / Me.MoveSpeed * 1000 >= bestAxe.Value - Game.TickCount)
                {
                    W.Cast();
                }

                if (bestAxe.Key.Position.DistanceToPlayer() > 100)
                {
                    if (Game.TickCount - lastCatchTime > 1800)
                    {
                        if (Orbwalker.Mode != OrbwalkingMode.None)
                        {
                            OrbwalkerPoint = bestAxe.Key.Position;
                        }
                        else
                        {
                            Me.IssueOrder(OrderType.MoveTo, bestAxe.Key.Position);
                        }
                    }
                    else
                    {
                        if (Orbwalker.Mode != OrbwalkingMode.None)
                        {
                            OrbwalkerPoint = Game.CursorPos;
                        }
                    }
                }
                else
                {
                    if (Orbwalker.Mode != OrbwalkingMode.None)
                    {
                        OrbwalkerPoint = Game.CursorPos;
                    }
                }
            }
            else
            {
                if (Orbwalker.Mode != OrbwalkingMode.None)
                {
                    OrbwalkerPoint = Game.CursorPos;
                }
            }
        }

        private static void KillStealEvent()
        {
            if (KillStealOption.UseE && E.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            x.IsValidTarget(E.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.E) &&
                            !x.IsUnKillable()))
                {
                    if (target.IsValidTarget(E.Range))
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.HitChance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
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
                            x.IsValidTarget(R.Range) &&
                            KillStealOption.GetKillStealTarget(x.ChampionName) &&
                            x.Health <
                            Me.GetSpellDamage(x, SpellSlot.R) +
                            Me.GetSpellDamage(x, SpellSlot.R, DamageStage.SecondCast) && !x.IsUnKillable()))
                {
                    if (target.IsValidTarget(R.Range) && !target.IsValidTarget(MiscOption.GetSlider("R", "GlobalRMin").Value))
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

        private static void AutoUseEvent()
        {
            if (MiscOption.GetKey("R", "SemiRKey").Enabled && Me.SpellBook.GetSpell(SpellSlot.R).Level > 0 && R.Ready)
            {
                var target = MyTargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range) && !target.IsValidTarget(MiscOption.GetSlider("R", "GlobalRMin").Value))
                {
                    var rPred = R.GetPrediction(target);

                    if (rPred.HitChance >= HitChance.High)
                    {
                        R.Cast(rPred.CastPosition);
                    }
                }
            }

            if (MiscOption.GetBool("W", "WSlow").Enabled && W.Ready && Me.HasBuffOfType(BuffType.Slow))
            {
                W.Cast();
            }
        }

        private static void ComboEvent()
        {
            var target = MyTargetSelector.GetTarget(E.Range);

            if (target != null && target.IsValidTarget(E.Range))
            {
                if (ComboOption.UseW && W.Ready && !Me.HasBuff("dravenfurybuff"))
                {
                    if (target.DistanceToPlayer() >= 600)
                    {
                        W.Cast();
                    }
                    else
                    {
                        if (target.Health <
                            (AxeCount > 0
                                ? Me.GetSpellDamage(target, SpellSlot.Q) * 5
                                : Me.GetAutoAttackDamage(target) * 5))
                        {
                            W.Cast();
                        }
                    }
                }

                if (ComboOption.UseE && E.Ready)
                {
                    if (!target.IsValidAutoRange() ||
                        target.Health <
                        (AxeCount > 0
                            ? Me.GetSpellDamage(target, SpellSlot.Q) * 3
                            : Me.GetAutoAttackDamage(target) * 3) || Me.HealthPercent() < 40)
                    {
                        var ePred = E.GetPrediction(target);

                        if (ePred.HitChance >= HitChance.High)
                        {
                            E.Cast(ePred.CastPosition);
                        }
                    }
                }

                if (ComboOption.UseR && R.Ready && !target.IsValidTarget(MiscOption.GetSlider("R", "GlobalRMin").Value))
                {
                    if (ComboOption.GetBool("RSolo").Enabled)
                    {
                        if (target.Health <
                            Me.GetSpellDamage(target, SpellSlot.R) +
                            Me.GetSpellDamage(target, SpellSlot.R, DamageStage.SecondCast) +
                            (AxeCount > 0
                                ? Me.GetSpellDamage(target, SpellSlot.Q) * 2
                                : Me.GetAutoAttackDamage(target) * 2) +
                            (E.Ready ? Me.GetSpellDamage(target, SpellSlot.E) : 0) &&
                            target.Health >
                            (AxeCount > 0
                                ? Me.GetSpellDamage(target, SpellSlot.Q) * 3
                                : Me.GetAutoAttackDamage(target) * 3) &&
                            (Me.CountEnemyHeroesInRange(1000) == 1 ||
                             Me.CountEnemyHeroesInRange(1000) == 2 && Me.HealthPercent() >= 60))
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.HitChance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }
                    }

                    if (ComboOption.GetBool("RTeam").Enabled)
                    {
                        if (Me.CountAllyHeroesInRange(1000) <= 3 && Me.CountEnemyHeroesInRange(1000) <= 3)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.HitChance >= HitChance.Medium)
                            {
                                if (rPred.AoeTargetsHitCount >= 3)
                                {
                                    R.Cast(rPred.CastPosition);
                                }
                                else if (rPred.AoeTargetsHitCount >= 2)
                                {
                                    R.Cast(rPred.CastPosition);
                                }
                            }
                        }
                        else if (Me.CountAllyHeroesInRange(1000) <= 2 && Me.CountEnemyHeroesInRange(1000) <= 4)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.HitChance >= HitChance.Medium)
                            {
                                if (rPred.AoeTargetsHitCount >= 3)
                                {
                                    R.Cast(rPred.CastPosition);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void HarassEvent()
        {
            if (HarassOption.HasEnouguMana() && HarassOption.UseE && E.Ready)
            {
                var target = HarassOption.GetTarget(E.Range);

                if (target != null && target.IsValidTarget(E.Range))
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.HitChance >= HitChance.VeryHigh ||
                        ePred.HitChance >= HitChance.Medium && ePred.AoeTargetsHitCount > 1)
                    {
                        E.Cast(ePred.CastPosition);
                    }
                }
            }
        }

        private static void ClearEvent()
        {
            if (MyManaManager.SpellHarass && Me.CountEnemyHeroesInRange(E.Range) > 0)
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
                if (LaneClearOption.UseQ && Q.Ready && AxeCount < 2 && Orbwalker.CanAttack())
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(600) && x.IsMinion()).ToArray();

                    if (minions.Any() && minions.Length >= 2)
                    {
                        Q.Cast();
                    }
                }

                if (LaneClearOption.GetSliderBool("LaneClearECount").Enabled && E.Ready)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToArray();

                    if (minions.Any() && minions.Length >= LaneClearOption.GetSliderBool("LaneClearECount").Value)
                    {
                        var eFarm = E.GetSpellFarmPosition(minions);

                        if (eFarm.HitCount >= LaneClearOption.GetSliderBool("LaneClearECount").Value)
                        {
                            E.Cast(eFarm.CastPosition);
                        }
                    }
                }
            }
        }

        private static void JungleClearEvent()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMob()).ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (JungleClearOption.UseE && E.Ready && mob != null && mob.IsValidTarget(E.Range))
                    {
                        E.Cast(mob);
                    }

                    if (JungleClearOption.UseW && W.Ready && !Me.HasBuff("dravenfurybuff") && AxeCount > 0)
                    {
                        foreach (
                            var m in
                            mobs.Where(
                                x =>
                                    x.DistanceToPlayer() <= 600 && !x.Name.ToLower().Contains("mini") &&
                                    !x.Name.ToLower().Contains("crab") && x.MaxHealth > 1500 &&
                                    x.Health > Me.GetAutoAttackDamage(x) * 2))
                        {
                            if (m.IsValidTarget(600))
                            {
                                W.Cast();
                            }
                        }
                    }

                    if (JungleClearOption.UseQ && Q.Ready && AxeCount < 2 && Orbwalker.CanAttack())
                    {
                        if (mobs.Length >= 2)
                        {
                            Q.Cast();
                        }

                        if (mobs.Length == 1 && mob != null && mob.IsValidAutoRange() && mob.Health > Me.GetAutoAttackDamage(mob) * 5)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
        }

        private static void OnWndProc(WndProcEventArgs Args)
        {
            if (AxeOption.GetBool("CancelCatch").Enabled)
            {
                if (AxeOption.GetBool("CancelKey2").Enabled && (Args.Message == 516 || Args.Message == 517))
                {
                    if (Game.TickCount - lastCatchTime > 1800)
                    {
                        lastCatchTime = Game.TickCount;
                    }
                }

                if (AxeOption.GetBool("CancelKey3").Enabled && Args.Message == 0x20a)
                {
                    if (Game.TickCount - lastCatchTime > 1800)
                    {
                        lastCatchTime = Game.TickCount;
                    }
                }
            }
        }

        private static void OnCreate(GameObject sender)
        {
            if (sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                AxeList.Add(sender, Game.TickCount + 1800);
            }
        }

        private static void OnDestroy(GameObject sender)
        {
            if (AxeList.Any(o => o.Key.NetworkId == sender.NetworkId))
            {
                AxeList.Remove(sender);
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (E.Ready && target != null && target.IsValidTarget(E.Range))
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

        private static void OnPreAttack(object sender, PreAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0 || !Q.Ready)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.obj_AI_Hero:
                    {
                        var target = Args.Target as Obj_AI_Hero;

                        if (target != null && target.IsValidAutoRange())
                        {
                            if (Orbwalker.Mode == OrbwalkingMode.Combo)
                            {
                                if (ComboOption.UseQ && AxeOption.GetSlider("CatchCount").Value >= AxeCount)
                                {
                                    Q.Cast();
                                }
                            }
                            else if (Orbwalker.Mode == OrbwalkingMode.Mixed || Orbwalker.Mode == OrbwalkingMode.Laneclear &&
                                MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana() && HarassOption.GetHarassTargetEnabled(target.ChampionName))
                                {
                                    if (HarassOption.UseQ)
                                    {
                                        if (AxeCount < 2)
                                        {
                                            Q.Cast();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnPreMove(object sender, PreMoveEventArgs Args)
        {
            Args.MovePosition = OrbwalkerPoint;
        }

        private static void OnRender()
        {
            if (Me.IsDead || MenuGUI.IsChatOpen() || MenuGUI.IsShopOpen())
            {
                return;
            }

            if (DrawOption.GetBool("AxeRange").Enabled)
            {
                Render.Circle(Game.CursorPos, AxeOption.GetSlider("CatchRange").Value, 30, Color.FromArgb(0, 255, 161));
            }

            if (DrawOption.GetBool("AxePosition").Enabled)
            {
                foreach (var axe in AxeList.Where(x => !x.Key.IsDead && x.Key.IsValid).Select(x => x.Key))
                {
                    if (axe != null && axe.IsValid)
                    {
                        Render.Circle(axe.Position, 130, 30, Color.FromArgb(86, 0, 255));
                    }
                }
            }
        }
    }
}