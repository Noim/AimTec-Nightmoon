namespace Flowers_Katarina.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.TargetSelector;

    using Flowers_Katarina.MyBase;

    using System;
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
                GameObject.OnCreate += OnCreate;
                GameObject.OnDestroy += OnDestroy;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
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
                Daggers.RemoveAll(x => Game.TickCount - x.time > 3850);

                if (Me.IsDead || Me.IsRecalling())
                {
                    return;
                }

                if (FleeMenu["FlowersKatarina.FleeMenu.Key"].As<MenuKeyBind>().Enabled)
                {
                    FleeEvent();
                }

                if (MiscMenu["FlowersKatarina.MiscMenu.OneKeyEW"].As<MenuKeyBind>().Enabled && E.Ready && W.Ready)
                {
                    SemiEW();
                }

                KillStealEvent();

                if (isCastingUlt)
                {
                    Orbwalker.AttackingEnabled = false;
                    Orbwalker.MovingEnabled = false;

                    if (MiscMenu["FlowersKatarina.MiscMenu.AutoCancelR"].Enabled)
                    {
                        MyExtraManager.CancelUlt();
                    }

                    return;
                }

                Orbwalker.AttackingEnabled = true;
                Orbwalker.MovingEnabled = true;

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

        private static void FleeEvent()
        {
            try
            {
                Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);

                if (FleeMenu["FlowersKatarina.FleeMenu.W"].Enabled && W.Ready)
                {
                    W.Cast();
                }

                if (FleeMenu["FlowersKatarina.FleeMenu.E"].Enabled && E.Ready)
                {
                    var fleeList = MyExtraManager.badaoFleeLogic.ToArray();

                    if (fleeList.Any())
                    {
                        var nearest = fleeList.MinOrDefault(x => x.Position.Distance(Game.CursorPos));

                        if (nearest != null && nearest.Position.DistanceToMouse() < Me.DistanceToMouse() &&
                            nearest.Position.DistanceToPlayer() > 300)
                        {
                            var pos = nearest.Position.To2D().Extend(Game.CursorPos.To2D(), 150);
                            E.Cast(pos);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.FleeEvent." + ex);
            }
        }

        private static void SemiEW()
        {
            try
            {
                if (E.Ready)
                {
                    var fleeList = MyExtraManager.badaoFleeLogic.ToArray();

                    if (fleeList.Any())
                    {
                        var nearest = fleeList.MinOrDefault(x => x.Position.Distance(Game.CursorPos));

                        if (nearest != null && nearest.Position.DistanceToPlayer() <= E.Range)
                        {
                            var pos = nearest.Position.To2D().Extend(Game.CursorPos.To2D(), 150);

                            E.Cast(pos);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.SetCoolDownTime." + ex);
            }
        }

        private static void KillStealEvent()
        {
            try
            {
                if (isCastingUlt && !KillStealMenu["FlowersKatarina.KillStealMenu.CancelR"].Enabled)
                {
                    return;
                }

                if (Me.CountEnemyHeroesInRange(E.Range) == 0)
                {
                    return;
                }

                foreach (
                    var target in 
                    GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget(E.Range + 300))
                        .OrderBy(x => x.Health))
                {
                    if (target.IsValidTarget(E.Range + 300))
                    {
                        if (KillStealMenu["FlowersKatarina.KillStealMenu.Q"].Enabled && target.Health < Q.GetDamage(target) && Q.Ready &&
                            target.IsValidTarget(Q.Range))
                        {
                            if (isCastingUlt)
                            {
                                MyExtraManager.CancelUlt(true);
                                Q.CastOnUnit(target);
                                return;
                            }
                            Q.CastOnUnit(target);
                            return;
                        }

                        if (KillStealMenu["FlowersKatarina.KillStealMenu.E"].Enabled && target.Health < E.GetDamage(target) && E.Ready)
                        {
                            if (target.DistanceToPlayer() <= E.Range + 130)
                            {
                                var pos = Me.Position.Extend(target.Position, target.DistanceToPlayer() + 130);
                                if (isCastingUlt)
                                {
                                    MyExtraManager.CancelUlt(true);
                                    E.Cast(pos);
                                    return;
                                }
                                E.Cast(pos);
                                return;
                            }

                            if (target.IsValidTarget(E.Range))
                            {
                                if (isCastingUlt)
                                {
                                    MyExtraManager.CancelUlt(true);
                                    E.Cast(target);
                                    return;
                                }
                                E.Cast(target);
                                return;
                            }
                        }

                        if (KillStealMenu["FlowersKatarina.KillStealMenu.Q"].Enabled && target.Health < Q.GetDamage(target) + E.GetDamage(target) &&
                             KillStealMenu["FlowersKatarina.KillStealMenu.E"].Enabled && Q.Ready && E.Ready &&
                            target.IsValidTarget(E.Range))
                        {
                            if (isCastingUlt)
                            {
                                MyExtraManager.CancelUlt(true);
                                Q.CastOnUnit(target);
                                E.Cast(target);
                                return;
                            }
                            Q.CastOnUnit(target);
                            E.Cast(target);
                            return;
                        }

                        if (target.Health < MyExtraManager.GetKataPassiveDamage(target) + E.GetDamage(target) &&
                            KillStealMenu["FlowersKatarina.KillStealMenu.E"].Enabled && E.Ready &&
                            Daggers.Any(
                                x =>
                                    x.obj.IsValid &&
                                    x.pos.Distance(target.Position) <= PassiveRange &&
                                    x.pos.DistanceToPlayer() <= E.Range))
                        {
                            foreach (
                                var obj in
                                Daggers.Where(x => x.pos.Distance(target.Position) <= PassiveRange)
                                    .OrderBy(x => x.pos.Distance(target.Position)))
                            {
                                if (obj.obj != null && obj.obj.IsValid && obj.pos.DistanceToPlayer() <= E.Range)
                                {
                                    if (isCastingUlt)
                                    {
                                        MyExtraManager.CancelUlt(true);
                                        E.Cast(obj.pos);
                                        MyDelayAction.Queue(100 + Game.Ping, () => E.Cast(target));
                                        return;
                                    }
                                    E.Cast(obj.pos);
                                    MyDelayAction.Queue(100 + Game.Ping, () => E.Cast(target));
                                    return;
                                }
                            }
                        }

                        if (target.Health < R.GetDamage(target) * 0.6 && KillStealMenu["FlowersKatarina.KillStealMenu.R"].Enabled
                            && R.Ready && target.IsValidTarget(R.Range) && target.Health > 50 * target.Level)
                        {
                            R.Cast();
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

        private static void ComboEvent()
        {
            try
            {
                if (isCastingUlt)
                {
                    return;
                }

                var target = TargetSelector.GetTarget(E.Range + 300f);

                if (target != null && target.IsValidTarget(E.Range + 300f))
                {
                    if (ComboMenu["FlowersKatarina.ComboMenu.Ignite"].Enabled && IgniteSlot != SpellSlot.Unknown &&
                        Ignite.Ready && target.IsValidTarget(600) &&
                        (target.Health < MyExtraManager.GetComboDamage(target) && target.IsValidTarget(400) ||
                         target.Health < Me.GetIgniteDamage(target)))
                    {
                        Ignite.Cast(target);
                    }

                    //Item Hextech_Gunblade Bilgewater_Cutlass

                    if (ComboMenu["FlowersKatarina.ComboMenu.W"].Enabled &&
                        ComboMenu["FlowersKatarina.ComboMenu.WSmart"].Enabled && W.Ready &&
                        target.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }

                    switch (ComboMenu["FlowersKatarina.ComboMenu.Mode"].As<MenuList>().Value)
                    {
                        case 0:
                            MyExtraManager.QEWLogic(target, ComboMenu["FlowersKatarina.ComboMenu.Q"].Enabled,
                                ComboMenu["FlowersKatarina.ComboMenu.W"].Enabled,
                                ComboMenu["FlowersKatarina.ComboMenu.E"].Enabled);
                            break;
                        case 1:
                            MyExtraManager.EQWLogic(target, ComboMenu["FlowersKatarina.ComboMenu.Q"].Enabled,
                                ComboMenu["FlowersKatarina.ComboMenu.W"].Enabled,
                                ComboMenu["FlowersKatarina.ComboMenu.E"].Enabled);
                            break;
                    }

                    if (ComboMenu["FlowersKatarina.ComboMenu.R"].Enabled && R.Ready &&
                        Me.CountEnemyHeroesInRange(R.Range) > 0 && !Q.Ready && !W.Ready && !E.Ready)
                    {
                        if (ComboMenu["FlowersKatarina.ComboMenu.RAlways"].Enabled)
                        {
                            Orbwalker.AttackingEnabled = false;
                            Orbwalker.MovingEnabled = false;
                            R.Cast();
                        }

                        if (ComboMenu["FlowersKatarina.ComboMenu.RKillAble"].Enabled &&
                            (target.Health <= MyExtraManager.GetComboDamage(target) ||
                             target.Health <= R.GetDamage(target) * 0.8) &&
                             target.Health > Q.GetDamage(target) + MyExtraManager.GetKataPassiveDamage(target)* 2)
                        {
                            Orbwalker.AttackingEnabled = false;
                            Orbwalker.MovingEnabled = false;
                            R.Cast();
                        }

                        if (ComboMenu["FlowersKatarina.ComboMenu.RCountHit"].As<MenuSliderBool>().Enabled &&
                            Me.CountEnemyHeroesInRange(R.Range) >= ComboMenu["FlowersKatarina.ComboMenu.RCountHit"].As<MenuSliderBool>().Value)
                        {
                            Orbwalker.AttackingEnabled = false;
                            Orbwalker.MovingEnabled = false;
                            R.Cast();
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
                if (isCastingUlt)
                {
                    return;
                }

                var target = TargetSelector.GetTarget(E.Range + 300f);

                if (target.IsValidTarget(E.Range + 300f))
                {
                    if (HarassMenu["FlowersKatarina.HarassMenu.W"].Enabled && W.Ready && target.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }

                    if (HarassMenu["FlowersKatarina.HarassMenu.Q"].Enabled && Q.Ready && Me.Level < 3 &&
                        target.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(target);
                    }

                    switch (HarassMenu["FlowersKatarina.HarassMenu.Mode"].As<MenuList>().Value)
                    {
                        case 0:
                            MyExtraManager.QEWLogic(target, HarassMenu["FlowersKatarina.HarassMenu.Q"].Enabled,
                                HarassMenu["FlowersKatarina.HarassMenu.W"].Enabled,
                                HarassMenu["FlowersKatarina.HarassMenu.E"].Enabled);
                            break;
                        case 1:
                            MyExtraManager.EQWLogic(target, HarassMenu["FlowersKatarina.HarassMenu.Q"].Enabled,
                                HarassMenu["FlowersKatarina.HarassMenu.W"].Enabled,
                                HarassMenu["FlowersKatarina.HarassMenu.E"].Enabled);
                            break;
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
                if (isCastingUlt)
                {
                    return;
                }

                if (ClearMenu["FlowersKatarina.ClearMenu.LaneClearQ"].Enabled && Q.Ready)
                {
                    if (ClearMenu["FlowersKatarina.ClearMenu.LaneClearQOnlyLH"].Enabled)
                    {
                        var qMinions =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                        foreach (var qMinion in qMinions.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                        {
                            if (qMinion != null && qMinion.IsValidTarget(Q.Range))
                            {
                                Q.CastOnUnit(qMinion);
                            }
                        }
                    }
                    else
                    {
                        var qMinions =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                        if (qMinions.Length >= 2)
                        {
                            Q.CastOnUnit(qMinions.FirstOrDefault());
                        }
                    }
                }

                if (ClearMenu["FlowersKatarina.ClearMenu.LaneClearW"].Enabled && W.Ready)
                {
                    var wMinions =
                           GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion()).ToArray();

                    if (wMinions.Length >= 3)
                    {
                        W.Cast();
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
                if (isCastingUlt)
                {
                    return;
                }

                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob()).ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.FirstOrDefault();

                    if (mob != null && mob.IsValidTarget(Q.Range))
                    {
                        if (ClearMenu["FlowersKatarina.ClearMenu.JungleClearQ"].Enabled && Q.Ready && mob.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(mob);
                        }

                        if (ClearMenu["FlowersKatarina.ClearMenu.JungleClearW"].Enabled && W.Ready && mob.IsValidTarget(W.Range))
                        {
                            W.Cast();
                        }

                        if (ClearMenu["FlowersKatarina.ClearMenu.JungleClearE"].Enabled && E.Ready)
                        {
                            if (Daggers.Any(
                                x =>
                                    mobs.Any(a => a.Distance(x.pos) <= PassiveRange) &&
                                    x.pos.DistanceToPlayer() <= E.Range))
                            {
                                foreach (
                                    var obj in
                                    Daggers.Where(x => x.pos.Distance(mob.Position) <= PassiveRange)
                                        .OrderByDescending(x => x.pos.Distance(mob.Position)))
                                {
                                    if (obj.obj != null && obj.obj.IsValid && obj.pos.DistanceToPlayer() <= E.Range)
                                    {
                                        E.Cast(obj.pos);
                                    }
                                }
                            }
                            else if (mob.DistanceToPlayer() <= E.Range + 130)
                            {
                                var pos = Me.Position.Extend(mob.Position, mob.DistanceToPlayer() + 130);

                                E.Cast(pos);
                            }
                            else if (mob.IsValidTarget(E.Range))
                            {
                                E.Cast(mob);
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

        private static void LastHitEvent()
        {
            try
            {
                if (isCastingUlt)
                {
                    return;
                }

                if (LastHitMenu["FlowersKatarina.LastHitMenu.Q"].Enabled && Q.Ready)
                {
                    var qMinions =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && (x.IsMinion() || x.IsMob()))
                            .ToArray();

                    foreach (var minion in qMinions.Where(x => x.IsValidTarget(Q.Range)))
                    {
                        if (minion != null && minion.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(minion);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.LastHitEvent." + ex);
            }
        }

        private static void OnCreate(GameObject sender)
        {
            try
            {
                if (!sender.Name.Contains("Katarina_"))
                {
                    return;
                }

                switch (sender.Name)
                {
                    case "Katarina_Base_Q_Dagger_Land_Stone.troy":
                    case "Katarina_Base_W_indicator_Ally.troy":
                    case "Katarina_Base_E_Beam.troy":
                    case "Katarina_Base_Dagger_Ground_Indicator.troy":
                        Daggers.Add(new MyDaggerManager(sender, sender.ServerPosition, Game.TickCount));
                        break;
                    case "Katarina_Base_Dagger_PickUp_Cas.troy":
                    case "Katarina_Base_Dagger_PickUp_Tar.troy":
                        Daggers.RemoveAll(x => x.obj.NetworkId == sender.NetworkId);
                        break;
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
                if (!sender.Name.Contains("Katarina"))
                {
                    return;
                }

                switch (sender.Name)
                {
                    case "Katarina_Base_Dagger_Ground_Indicator.troy":
                    case "Katarina_Base_Dagger_PickUp_Cas.troy":
                    case "Katarina_Base_Q_Dagger_Land_Stone.troy":
                    case "Katarina_Base_Dagger_PickUp_Tar.troy":
                        Daggers.RemoveAll(x => x.obj.NetworkId == sender.NetworkId);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCreate." + ex);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            try
            {
                if (sender.IsMe)
                {
                    if (Args.SpellData.Name.Contains("KatarinaW"))
                    {
                        lastWTime = Game.TickCount;
                    }

                    if (Args.SpellData.Name.Contains("KatarinaE"))
                    {
                        lastETime = Game.TickCount;

                        if (MiscMenu["FlowersKatarina.MiscMenu.OneKeyEW"].As<MenuKeyBind>().Enabled && W.Ready)
                        {
                            W.Cast();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnProcessSpellCast." + ex);
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

                if (DrawMenu["FlowersKatarina.DrawMenu.Dagger"].Enabled)
                {
                    foreach (var position in Daggers.Where(x => !x.obj.IsDead && x.obj.IsValid).Select(x => x.pos))
                    {
                        if (position != Vector3.Zero)
                        {
                            Render.Circle(position, PassiveRange, 30, Color.FromArgb(69, 0, 255));
                        }
                    }
                }

                if (DrawMenu["FlowersKatarina.DrawMenu.Q"].Enabled && Q.Ready)
                {
                    Render.Circle(Me.Position, Q.Range, 23, Color.FromArgb(251, 0, 133));
                }

                if (DrawMenu["FlowersKatarina.DrawMenu.E"].Enabled && E.Ready)
                {
                    Render.Circle(Me.Position, E.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersKatarina.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.Position, R.Range, 23, Color.FromArgb(0, 255, 161));
                }

                Vector2 MePos = Vector2.Zero;
                Render.WorldToScreen(ObjectManager.GetLocalPlayer().ServerPosition, out MePos);

                if (DrawMenu["FlowersKatarina.DrawMenu.ComboE"].Enabled)
                {
                    Render.Text(MePos.X - 57, MePos.Y + 68, Color.Orange,
                        "Only E KillAble(" + ComboMenu["FlowersKatarina.ComboMenu.EKillAble"].As<MenuKeyBind>().Key + "): " +
                        (ComboMenu["FlowersKatarina.ComboMenu.EKillAble"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }

                if (DrawMenu["FlowersKatarina.DrawMenu.ComboMode"].Enabled)
                {
                    Render.Text(MePos.X - 57, MePos.Y + 88, Color.Orange,
                        "Combo Mode(" + ComboMenu["FlowersKatarina.ComboMenu.SwitchMode"].As<MenuKeyBind>().Key + "): " +
                        ComboMenu["FlowersKatarina.ComboMenu.Mode"].As<MenuList>().SelectedItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }
    }
}