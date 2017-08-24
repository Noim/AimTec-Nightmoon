namespace Flowers_Yasuo.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Damage.JSON;
    using Aimtec.SDK.Events;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Yasuo.MyBase;

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    #endregion

    internal class MyEventManager : MyLogic
    {
        private static readonly int _menuX = (int)(Render.Width * 0.91f);
        private static readonly int _menuY = (int)(Render.Height * 0.04f);

        internal static void Initializer()
        {
            try
            {
                Game.OnUpdate += OnUpdate;
                BuffManager.OnAddBuff += OnAddBuff;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
                Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
                Orbwalker.PostAttack += OnPostAttack;
                Render.OnRender += OnRender;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.Initializer." + ex);
            }
        }

        private static void OnUpdate()
        {
            try
            {
                ResetToDefalut();

                if (Me.IsDead || Me.IsRecalling())
                {
                    return;
                }

                if (FleeMenu["FlowersYasuo.FleeMenu.FleeKey"].As<MenuKeyBind>().Enabled && Me.CanMoveMent())
                {
                    FleeEvent();
                }

                if (MiscMenu["FlowersYasuo.MiscMenu.EQFlashKey"].As<MenuKeyBind>().Enabled && Me.CanMoveMent())
                {
                    EQFlashEvent();
                }

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
                    case OrbwalkingMode.Lasthit:
                        LastHitEvent();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnUpdate." + ex);
            }
        }

        private static void ResetToDefalut()
        {
            try
            {
                IsMyDashing = isYasuoDashing;// || Me.IsDashing();

                if (Game.TickCount - YasuolastETime - (Game.Ping /2) > 500)
                {
                    isYasuoDashing = false;
                    YasuolastEPos = Vector3.Zero;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ResetToDefalut." + ex);
            }
        }

        private static void FleeEvent()
        {
            try
            {
                Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);

                if (IsMyDashing)
                {
                    if (FleeMenu["FlowersYasuo.FleeMenu.EQ"].Enabled && Q.Ready && !HaveQ3)
                    {
                        var qMinion =
                            GameObjects.EnemyMinions.FirstOrDefault(
                                x =>
                                    x.IsValidTarget(220, false, false, YasuolastEPos) && x.Health > 5 &&
                                    !x.Name.ToLower().Contains("plant"));

                        if (qMinion != null && qMinion.IsValidTarget())
                        {
                            Q.Cast(Me.ServerPosition);
                        }
                    }
                }
                else
                {
                    if (FleeMenu["FlowersYasuo.FleeMenu.Q3"].Enabled && HaveQ3 && Q3.Ready &&
                        GameObjects.EnemyHeroes.Any(x => x.IsValidTarget(Q3.Range)))
                    {
                        CastQ3();
                    }

                    if (FleeMenu["FlowersYasuo.FleeMenu.E"].Enabled && E.Ready)
                    {
                        var obj = MyExtraManager.GetNearObj();

                        if (obj != null && obj.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(obj);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.FleeEvent." + ex);
            }
        }

        private static void EQFlashEvent()
        {
            try
            {
                if (Orbwalker.Mode == OrbwalkingMode.None && FlashSlot != SpellSlot.Unknown && Flash.Ready)
                {
                    Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);

                    if (!HaveQ3)
                    {
                        if (Q.Ready)
                        {
                            var minion = GameObjects.EnemyMinions.FirstOrDefault(x => x.IsValidTarget(Q.Range) && x.MaxHealth > 5);

                            if (minion != null && minion.IsValidTarget(Q.Range))
                            {
                                Q.Cast(minion);
                            }
                        }
                    }
                    else if (HaveQ3 && Q3.Ready)
                    {
                        if (IsMyDashing && FlashSlot != SpellSlot.Unknown && Flash.Ready)
                        {
                            var bestPos =
                                MyExtraManager.FlashPoints().ToArray()
                                    .Where(x => GameObjects.EnemyHeroes.Count(a => a.IsValidTarget(600f, true, false, x)) > 0)
                                    .OrderByDescending(x => GameObjects.EnemyHeroes.Count(i => i.Distance(x) <= 220))
                                    .FirstOrDefault();

                            if (bestPos != Vector3.Zero && bestPos.CountEnemyHeroesInRange(220) > 0 && Q.Cast(Me.ServerPosition))
                            {
                                Aimtec.SDK.Util.DelayAction.Queue(10 + (Game.Ping / 2 - 5), () =>
                                {
                                    Flash.Cast(bestPos);
                                    YasuolastEQFlashTime = Game.TickCount;
                                });
                            }
                        }

                        if (E.Ready)
                        {
                            var allTargets = new List<Obj_AI_Base>();

                            allTargets.AddRange(GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.MaxHealth > 5));
                            allTargets.AddRange(GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(E.Range)));

                            if (allTargets.Any())
                            {
                                var eTarget =
                                    allTargets.Where(x => x.IsValidTarget(E.Range) && MyExtraManager.CanCastE(x))
                                        .OrderByDescending(
                                            x =>
                                                GameObjects.EnemyHeroes.Count(
                                                    t => t.IsValidTarget(600f, true, false, MyExtraManager.PosAfterE(x))))
                                        .FirstOrDefault();

                                if (eTarget != null)
                                {
                                    E.CastOnUnit(eTarget);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.EQFlashEvent." + ex);
            }
        }

        private static void KillStealEvent()
        {
            try
            {
                if (IsMyDashing)
                {
                    return;
                }

                if (KillStealMenu["FlowersYasuo.KillStealMenu.Q"].Enabled && Q.Ready && !HaveQ3)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                    {
                        if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                        {
                            CastQ(target);
                            return;
                        }
                    }
                }

                if (KillStealMenu["FlowersYasuo.KillStealMenu.Q3"].Enabled && Q3.Ready && HaveQ3)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(Q3.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                    {
                        if (target.IsValidTarget(Q3.Range) && !target.IsUnKillable())
                        {
                            Q3.Cast(target);
                            return;
                        }
                    }
                }

                if (KillStealMenu["FlowersYasuo.KillStealMenu.E"].Enabled && E.Ready)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(E.Range) &&
                                x.Health <
                                Me.GetSpellDamage(x, SpellSlot.E) + Me.GetSpellDamage(x, SpellSlot.E, DamageStage.Buff)))
                    {
                        if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                        {
                            E.CastOnUnit(target);
                            return;
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
                if (Q.Ready)
                {
                    if (HarassMenu["FlowersYasuo.HarassMenu.AutoQ"].As<MenuKeyBind>().Enabled)
                    {
                        AutoQHarassEvent();
                    }

                    if (MiscMenu["FlowersYasuo.MiscMenu.StackQ"].As<MenuKeyBind>().Enabled)
                    {
                        StackQEvent();
                    }
                }

                if (MiscMenu["FlowersYasuo.MiscMenu.AutoR"].Enabled && R.Ready)
                {
                    if (Game.TickCount - YasuolastEQFlashTime < 800)
                    {
                        var enemiesKnockedUp =
                            GameObjects.EnemyHeroes
                                .Where(x => x.IsValidTarget(R.Range, true))
                                .Where(x => !x.IsInvulnerable)
                                .Where(x => x.HasBuffOfType(BuffType.Knockup));
                        var enemies = enemiesKnockedUp as IList<Obj_AI_Hero> ?? enemiesKnockedUp.ToList();

                        if (enemies.Count > 0)
                        {
                            R.CastOnUnit(enemies.FirstOrDefault());
                        }
                    }
                    else
                    {
                        var enemiesKnockedUp =
                            GameObjects.EnemyHeroes
                                .Where(x => x.IsValidTarget(R.Range, true))
                                .Where(x => !x.IsInvulnerable)
                                .Where(x => x.HasBuffOfType(BuffType.Knockup));
                        var enemies = enemiesKnockedUp as IList<Obj_AI_Hero> ?? enemiesKnockedUp.ToList();
                        var allallies =
                            GameObjects.AllyHeroes
                                .Where(x => x.IsValidTarget(R.Range, true) && !x.IsMe)
                                .Where(x => !x.IsInvulnerable);
                        var allies = allallies as IList<Obj_AI_Hero> ?? allallies.ToList();

                        if (enemies.Count >= MiscMenu["FlowersYasuo.MiscMenu.AutoRCount"].Value &&
                            Me.HealthPercent() >= MiscMenu["FlowersYasuo.MiscMenu.AutoRHP"].Value &&
                            (MiscMenu["FlowersYasuo.MiscMenu.AutoRAlly"].Value == 0 || 
                            allies.Count >= MiscMenu["FlowersYasuo.MiscMenu.AutoRAlly"].Value))
                        {
                            R.CastOnUnit(enemies.FirstOrDefault());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.AutoUseEvent." + ex);
            }
        }

        private static void AutoQHarassEvent()
        {
            try
            {
                if (IsMyDashing || Me.CountEnemyHeroesInRange(Q.Range) == 0 || Me.IsUnderEnemyTurret() ||
                    Orbwalker.Mode == OrbwalkingMode.Combo || Orbwalker.Mode == OrbwalkingMode.Mixed ||
                    FleeMenu["FlowersYasuo.FleeMenu.FleeKey"].As<MenuKeyBind>().Enabled)
                {
                    return;
                }

                if (HarassMenu["FlowersYasuo.HarassMenu.AutoQ3"].Enabled && HaveQ3)
                {
                    CastQ3();
                }
                else if (!HaveQ3)
                {
                    var target = TargetSelector.GetTarget(Q.Range);

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        CastQ(target);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.AutoQHarassEvent." + ex);
            }
        }

        private static void StackQEvent()
        {
            try
            {
                if (IsMyDashing || HaveQ3 || Me.CountEnemyHeroesInRange(Q.Range) > 0 || Me.IsUnderEnemyTurret() ||
                    Orbwalker.Mode != OrbwalkingMode.None ||
                    FleeMenu["FlowersYasuo.FleeMenu.FleeKey"].As<MenuKeyBind>().Enabled)
                {
                    return;
                }

                var minion = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.MaxHealth > 5).FirstOrDefault(x => x.IsValidTarget(Q.Range));

                if (minion != null && minion.IsValidTarget(Q.Range))
                {
                    Q.Cast(minion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.StackQEvent." + ex);
            }
        }

        private static void ComboEvent()
        {
            try
            {
                var target = TargetSelector.GetTarget(1200);

                if (target != null && target.IsValidTarget(1200))
                {
                    if (ComboMenu["FlowersYasuo.ComboMenu.Ignite"].Enabled && IgniteSlot != SpellSlot.Unknown &&
                        Ignite.Ready && target.IsValidTarget(600) && 
                        (target.HealthPercent() <= 25 || target.Health <= Me.GetIgniteDamage(target)))
                    {
                        Ignite.CastOnUnit(target);
                    }

                    if (ComboMenu["FlowersYasuo.ComboMenu.EQFlash"].As<MenuKeyBind>().Enabled)
                    {
                        ComboEQFlashEvent(target);
                    }

                    if (ComboMenu["FlowersYasuo.ComboMenu.R"].As<MenuKeyBind>().Enabled && R.Ready)
                    {
                        foreach (var rTarget in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(1200) && MyExtraManager.CanCastR(x)))
                        {
                            if (ComboMenu["FlowersYasuo.ComboMenu.RHitCount"].As<MenuSliderBool>().Enabled)
                            {
                                if (rTarget.IsValidTarget(1200))
                                {
                                    var enemiesKnockedUp =
                                        GameObjects.EnemyHeroes
                                            .Where(x => x.IsValidTarget(R.Range))
                                            .Where(MyExtraManager.CanCastR);

                                    var enemiesKnocked = enemiesKnockedUp as IList<Obj_AI_Hero> ?? enemiesKnockedUp.ToList();

                                    if (enemiesKnocked.Count >= ComboMenu["FlowersYasuo.ComboMenu.RHitCount"].As<MenuSliderBool>().Value)
                                    {
                                        R.CastOnUnit(rTarget);
                                    }
                                }
                            }

                            if (ComboMenu["FlowersYasuo.ComboMenu.RTargetHP"].As<MenuSliderBool>().Enabled)
                            {
                                if (rTarget.IsValidTarget(R.Range))
                                {
                                    if (ComboMenu["FlowersYasuo.ComboMenu.RTargetFor" + rTarget.ChampionName].Enabled &&
                                        MyExtraManager.CanCastR(rTarget) &&
                                        rTarget.HealthPercent() <=
                                        ComboMenu["FlowersYasuo.ComboMenu.RTargetHP"].As<MenuSliderBool>().Value)
                                    {
                                        R.CastOnUnit(rTarget);
                                    }
                                }
                            }
                        }
                    }

                    if (E.Ready)
                    {
                        if (ComboMenu["FlowersYasuo.ComboMenu.E"].Enabled)
                        {
                            var dmg = Me.GetSpellDamage(target, SpellSlot.Q) * 2 + Me.GetSpellDamage(target, SpellSlot.E) +
                                      Me.GetAutoAttackDamage(target) * 2 +
                                      (R.Ready
                                          ? Me.GetSpellDamage(target, SpellSlot.R)
                                          : Me.GetSpellDamage(target, SpellSlot.Q));

                            if (target.DistanceToPlayer() >= Me.BoundingRadius + Me.AttackRange + 65 &&
                                (dmg >= target.Health || HaveQ3 && Q.Ready) && MyExtraManager.CanCastE(target) &&
                                (ComboMenu["FlowersYasuo.ComboMenu.ETurret"].Enabled ||
                                 !MyExtraManager.UnderTower(MyExtraManager.PosAfterE(target))))
                            {
                                E.CastOnUnit(target);
                            }
                        }

                        if (ComboMenu["FlowersYasuo.ComboMenu.EGapcloser"].Enabled)
                        {
                            if (!target.IsValidAutoRange())
                            {
                                if (ComboMenu["FlowersYasuo.ComboMenu.EQGapcloserMode"].As<MenuList>().Value == 0)
                                {
                                    MyExtraManager.EGapTarget(target, ComboMenu["FlowersYasuo.ComboMenu.ETurret"].Enabled, Me.BoundingRadius + Me.AttackRange + target.BoundingRadius - 50, HaveQ3);
                                }
                                else
                                {
                                    MyExtraManager.EGapMouse(target, ComboMenu["FlowersYasuo.ComboMenu.ETurret"].Enabled, Me.BoundingRadius + Me.AttackRange + target.BoundingRadius - 50, HaveQ3);
                                }
                            }
                        }
                    }

                    if (Q.Ready)
                    {
                        if (IsMyDashing)
                        {
                            if (ComboMenu["FlowersYasuo.ComboMenu.EQ"].Enabled && !HaveQ3)
                            {
                                if (ObjectManager.Get<Obj_AI_Base>()
                                        .Any(x => x.IsValidTarget(220, false, false, YasuolastEPos)) && Me.Distance(YasuolastEPos) <= 250)
                                {
                                    Q.Cast(Me.ServerPosition);
                                }
                            }

                            if (ComboMenu["FlowersYasuo.ComboMenu.EQ3"].Enabled && HaveQ3)
                            {
                                if (YasuolastEPos.CountEnemyHeroesInRange(220) > 0 && Me.Distance(YasuolastEPos) <= 250)
                                {
                                    Q.Cast(Me.ServerPosition);
                                }
                            }
                        }
                        else
                        {
                            if (ComboMenu["FlowersYasuo.ComboMenu.Q"].Enabled && !HaveQ3 &&
                                target.IsValidTarget(Q.Range))
                            {
                                CastQ(target);
                            }

                            if (ComboMenu["FlowersYasuo.ComboMenu.Q3"].Enabled && HaveQ3 &&
                                target.IsValidTarget(Q3.Range))
                            {
                                CastQ3();
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

        private static void ComboEQFlashEvent(Obj_AI_Hero target)
        {
            try
            {
                if (FlashSlot == SpellSlot.Unknown || !Flash.Ready || !R.Ready)
                {
                    return;
                }

                if (ComboMenu["FlowersYasuo.ComboMenu.EQFlashKS"].Enabled &&
                    GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(1200) && x.NetworkId != target.NetworkId) <= 2&&
                    GameObjects.AllyHeroes.Count(x => x.IsValidTarget(1200, true) && x.NetworkId != Me.NetworkId) <= 1)
                {
                    if (target.Health + target.HPRegenRate * 2 <
                        Me.GetSpellDamage(target, SpellSlot.Q) +
                        (MyExtraManager.CanCastE(target) ? Me.GetSpellDamage(target, SpellSlot.E) : 0) +
                        Me.GetAutoAttackDamage(target) * 2 + Me.GetSpellDamage(target, SpellSlot.R))
                    {
                        var bestPos = MyExtraManager.FlashPoints().ToArray().FirstOrDefault(x => target.Distance(x) <= 220);

                        if (bestPos != Vector3.Zero && bestPos.CountEnemyHeroesInRange(220) > 0 && Q.Cast(Me.ServerPosition))
                        { 
                            Aimtec.SDK.Util.DelayAction.Queue(10 + (Game.Ping / 2 - 5),
                                () =>
                                {
                                    Flash.Cast(bestPos);
                                    YasuolastEQFlashTime = Game.TickCount;
                                });
                        }
                    }
                }

                if (ComboMenu["FlowersYasuo.ComboMenu.EQFlashCount"].As<MenuSliderBool>().Enabled &&
                    GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(1200)) >=
                    ComboMenu["FlowersYasuo.ComboMenu.EQFlashCount"].As<MenuSliderBool>().Value &&
                    GameObjects.AllyHeroes.Count(x => x.IsValidTarget(1200, true) && x.NetworkId != Me.NetworkId) >=
                    ComboMenu["FlowersYasuo.ComboMenu.EQFlashCount"].As<MenuSliderBool>().Value - 1)
                {
                    var bestPos =
                        MyExtraManager.FlashPoints().ToArray()
                            .Where(
                                x =>
                                    GameObjects.EnemyHeroes.Count(a => a.IsValidTarget(600f, true, true, x)) >=
                                    ComboMenu["FlowersYasuo.ComboMenu.EQFlashCount"].As<MenuSliderBool>().Value)
                            .OrderByDescending(x => GameObjects.EnemyHeroes.Count(i => i.Distance(x) <= 220))
                            .FirstOrDefault();

                    if (bestPos != Vector3.Zero &&
                        bestPos.CountEnemyHeroesInRange(220) >=
                        ComboMenu["FlowersYasuo.ComboMenu.EQFlashCount"].As<MenuSliderBool>().Value && Q.Cast(Me.ServerPosition))
                    {
                        Aimtec.SDK.Util.DelayAction.Queue(10 + (Game.Ping / 2 - 5),
                            () =>
                            {
                                Flash.Cast(bestPos);
                                YasuolastEQFlashTime = Game.TickCount;
                            });
                
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ComboEQFlashEvent." + ex);
            }
        }

        private static void HarassEvent()
        {
            try
            {
                if (Me.IsUnderEnemyTurret())
                {
                    return;
                }

                if (HarassMenu["FlowersYasuo.HarassMenu.Q"].Enabled && Q.Ready && !HaveQ3)
                {
                    var target = TargetSelector.GetTarget(Q.Range);

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        CastQ(target);
                    }
                }

                if (HarassMenu["FlowersYasuo.HarassMenu.Q3"].Enabled && Q3.Ready && HaveQ3)
                {
                    CastQ3();
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
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ClearEvent." + ex);
            }
        }

        private static void LaneClearEvent()
        {
            try
            {
                if (ClearMenu["FlowersYasuo.ClearMenu.LaneClearTurret"].Enabled && Me.IsUnderEnemyTurret())
                {
                    return;
                }

                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q3.Range) && x.Health > 5).ToArray();

                if (minions.Any())
                {
                    if (ClearMenu["FlowersYasuo.ClearMenu.LaneClearE"].Enabled && E.Ready)
                    {
                        foreach (
                            var minion in
                            minions.Where(
                                x =>
                                    x.DistanceToPlayer() <= E.Range && MyExtraManager.CanCastE(x) &&
                                    x.Health <=
                                    (Q.Ready
                                        ? Me.GetSpellDamage(x, SpellSlot.Q) + Me.GetSpellDamage(x, SpellSlot.E)
                                        : Me.GetSpellDamage(x, SpellSlot.E))))
                        {
                            if (minion != null && minion.IsValidTarget(E.Range) && 
                                (!ClearMenu["FlowersYasuo.ClearMenu.LaneClearTurret"].Enabled && 
                                !MyExtraManager.UnderTower(MyExtraManager.PosAfterE(minion)) ||
                                MyExtraManager.UnderTower(MyExtraManager.PosAfterE(minion))) && 
                                MyExtraManager.PosAfterE(minion).IsSafePosition())
                            {
                                E.CastOnUnit(minion);
                            }
                        }
                    }

                    if (IsMyDashing)
                    {
                        if (ClearMenu["FlowersYasuo.ClearMenu.LaneClearEQ"].Enabled && Q.Ready && !HaveQ3)
                        {
                            if (minions.Count(x => x.Health > 0 && x.IsValidTarget(220, false, false, YasuolastEPos)) >= 1)
                            {
                                Q.Cast(Me.ServerPosition);
                            }
                        }
                    }
                    else
                    {
                        foreach (var minion in minions.Where(x => x.IsValidTarget(Q3.Range)))
                        {
                            if (minion != null && minion.Health > 0)
                            {
                                if (ClearMenu["FlowersYasuo.ClearMenu.LaneClearQ"].Enabled && Q.Ready && !HaveQ3 && minion.IsValidTarget(Q.Range))
                                {
                                    Q.Cast(minion);
                                }

                                if (ClearMenu["FlowersYasuo.ClearMenu.LaneClearQ3"].Enabled && Q3.Ready && HaveQ3 && minion.IsValidTarget(Q3.Range))
                                {
                                    Q3.Cast(minion);
                                }
                            }
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
                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q3.Range) && x.Health > 5 && x.Team == GameObjectTeam.Neutral).ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.OrderBy(x => x.MaxHealth).FirstOrDefault(x => x.IsValidTarget(E.Range) && MyExtraManager.CanCastE(x));

                    if (mob != null)
                    {
                        if (ClearMenu["FlowersYasuo.ClearMenu.JungleClearE"].Enabled && E.Ready && 
                            mob.IsValidTarget(E.Range) && MyExtraManager.CanCastE(mob))
                        {
                            E.CastOnUnit(mob);
                        }

                        if (ClearMenu["FlowersYasuo.ClearMenu.JungleClearQ"].Enabled && Q.Ready && !HaveQ3 && mob.IsValidTarget(Q.Range))
                        {
                            Q.Cast(mob);
                        }

                        if (ClearMenu["FlowersYasuo.ClearMenu.JungleClearQ3"].Enabled && Q3.Ready && HaveQ3 &&
                            mob.IsValidTarget(Q3.Range))
                        {
                            Q3.Cast(mob);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.JungleClearEvent." + ex);
            }
        }

        private static void LastHitEvent()
        {
            try
            {
                if (IsMyDashing)
                {
                    return;
                }

                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q3.Range) && x.Health > 5).ToArray();

                if (minions.Any())
                {
                    foreach (var minion in minions)
                    {
                        if (LastHitMenu["FlowersYasuo.LastHitMenu.Q"].Enabled && !HaveQ3 && Q.Ready)
                        {
                            if (minion.IsValidTarget(Q.Range)  && minion.Health < Me.GetSpellDamage(minion, SpellSlot.Q))
                            {
                                Q.Cast(minion);
                            }
                        }

                        if (LastHitMenu["FlowersYasuo.LastHitMenu.Q3"].Enabled && HaveQ3 && Q.Ready)
                        {
                            if (minion.IsValidTarget(Q3.Range) && HaveQ3 &&
                                minion.Health < Me.GetSpellDamage(minion, SpellSlot.Q))
                            {
                                Q3.Cast(minion);
                            }
                        }

                        if (LastHitMenu["FlowersYasuo.LastHitMenu.E"].Enabled && E.Ready)
                        {
                            if (minion.IsValidTarget(E.Range) &&
                                minion.Health <
                                Me.GetSpellDamage(minion, SpellSlot.E) +
                                Me.GetSpellDamage(minion, SpellSlot.E, DamageStage.Buff) &&
                                MyExtraManager.CanCastE(minion) &&
                                !MyExtraManager.UnderTower(MyExtraManager.PosAfterE(minion)) &&
                                MyExtraManager.PosAfterE(minion).IsSafePosition())
                            {
                                E.CastOnUnit(minion);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.LastHitEvent." + ex);
            }
        }

        private static void OnAddBuff(Obj_AI_Base sender, Buff buff)
        {
            try
            {
                switch (buff.Name.ToLower())
                {
                    case "yasuodashwrapper":
                        if (buff.Caster.IsMe)
                        {
                            YasuolastETime = Game.TickCount;
                            isYasuoDashing = true;
                            Aimtec.SDK.Util.DelayAction.Queue(500 + (Game.Ping / 2), () => { isYasuoDashing = false; });
                        }
                        break;
                    case "yasuoqcombosoundmiss":
                    case "yasuoeqcombosoundhit":
                        if (sender.IsMe)
                        {
                            Orbwalker.AttackingEnabled = false;
                            Aimtec.SDK.Util.DelayAction.Queue(250 + (Game.Ping /2 + 10), () =>
                            {
                                Orbwalker.ResetAutoAttackTimer();
                                Me.IssueOrder(OrderType.MoveTo, Me.Position.Extend(Game.CursorPos, 50));
                                Orbwalker.AttackingEnabled = true;
                            });
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnAddBuff." + ex);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            try
            {
                if (sender.IsMe && Args.Target != null && Args.Target.IsEnemy)
                {
                    if (Args.SpellData.Name == "YasuoDashWrapper")
                    {
                        var target = Args.Target as Obj_AI_Base;

                        if (target != null && target.IsValidTarget())
                        {
                            YasuolastEPos = MyExtraManager.PosAfterE(target);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnProcessSpellCast." + ex);
            }
        }

        private static void OnPlayAnimation(Obj_AI_Base sender, Obj_AI_BasePlayAnimationEventArgs Args)
        {
            try
            {
                if (sender.IsMe)
                {
                    if (Args.Animation == "Spell3")
                    {
                        YasuolastETime = Game.TickCount;
                        isYasuoDashing = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnPlayAnimation." + ex);
            }
        }

        private static void OnPostAttack(object sender, PostAttackEventArgs Args)
        {
            try
            {
                if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || HaveQ3 || !Q.Ready)
                {
                    return;
                }

                switch (Orbwalker.Mode)
                {
                    case OrbwalkingMode.Combo:
                        if (ComboMenu["FlowersYasuo.ComboMenu.Q"].Enabled)
                        {
                            var target = Args.Target as Obj_AI_Hero;

                            if (target != null && target.IsValidTarget(Q.Range) && target.Health > 0)
                            {
                                Q.Cast(target);
                            }
                        }
                        break;
                    case OrbwalkingMode.Laneclear:
                        if (ClearMenu["FlowersYasuo.ClearMenu.JungleClearQ"].Enabled && MyManaManager.SpellFarm)
                        {
                            var mob = Args.Target as Obj_AI_Minion;

                            if (mob != null && mob.Team == GameObjectTeam.Neutral && mob.IsValidTarget(Q.Range) && mob.Health > 0)
                            {
                                Q.Cast(mob);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnPostAttack." + ex);
            }
        }

        private static void OnRender()
        {
            try
            {
                if (DrawMenu["FlowersYasuo.DrawMenu.Q"].Enabled && Q.Ready && !HaveQ3)
                {
                    Render.Circle(Me.Position, Q.Range, 23, Color.FromArgb(86, 0, 255));
                }

                if (DrawMenu["FlowersYasuo.DrawMenu.Q3"].Enabled && Q3.Ready && HaveQ3)
                {
                    Render.Circle(Me.Position, Q3.Range, 23, Color.FromArgb(0, 255, 161));
                }

                if (DrawMenu["FlowersYasuo.DrawMenu.E"].Enabled && E.Ready)
                {
                    Render.Circle(Me.Position, E.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersYasuo.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.Position, R.Range, 23, Color.FromArgb(251, 0, 133));
                }

                if (DrawMenu["FlowersYasuo.DrawMenu.AutoHarass"].Enabled)
                {
                    Render.Text(_menuX + 10, _menuY + 25, Color.Orange,
                        "Auto Q(" + HarassMenu["FlowersYasuo.HarassMenu.AutoQ"].As<MenuKeyBind>().Key + "): " +
                        (HarassMenu["FlowersYasuo.HarassMenu.AutoQ"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }

                if (DrawMenu["FlowersYasuo.DrawMenu.StackQ"].Enabled)
                {
                    Render.Text(_menuX + 10, _menuY + 45, Color.Orange,
                        "Stack Q(" + MiscMenu["FlowersYasuo.MiscMenu.StackQ"].As<MenuKeyBind>().Key + "): " +
                        (MiscMenu["FlowersYasuo.MiscMenu.StackQ"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }

                if (DrawMenu["FlowersYasuo.DrawMenu.ComboR"].Enabled)
                {
                    Render.Text(_menuX + 10, _menuY + 65, Color.Orange,
                        "Combo R(" + ComboMenu["FlowersYasuo.ComboMenu.R"].As<MenuKeyBind>().Key + "): " +
                        (ComboMenu["FlowersYasuo.ComboMenu.R"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }

                if (DrawMenu["FlowersYasuo.DrawMenu.ComboEQFlash"].Enabled)
                {
                    Render.Text(_menuX + 10, _menuY + 85, Color.Orange,
                        "Flash Combo(" + ComboMenu["FlowersYasuo.ComboMenu.EQFlash"].As<MenuKeyBind>().Key + "): " +
                        (ComboMenu["FlowersYasuo.ComboMenu.EQFlash"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }

                //Render.Text(_menuX + 10, _menuY + 105, Color.Orange, "IsMyDashing: " + (IsMyDashing ? "On" : "Off"));
                //Render.Text(_menuX + 10, _menuY + 125, Color.Orange, "IsYasuoDashing: " + (isYasuoDashing ? "On" : "Off"));
                //Render.Text(_menuX + 10, _menuY + 145, Color.Orange, "HaveQ3: " + (HaveQ3 ? "On" : "Off"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }

        internal static void CastQ3() //Made by Brian(Valve Sharp)
        {
            try
            {
                var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(1200)).ToArray();
                var castPos = Vector3.Zero;

                if (!targets.Any())
                {
                    return;
                }

                foreach (var pred in
                    targets.Select(i => Q3.GetPrediction(i))
                        .Where(
                            i => (int)i.HitChance >= 6 ||
                                 (int)i.HitChance >= 5 && i.AoeTargetsHitCount > 1)
                        .OrderByDescending(i => i.AoeTargetsHitCount))
                {
                    castPos = pred.CastPosition;
                    break;
                }

                if (castPos != Vector3.Zero)
                {
                    Q3.Cast(castPos);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.CastQ3." + ex);
            }
        }

        internal static void CastQ(Obj_AI_Base target)
        {
            try
            {
                var qPred = Q.GetPrediction(target);

                if (qPred.HitChance >= Aimtec.SDK.Prediction.Skillshots.HitChance.Medium)
                {
                    Q.Cast(qPred.UnitPosition);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.CastQ3." + ex);
            }
        }
    }
}