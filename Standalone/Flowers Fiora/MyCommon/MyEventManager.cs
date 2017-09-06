namespace Flowers_Fiora.MyCommon
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

    using Flowers_Fiora.MyBase;

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    #endregion

    internal class MyEventManager : MyLogic
    {
        internal static void Initializer()
        {
            try
            {
                Game.OnUpdate += OnUpdate;
                SpellBook.OnCastSpell += OnCastSpell;
                Orbwalker.PreMove += OnPreMove;
                Orbwalker.PostAttack += OnPostAttack;
                Render.OnRender += OnRender;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.Initializer." + ex);
            }
        }

        private static void OnCastSpell(Obj_AI_Base sender, SpellBookCastSpellEventArgs Args)
        {
            try
            {
                if (sender.IsMe && Args.Slot == SpellSlot.Q)
                {
                    lastQTime = Game.TickCount;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCastSpell." + ex);
            }
        }

        private static void OnUpdate()
        {
            try
            {
                ResetToDefalut();

                if (Me.IsDead || Me.IsRecalling())
                {
                    if (Orbwalker.Mode != OrbwalkingMode.None)
                    {
                        OrbwalkerPoint = Game.CursorPos;
                    }
                    return;
                }

                if (FleeMenu["FlowersFiora.FleeMenu.FleeKey"].As<MenuKeyBind>().Enabled && Me.CanMoveMent())
                {
                    FleeEvent();
                }

                KillStealEvent();
                
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

        private static void ResetToDefalut()
        {
            try
            {
                if (Orbwalker.Mode != OrbwalkingMode.Combo && Orbwalker.Mode != OrbwalkingMode.None)
                {
                    OrbwalkerPoint = Game.CursorPos;
                }

                if (Game.TickCount - lastMoveChangedTime >
                    MiscMenu["FlowersFiora.MiscMenu.ForceResetTime"].Value +
                    (MiscMenu["FlowersFiora.MiscMenu.ForceResetTimePing"].Enabled ? Game.Ping : 0) +
                    (MiscMenu["FlowersFiora.MiscMenu.ForceResetTimeMoveSpeed"].Enabled ? Me.MoveSpeed : 0))
                {
                    if (Orbwalker.Mode == OrbwalkingMode.Combo)
                    {
                        OrbwalkerPoint = Game.CursorPos;
                    }

                    Orbwalker.ForceTarget(null);
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

                if (FleeMenu["FlowersFiora.FleeMenu.Q"].Enabled && Q.Ready)
                {
                    var obj = MyExtraManager.GetNearObj();

                    if (obj != null && obj.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(obj);
                    }
                    else
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.FleeEvent." + ex);
            }
        }
 
        private static void KillStealEvent()
        {
            try
            {
                if (KillStealMenu["FlowersFiora.KillStealMenu.Q"].Enabled && Q.Ready && GameObjects.EnemyHeroes.Where(
                            x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q))
                        .Any(
                            target =>
                                target.IsValidTarget(Q.Range) && !target.IsUnKillable() && CastQ(target)))
                {
                    return;
                }

                if (KillStealMenu["FlowersFiora.KillStealMenu.W"].Enabled && W.Ready)
                {
                    foreach(var target in GameObjects.EnemyHeroes.Where(
                                x => x.IsValidTarget(W.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                    {
                        if (target.IsValidTarget(W.Range) && !target.IsUnKillable())
                        {
                            var wPred = W.GetPrediction(target);

                            if (wPred.HitChance >= Aimtec.SDK.Prediction.Skillshots.HitChance.VeryHigh)
                            {
                                W.Cast(wPred.UnitPosition);
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

        private static void ComboEvent()
        {
            try
            {
                var target = TargetSelector.GetTarget(800f);

                if (target != null && target.IsValidTarget(800f))
                {
                    if (ComboMenu["FlowersFiora.ComboMenu.Item"].Enabled)
                    {
                        if ((target.Health * 1.12 >= target.MaxHealth ? target.MaxHealth : target.Health * 1.12) <
                            MyExtraManager.GetComboDamage(target) && target.DistanceToPlayer() > 400)
                        {
                            ItemsUse(true, false, false);
                        }

                        if (target.IsValidTarget(400) && (!target.IsValidAutoRange() || !Orbwalker.CanAttack()))
                        {
                            ItemsUse(false, true, false);
                        }
                    }

                    if (ComboMenu["FlowersFiora.ComboMenu.Ignite"].Enabled && IgniteSlot != SpellSlot.Unknown &&
                        Ignite.Ready)
                    {
                        if (target.Health < MyExtraManager.GetComboDamage(target) * 0.6 ||
                            target.Health < Me.GetIgniteDamage(target))
                        {
                            Ignite.CastOnUnit(target);
                        }
                    }

                    if (ComboMenu["FlowersFiora.ComboMenu.Force"].Enabled && target.IsValidTarget(500))
                    {
                        ForcusAttack(target);
                    }

                    if (ComboMenu["FlowersFiora.ComboMenu.Q"].Enabled && Q.Ready && target.IsValidTarget(Q.Range))
                    {
                        CastQ(target);
                    }

                    if (ComboMenu["FlowersFiora.ComboMenu.R"].As<MenuKeyBind>().Enabled && R.Ready)
                    {
                        if (ComboMenu["FlowersFiora.ComboMenu.RSolo"].Enabled &&
                            ComboMenu["FlowersFiora.ComboMenu.RTargetFor" + target.ChampionName].Enabled &&
                            target.IsValidTarget(R.Range))
                        {
                            if (Me.CountAllyHeroesInRange(1000) < 2 && Me.CountEnemyHeroesInRange(1000) <= 2)
                            {
                                if (target.Health < MyExtraManager.GetComboDamage(target) - Me.GetAutoAttackDamage(target))
                                {
                                    if (!target.IsUnKillable() && target.Health > Me.GetAutoAttackDamage(target) * 3)
                                    {
                                        R.CastOnUnit(target);
                                    }
                                }
                            }
                        }

                        if (ComboMenu["FlowersFiora.ComboMenu.RTeam"].Enabled)
                        {
                            if (Me.CountEnemyHeroesInRange(1000) > 2 && Me.CountAllyHeroesInRange(1000) > 1)
                            {
                                foreach (
                                    var x in
                                    GameObjects.EnemyHeroes.Where(
                                        x =>
                                            x.IsValidTarget(R.Range) &&
                                            x.Health <=
                                            MyExtraManager.GetComboDamage(target) + Me.GetAutoAttackDamage(x) * 3))
                                {
                                    if (!x.IsUnKillable() && x.IsValidTarget(R.Range))
                                    {
                                        R.CastOnUnit(x);
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
                if (Me.ManaPercent() >= HarassMenu["FlowersFiora.HarassMenu.HarassManaPercent"].Value)
                {
                    if (HarassMenu["FlowersFiora.HarassMenu.Turret"].Enabled && Me.IsUnderEnemyTurret())
                    {
                        return;
                    }

                    if (HarassMenu["FlowersFiora.HarassMenu.Q"].Enabled && Q.Ready)
                    {
                        var target = TargetSelector.GetTarget(800f);

                        if (target != null && target.IsValidTarget(800f))
                        {
                            CastQ(target);
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
                if (Me.ManaPercent() >= ClearMenu["FlowersFiora.ClearMenu.LaneClearManaPercent"].Value)
                {
                    if (ClearMenu["FlowersFiora.ClearMenu.LaneClearTurret"].Enabled && Me.IsUnderEnemyTurret())
                    {
                        return;
                    }

                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        foreach (var minion in minions)
                        {
                            if (ClearMenu["FlowersFiora.ClearMenu.LaneClearQ"].Enabled && Q.Ready)
                            {
                                if (minion.Health < Me.GetSpellDamage(minion, SpellSlot.Q))
                                {
                                    Q.CastOnUnit(minion);
                                }
                                else if (!ClearMenu["FlowersFiora.ClearMenu.LaneClearQLH"].Enabled && minion.IsValidTarget(Q.Range))
                                {
                                    Q.CastOnUnit(minion);
                                }
                            }

                            if (ClearMenu["FlowersFiora.ClearMenu.LaneClearE"].Enabled && E.Ready && Orbwalker.CanAttack())
                            {
                                if (
                                    minion.IsValidTarget(Me.AttackRange + Me.BoundingRadius + minion.BoundingRadius + 30) &&
                                    minion.Health <
                                    Me.GetAutoAttackDamage(minion) + Me.GetAutoAttackDamage(minion) * 1.4)
                                {
                                    E.Cast();
                                    Orbwalker.ForceTarget(minion);
                                }
                            }

                            if (ClearMenu["FlowersFiora.ClearMenu.LaneClearItem"].Enabled && minions.Count(x => x.IsValidTarget(400)) > 2)
                            {
                                ItemsUse(false, true, true);
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
                if (Me.ManaPercent() >= ClearMenu["FlowersFiora.ClearMenu.JungleClearManaPercent"].Value)
                {
                    if (ClearMenu["FlowersFiora.ClearMenu.JungleClearQ"].Enabled && Q.Ready)
                    {
                        var mobs =
                            GameObjects.EnemyMinions.Where(
                                    x => x.IsValidTarget(Q.Range) && x.Team == GameObjectTeam.Neutral && x.IsMob())
                                .ToArray();

                        if (mobs.Any())
                        {
                            var mob = mobs.OrderBy(x => x.MaxHealth).FirstOrDefault(x => x.IsValidTarget(Q.Range));

                            if (mob != null)
                            {
                                Q.CastOnUnit(mob);
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

        private static void ForcusAttack(Obj_AI_Hero target)
        {
            if (Me.IsDashing() || Me.SpellBook.IsCastingSpell || Me.SpellBook.IsAutoAttacking ||
                target.IsValidAutoRange() && Orbwalker.CanAttack())
            {
                return;
            }

            if (Game.TickCount - lastMoveChangedTime < 650 + Game.Ping || Game.TickCount - lastQTime < 650 + Game.Ping)
            {
                return;
            }

            var pos = MyPassiveManager.OrbwalkerPosition(target);

            if (!pos.IsZero)
            {
                OrbwalkerPoint = Me.ServerPosition.Extend(pos, Me.ServerPosition.Distance(pos) + 150);
                lastMoveChangedTime = Game.TickCount;
            }
            else
            {
                OrbwalkerPoint = Game.CursorPos;
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

        private static void OnPostAttack(object sender, PostAttackEventArgs Args)
        {
            try
            {
                OrbwalkerPoint = Game.CursorPos;
                Orbwalker.ForceTarget(null);

                if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget())
                {
                    return;
                }

                switch (Orbwalker.Mode)
                {
                    case OrbwalkingMode.Combo:
                        if (ComboMenu["FlowersFiora.ComboMenu.Item"].Enabled)
                        {
                            ItemsUse(true, true, false);
                        }

                        if (ComboMenu["FlowersFiora.ComboMenu.E"].Enabled && E.Ready)
                        {
                            var target = Args.Target as Obj_AI_Hero;

                            if (target != null && target.IsValidAutoRange())
                            {
                                E.Cast();
                            }
                        }
                        break;
                    case OrbwalkingMode.Mixed:
                        if (Me.ManaPercent() >= HarassMenu["FlowersFiora.HarassMenu.HarassManaPercent"].Value)
                        {
                            if (HarassMenu["FlowersFiora.HarassMenu.Turret"].Enabled && Me.IsUnderEnemyTurret())
                            {
                                return;
                            }

                            if (HarassMenu["FlowersFiora.HarassMenu.Item"].Enabled)
                            {
                                ItemsUse(true, true, false);
                            }

                            if (HarassMenu["FlowersFiora.HarassMenu.E"].Enabled && E.Ready)
                            {
                                var target = Args.Target as Obj_AI_Hero;

                                if (target != null && target.IsValidAutoRange())
                                {
                                    E.Cast();
                                }
                            }
                        }
                        break;
                    case OrbwalkingMode.Laneclear:
                        if (MyManaManager.SpellFarm)
                        {
                            if (Args.Target.IsBuilding())
                            {
                                if (Me.ManaPercent() >= ClearMenu["FlowersFiora.ClearMenu.LaneClearManaPercent"].Value)
                                {
                                    if (ClearMenu["FlowersFiora.ClearMenu.LaneClearE"].Enabled && E.Ready)
                                    {
                                        E.Cast();
                                    }
                                }
                            }
                            else if (Args.Target.IsMob())
                            {
                                if (Me.ManaPercent() >= ClearMenu["FlowersFiora.ClearMenu.JungleClearManaPercent"].Value)
                                {
                                    var mob = Args.Target as Obj_AI_Minion;

                                    if (mob != null && mob.Team == GameObjectTeam.Neutral && mob.IsMob() && mob.IsValidAutoRange())
                                    {
                                        if (ClearMenu["FlowersFiora.ClearMenu.JungleClearItem"].Enabled)
                                        {
                                            ItemsUse(false, true, true);
                                        }

                                        if (ClearMenu["FlowersFiora.ClearMenu.JungleClearE"].Enabled && E.Ready)
                                        {
                                            E.Cast();
                                        }
                                    }
                                }
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
                if (DrawMenu["FlowersFiora.DrawMenu.Q"].Enabled && Q.Ready)
                {
                    Render.Circle(Me.Position, Q.Range, 23, Color.FromArgb(86, 0, 255));
                }

                if (DrawMenu["FlowersFiora.DrawMenu.W"].Enabled && W.Ready)
                {
                    Render.Circle(Me.Position, W.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersFiora.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.Position, R.Range, 23, Color.FromArgb(251, 0, 133));
                }

                if (DrawMenu["FlowersFiora.DrawMenu.ComboR"].Enabled)
                {
                    Vector2 MePos = Vector2.Zero;
                    Render.WorldToScreen(ObjectManager.GetLocalPlayer().Position, out MePos);

                    Render.Text(MePos.X - 57, MePos.Y + 68, Color.Orange,
                        "Combo R(" + ComboMenu["FlowersFiora.ComboMenu.R"].As<MenuKeyBind>().Key + "): " +
                        (ComboMenu["FlowersFiora.ComboMenu.R"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }

        private static void ItemsUse(bool UseYoumuu, bool UseTiamat, bool LaneClear)
        {
            if (UseYoumuu && Me.CountEnemyHeroesInRange(W.Range) > 0)
            {
                if (Me.CanUseItem("YoumusBlade"))
                {
                    Me.UseItem("YoumusBlade");
                }
            }

            if (UseTiamat && (Me.CountEnemyHeroesInRange(385f) > 0 || LaneClear))
            {
                if (Me.CanUseItem("ItemTiamatCleave"))
                {
                    Me.UseItem("ItemTiamatCleave");
                }

                if (Me.CanUseItem("ItemTitanicHydraCleave"))
                {
                    Me.UseItem("ItemTitanicHydraCleave");
                }
            }
        }

        private static bool CastQ(Obj_AI_Hero target)
        {
            if (!Q.Ready || !target.IsValidTarget(Q.Range))
            {
                return false;
            }

            if (Q.Ready)
            {
                if (MyPassiveManager.PassiveCount(target) > 0)
                {
                    var pos = MyPassiveManager.CastQPosition(target);

                    if (MiscMenu["FlowersFiora.MiscMenu.UnderTurret"].Enabled)
                    {
                        if (MiscMenu["FlowersFiora.MiscMenu.ComboUnderTurret"].Enabled ||
                            Orbwalker.Mode != OrbwalkingMode.Combo)
                        {
                            if (pos.PointUnderEnemyTurret())
                            {
                                return false;
                            }
                        }
                    }

                    if (MiscMenu["FlowersFiora.MiscMenu.CheckSafe"].Enabled && !pos.IsSafePosition())
                    {
                        return false;
                    }

                    if (Me.Distance(pos) > Q.Range)
                    {
                        return false;
                    }

                    if (Me.Distance(pos) < 50)
                    {
                        return false;
                    }

                    return Q.Cast(pos);
                }
                else
                {
                    var pos = target.ServerPosition;

                    if (MiscMenu["FlowersFiora.MiscMenu.UnderTurret"].Enabled)
                    {
                        if (MiscMenu["FlowersFiora.MiscMenu.ComboUnderTurret"].Enabled ||
                            Orbwalker.Mode != OrbwalkingMode.Combo)
                        {
                            if (pos.PointUnderEnemyTurret())
                            {
                                return false;
                            }
                        }
                    }

                    if (MiscMenu["FlowersFiora.MiscMenu.CheckSafe"].Enabled && !pos.IsSafePosition())
                    {
                        return false;
                    }

                    if (target.IsValidTarget(Q.Range) && Me.Distance(target) >= 80)
                    {
                        return Q.Cast(target.ServerPosition);
                    }
                }
            }

            return false;
        }
    }
}