namespace Flowers_Draven.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Damage.JSON;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.TargetSelector;

    using Flowers_Draven.MyBase;

    using System;
    using System.Drawing;
    using System.Linq;
    using Aimtec.SDK.Menu;
    using System.Collections.Generic;

    #endregion

    internal class MyEventManager : MyLogic
    {
        private static Dictionary<GameObject, int> AxeList { get; set; } = new Dictionary<GameObject, int>();

        private static Vector3 OrbwalkerPoint { get; set; } = Game.CursorPos;

        private static int AxeCount => (Me.HasBuff("dravenspinning") ? 1 : 0) + (Me.HasBuff("dravenspinningleft") ? 1 : 0) + AxeList.Count;

        internal static void Initializer()
        {
            try
            {
                Game.OnUpdate += OnUpdate;
                Game.OnWndProc += OnWndProc;
                GameObject.OnCreate += OnCreate;
                GameObject.OnDestroy += OnDestroy;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
                Orbwalker.PreAttack += OnPreAttack;
                Orbwalker.PreMove += OnPreMove;
                Render.OnRender += OnRender;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.Initializer." + ex);
            }
        }

        internal static void OnCancelValueChange(MenuComponent sender, ValueChangedArgs Args)
        {
            try
            {
                if (AxeMenu["FlowersDraven.AxeMenu.CancelCatch"].Enabled)
                {
                    if (AxeMenu["FlowersDraven.AxeMenu.CancelKey1"].As<MenuKeyBind>().Enabled)
                    {
                        if (Game.TickCount - lastCatchTime > 1800)
                        {
                            lastCatchTime = Game.TickCount;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCancelValueChange." + ex);
            }
        }

        private static void OnUpdate()
        {
            try
            {
                foreach (var sender in AxeList.Where(x => x.Key.IsDead || !x.Key.IsValid).Select(x => x.Key))
                {
                    AxeList.Remove(sender);
                }
             
                if (Me.IsDead || Me.IsRecalling())
                {
                    return;
                }

                CatchAxeEvent();
                KillStealEvent();
                AutoUseEvent();

                switch (Orbwalker.Mode)
                {
                    case OrbwalkingMode.Combo:
                        ComboEvent();
                        break;
                    case OrbwalkingMode.Mixed:
                        HarassEvent();
                        break;
                    case OrbwalkingMode.Laneclear:
                        ClearEvent();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnUpdate." + ex);
            }
        }

        private static void CatchAxeEvent()
        {
            try
            {
                if (AxeList.Count == 0)
                {
                    OrbwalkerPoint = Game.CursorPos;
                    return;
                }

                if (AxeMenu["FlowersDraven.AxeMenu.CatchMode"].Value == 2 ||
                    AxeMenu["FlowersDraven.AxeMenu.CatchMode"].Value == 1 && Orbwalker.Mode != OrbwalkingMode.Combo)
                {
                    OrbwalkerPoint = Game.CursorPos;
                    return;
                }

                var catchRange = AxeMenu["FlowersDraven.AxeMenu.CatchRange"].Value;

                var bestAxe =
                    AxeList.Where(x => !x.Key.IsDead && x.Key.IsValid && x.Key.Position.DistanceToMouse() <= catchRange)
                        .OrderBy(x => x.Value)
                        .ThenBy(x => x.Key.Position.DistanceToPlayer())
                        .ThenBy(x => x.Key.Position.DistanceToMouse())
                        .FirstOrDefault();

                if (bestAxe.Key != null)
                {
                    if (AxeMenu["FlowersDraven.AxeMenu.NotCatchTurret"].Enabled &&
                        (Me.IsUnderEnemyTurret() && bestAxe.Key.Position.PointUnderEnemyTurret() ||
                         bestAxe.Key.Position.PointUnderEnemyTurret() && !Me.IsUnderEnemyTurret()))
                    {
                        return;
                    }

                    if (AxeMenu["FlowersDraven.AxeMenu.NotCatchMoreEnemy"].As<MenuSliderBool>().Enabled &&
                        (bestAxe.Key.Position.CountEnemyHeroesInRange(350) >=
                         AxeMenu["FlowersDraven.AxeMenu.NotCatchMoreEnemy"].As<MenuSliderBool>().Value ||
                         GameObjects.EnemyHeroes.Count(x => x.Distance(bestAxe.Key.Position) < 350 && x.IsMelee) >=
                         AxeMenu["FlowersDraven.AxeMenu.NotCatchMoreEnemy"].As<MenuSliderBool>().Value - 1))
                    {
                        return;
                    }

                    if (AxeMenu["FlowersDraven.AxeMenu.NotCatchKS"].Enabled && Orbwalker.Mode == OrbwalkingMode.Combo)
                    {
                        var target = TargetSelector.GetTarget(800, true);

                        if (target != null && target.IsValidTarget(800) &&
                            target.DistanceToPlayer() > target.BoundingRadius + Me.BoundingRadius + 200 &&
                            target.Health < Me.GetAutoAttackDamage(target) * 2.5 - 80)
                        {
                            OrbwalkerPoint = Game.CursorPos;
                            return;
                        }
                    }

                    if (AxeMenu["FlowersDraven.AxeMenu.CatchWSpeed"].Enabled && W.Ready &&
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
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.CatchAxeEvent." + ex);
            }
        }

        private static void KillStealEvent()
        {
            try
            {
                if (KillStealMenu["FlowersDraven.KillStealMenu.E"].Enabled && E.Ready)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(E.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.E) &&
                                !x.IsUnKillable()))
                    {
                        if (target != null && target.IsValidTarget(E.Range))
                        {
                            var ePred = E.GetPrediction(target);

                            if (ePred.HitChance >= HitChance.High)
                            {
                                E.Cast(ePred.CastPosition);
                            }
                        }
                    }
                }

                if (KillStealMenu["FlowersDraven.KillStealMenu.R"].Enabled && R.Ready)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(R.Range) &&
                                KillStealMenu["FlowersDraven.KillStealMenu.UltKS_" + x.ChampionName].Enabled &&
                                x.Health <
                                Me.GetSpellDamage(x, SpellSlot.R) +
                                Me.GetSpellDamage(x, SpellSlot.R, DamageStage.SecondCast) && !x.IsUnKillable()))
                    {
                        if (target != null && target.IsValidTarget(R.Range))
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
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.KillStealEvent." + ex);
            }
        }

        private static void AutoUseEvent()
        {
            try
            {
                if (MiscMenu["FlowersDraven.MiscMenu.SemiRKey"].Enabled && Me.SpellBook.GetSpell(SpellSlot.R).Level > 0 && R.Ready)
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

                if (MiscMenu["FlowersDraven.MiscMenu.WSlow"].Enabled && W.Ready && Me.HasBuffOfType(BuffType.Slow))
                {
                    W.Cast();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.AutoUltEvent." + ex);
            }
        }

        private static void ComboEvent()
        {
            try
            {
                var target = TargetSelector.GetTarget(E.Range);

                if (target != null && target.IsValidTarget(E.Range))
                {
                    if (ComboMenu["FlowersDraven.ComboMenu.W"].Enabled && W.Ready && !Me.HasBuff("dravenfurybuff"))
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

                    if (ComboMenu["FlowersDraven.ComboMenu.E"].Enabled && E.Ready)
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

                    if (R.Ready)
                    {
                        if (ComboMenu["FlowersDraven.ComboMenu.RSolo"].Enabled)
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

                        if (ComboMenu["FlowersDraven.ComboMenu.RTeam"].Enabled)
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
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ComboEvent." + ex);
            }
        }

        private static void HarassEvent()
        {
            try
            {
                if (Me.ManaPercent() >= HarassMenu["FlowersDraven.HarassMenu.Mana"].Value)
                {
                    var target = TargetSelector.GetTarget(E.Range);

                    if (target != null && target.IsValidTarget(E.Range))
                    {
                        if (HarassMenu["FlowersDraven.HarassMenu.E"].Enabled && E.Ready)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.HarassEvent." + ex);
            }
        }

        private static void ClearEvent()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ClearEvent." + ex);
            }
        }

        private static void LaneClearEvent()
        {
            try
            {
                if (Me.ManaPercent() >= ClearMenu["FlowersDraven.ClearMenu.LaneClearMana"].Value && Q.Ready)
                {
                    if (ClearMenu["FlowersDraven.ClearMenu.LaneClearQ"].Enabled && Q.Ready && AxeCount < 2 && Orbwalker.CanAttack())
                    {
                        var minions =
                            GameObjects.EnemyMinions.Where(
                                    x => x.IsValidTarget(600) && x.MaxHealth > 5 && x.Team != GameObjectTeam.Neutral)
                                .ToArray();

                        if (minions.Any() && minions.Length >= 2)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.LaneClearEvent." + ex);
            }
        }

        private static void JungleClearEvent()
        {
            try
            {
                if (Me.ManaPercent() >= ClearMenu["FlowersDraven.ClearMenu.JungleClearMana"].Value)
                {
                    var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.MaxHealth > 5 && x.Team == GameObjectTeam.Neutral).ToArray();

                    if (mobs.Any())
                    {
                        var mob = mobs.FirstOrDefault();

                        if (ClearMenu["FlowersDraven.ClearMenu.JungleClearE"].Enabled && E.Ready && mob != null && mob.IsValidTarget(E.Range))
                        {
                            E.Cast(mob);
                        }

                        if (ClearMenu["FlowersDraven.ClearMenu.JungleClearW"].Enabled && W.Ready && !Me.HasBuff("dravenfurybuff") && AxeCount > 0)
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

                        if (ClearMenu["FlowersDraven.ClearMenu.JungleClearQ"].Enabled && Q.Ready && AxeCount < 2 && Orbwalker.CanAttack())
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
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.JungleClearEvent." + ex);
            }
        }

        private static void OnWndProc(WndProcEventArgs Args)
        {
            try
            {
                if (AxeMenu["FlowersDraven.AxeMenu.CancelCatch"].Enabled)
                {
                    if (AxeMenu["FlowersDraven.AxeMenu.CancelKey2"].Enabled && (Args.Message == 516 || Args.Message == 517))
                    {
                        if (Game.TickCount - lastCatchTime > 1800)
                        {
                            lastCatchTime = Game.TickCount;
                        }
                    }

                    if (AxeMenu["FlowersDraven.AxeMenu.CancelKey3"].Enabled && Args.Message == 0x20a)
                    {
                        if (Game.TickCount - lastCatchTime > 1800)
                        {
                            lastCatchTime = Game.TickCount;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnWndProc." + ex);
            }
        }

        private static void OnCreate(GameObject sender)
        {
            try
            {
                if (sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
                {
                    AxeList.Add(sender, Game.TickCount + 1800);
                }

                if (E.Ready)
                {
                    var Rengar = GameObjects.EnemyHeroes.Find(heros => heros.ChampionName.Equals("Rengar"));
                    var Khazix = GameObjects.EnemyHeroes.Find(heros => heros.ChampionName.Equals("Khazix"));

                    if (MiscMenu["FlowersDraven.MiscMenu.ERengar"].Enabled && Rengar != null)
                    {
                        if (sender.Name == "Rengar_LeapSound.troy" && sender.Position.Distance(Me.Position) < E.Range)
                        {
                            E.Cast(Rengar.Position);
                        }
                    }

                    if (MiscMenu["FlowersDraven.MiscMenu.EKhazix"].Enabled && Khazix != null)
                    {
                        if (sender.Name == "Khazix_Base_E_Tar.troy" && sender.Position.Distance(Me.Position) <= 300)
                        {
                            E.Cast(Khazix.Position);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCreate." + ex);
            }
        }

        private static void OnDestroy(GameObject sender)
        {
            try
            {
                if (AxeList.Any(o => o.Key.NetworkId == sender.NetworkId))
                {
                    AxeList.Remove(sender);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnDestroy." + ex);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            try
            {
                if (Me.IsDead || sender == null || !sender.IsEnemy || !MiscMenu["FlowersDraven.MiscMenu.EMelee"].Enabled || !E.Ready)
                {
                    return;
                }

                if (Args.Target != null && Args.Target.IsMe && sender.Type == GameObjectType.obj_AI_Hero &&
                    sender.IsMelee)
                {
                    E.Cast(sender.Position);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnProcessSpellCast." + ex);
            }
        }

        private static void OnPreAttack(object sender, PreAttackEventArgs Args)
        {
            try
            {
                if (Args.Target != null && !Args.Target.IsDead && Args.Target.Type == GameObjectType.obj_AI_Hero &&
                    Args.Target.IsValidTarget() && Args.Target.Health > 0 && Q.Ready)
                {
                    var target = Args.Target as Obj_AI_Hero;

                    if (target != null && target.IsValidAutoRange())
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            if (ComboMenu["FlowersDraven.ComboMenu.Q"].Enabled &&
                                AxeMenu["FlowersDraven.AxeMenu.CatchCount"].Value >= AxeCount)
                            {
                                Q.Cast();
                            }
                        }
                        else if (Orbwalker.Mode == OrbwalkingMode.Mixed || Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellHarass)
                        {
                            if (Me.ManaPercent() >= HarassMenu["FlowersDraven.HarassMenu.Mana"].Value)
                            {
                                if (HarassMenu["FlowersDraven.HarassMenu.Q"].Enabled)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnPreAttack." + ex);
            }
        }

        private static void OnPreMove(object sender, PreMoveEventArgs Args)
        {
            try
            {
                Args.MovePosition = OrbwalkerPoint;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnPreMove." + ex);
            }
        }

        private static void OnRender()
        {
            try
            {
                if (Me.IsDead || MenuGUI.IsChatOpen() || MenuGUI.IsShopOpen())
                {
                    return;
                }

                if (DrawMenu["FlowersDraven.DrawMenu.E"].Enabled && E.Ready)
                {
                    Render.Circle(Me.Position, E.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersDraven.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.Position, R.Range, 23, Color.FromArgb(251, 0, 133));
                }

                if (DrawMenu["FlowersDraven.DrawMenu.AxeRange"].Enabled)
                {
                    Render.Circle(Game.CursorPos, AxeMenu["FlowersDraven.AxeMenu.CatchRange"].Value, 23,
                        Color.FromArgb(0, 255, 161));
                }

                if (DrawMenu["FlowersDraven.DrawMenu.AxePosition"].Enabled)
                {
                    foreach (var axe in AxeList.Where(x => !x.Key.IsDead && x.Key.IsValid).Select(x => x.Key))
                    {
                        if (axe != null && axe.IsValid)
                        {
                            Render.Circle(axe.Position, 130, 23, Color.FromArgb(86, 0, 255));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }
    }
}