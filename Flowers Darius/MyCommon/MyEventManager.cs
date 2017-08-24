namespace Flowers_Darius.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Events;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.TargetSelector;

    using Flowers_Darius.MyBase;

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
                Dash.HeroDashed += OnDash;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
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
                if (Me.IsDead || Me.IsRecalling())
                {
                    return;
                }

                KillStealEvent();

                if (!Me.HasBuff("dariusqcast"))
                {
                    Orbwalker.AttackingEnabled = true;
                    Orbwalker.MovingEnabled = true;
                }

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

        private static void KillStealEvent()
        {
            try
            {
                if (Me.CountEnemyHeroesInRange(E.Range + R.Range) == 0)
                {
                    return;
                }

                if (KillStealMenu["FlowersDarius.KillStealMenu.R"].Enabled && R.Ready)
                {
                    foreach (var rTarget in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) &&
                                                                           x.Health <= R.GetDamage(x) &&
                                                                           !x.HasBuff("willrevive")))
                    {
                        if (rTarget != null && rTarget.IsValidTarget(R.Range) && !rTarget.IsUnKillable())
                        {
                            R.CastOnUnit(rTarget);
                            return;
                        }
                    }
                }

                if (KillStealMenu["FlowersDarius.KillStealMenu.R"].Enabled && R.Ready && 
                    KillStealMenu["FlowersDarius.KillStealMenu.E"].Enabled && E.Ready)
                {
                    foreach (var rTarget in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) &&
                                                                               !x.IsValidTarget(R.Range) &&
                                                                               x.Health <= R.GetDamage(x) &&
                                                                               !x.HasBuff("willrevive")))
                    {
                        if (rTarget != null && rTarget.IsValidTarget(E.Range) && !rTarget.IsUnKillable())
                        {
                            var ePred = E.GetPrediction(rTarget);

                            if (ePred.HitChance >= Aimtec.SDK.Prediction.Skillshots.HitChance.Medium)
                            {
                                E.Cast(ePred.UnitPosition);

                                if (rTarget.IsValidTarget(R.Range))
                                {
                                    R.CastOnUnit(rTarget);
                                    return;
                                }
                            }
                        }
                    }
                }

                if (KillStealMenu["FlowersDarius.KillStealMenu.Q"].Enabled && Q.Ready && Orbwalker.Mode != OrbwalkingMode.Combo)
                {
                    foreach (var qTarget in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range) &&
                                                                           x.Health <= Q.GetDamage(x) &&
                                                                           !x.HasBuff("willrevive")))
                    {
                        if (qTarget != null && qTarget.IsValidTarget(Q.Range) && !qTarget.IsUnKillable())
                        {
                            Q.Cast();
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
                if (ComboMenu["FlowersDarius.ComboMenu.R"].As<MenuKeyBind>().Enabled && R.Ready)
                {
                    foreach (var rTarget in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) &&
                                                                           x.Health <= R.GetDamage(x) &&
                                                                           !x.HasBuff("willrevive")))
                    {
                        if (rTarget != null && rTarget.IsValidTarget(R.Range) && !rTarget.IsUnKillable())
                        {
                            R.CastOnUnit(rTarget);
                        }
                    }
                }

                var target = TargetSelector.GetTarget(600f);

                if (target != null && target.IsValidTarget(600f))
                {
                    if (ComboMenu["FlowersDarius.ComboMenu.Ignite"].Enabled && IgniteSlot != SpellSlot.Unknown &&
                        Ignite.Ready && target.IsValidTarget(600) &&
                        (target.Health < MyExtraManager.GetComboDamage(target) && target.IsValidTarget(400) ||
                         target.Health < Me.GetIgniteDamage(target)))
                    {
                        Ignite.Cast(target);
                    }

                    if (Me.CountEnemyHeroesInRange(400) > 0)
                    {
                        UseItem();
                    }

                    if ((MiscMenu["FlowersDarius.MiscMenu.LockQ"].As<MenuList>().Value == 0 ||
                        MiscMenu["FlowersDarius.MiscMenu.LockQ"].As<MenuList>().Value == 1) &&
                        Me.HasBuff("dariusqcast") && Me.CountEnemyHeroesInRange(650) < 3)
                    {
                        Orbwalker.AttackingEnabled = false;
                        Orbwalker.MovingEnabled = false;

                        if (target.DistanceToPlayer() <= 250)
                        {
                            Me.IssueOrder(OrderType.MoveTo, Me.Position.Extend(target.Position, -Q.Range));
                        }
                        else if (target.DistanceToPlayer() <= Q.Range)
                        {
                            Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                        }
                        else
                        {
                            Me.IssueOrder(OrderType.MoveTo, target.Position);
                        }
                    }
                    else
                    {
                        Orbwalker.AttackingEnabled = true;
                        Orbwalker.MovingEnabled = true;
                    }

                    if (ComboMenu["FlowersDarius.ComboMenu.Q"].Enabled && Q.Ready && target.DistanceToPlayer() <= Q.Range &&
                        CanQHit(target) && Me.CanMoveMent())
                    {
                        if (ComboMenu["FlowersDarius.ComboMenu.SaveMana"].Enabled && Me.Mana < RMana + Me.GetSpell(SpellSlot.Q).Cost)
                        {
                            return;
                        }

                        if (Game.TickCount - lastETime > 1000)
                        {
                            Q.Cast();
                        }
                    }

                    if (ComboMenu["FlowersDarius.ComboMenu.E"].Enabled && E.Ready && target.DistanceToPlayer() <= E.Range - 30 &&
                        !target.IsValidAutoRange() && !target.HaveShiled())
                    {
                        if (ComboMenu["FlowersDarius.ComboMenu.SaveMana"].Enabled && Me.Mana < RMana + Me.GetSpell(SpellSlot.E).Cost)
                        {
                            return;
                        }

                        var ePred = E.GetPrediction(target);

                        if (ePred.HitChance >= Aimtec.SDK.Prediction.Skillshots.HitChance.Medium)
                        {
                            E.Cast(ePred.UnitPosition);
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
                if (Me.ManaPercent() >= HarassMenu["FlowersDarius.HarassMenu.HarassManaPercent"].Value)
                {
                    var target = TargetSelector.GetTarget(600f);

                    if (target.IsValidTarget(600f))
                    {
                        if ((MiscMenu["FlowersDarius.MiscMenu.LockQ"].As<MenuList>().Value == 0 ||
                             MiscMenu["FlowersDarius.MiscMenu.LockQ"].As<MenuList>().Value == 2) &&
                            Me.HasBuff("dariusqcast") && Me.CountEnemyHeroesInRange(650) < 3)
                        {
                            Orbwalker.AttackingEnabled = false;
                            Orbwalker.MovingEnabled = false;

                            if (target.DistanceToPlayer() <= 250)
                            {
                                Me.IssueOrder(OrderType.MoveTo, Me.Position.Extend(target.Position, -Q.Range));
                            }
                            else if (target.DistanceToPlayer() <= Q.Range)
                            {
                                Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                            }
                            else
                            {
                                Me.IssueOrder(OrderType.MoveTo, target.Position);
                            }
                        }
                        else
                        {
                            Orbwalker.AttackingEnabled = true;
                            Orbwalker.MovingEnabled = true;
                        }

                        if (HarassMenu["FlowersDarius.HarassMenu.E"].Enabled && E.Ready && !target.IsValidAutoRange() &&
                            !target.HaveShiled() && target.DistanceToPlayer() <= E.Range - 30)
                        {
                            var ePred = E.GetPrediction(target);

                            if (ePred.HitChance >= Aimtec.SDK.Prediction.Skillshots.HitChance.Medium)
                            {
                                E.Cast(ePred.UnitPosition);
                            }  
                        }

                        if (HarassMenu["FlowersDarius.HarassMenu.Q"].Enabled && Q.Ready && !target.IsValidAutoRange() &&
                            target.DistanceToPlayer() <= Q.Range && CanQHit(target))
                        {
                            Q.Cast();
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
                if (Me.ManaPercent() >= ClearMenu["FlowersDarius.ClearMenu.LaneClearManaPercent"].Value)
                {
                    if (ClearMenu["FlowersDarius.ClearMenu.LaneClearQ"].As<MenuSliderBool>().Enabled && Q.Ready)
                    {
                        var qMinions =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                        if (qMinions.Length >=
                            ClearMenu["FlowersDarius.ClearMenu.LaneClearQ"].As<MenuSliderBool>().Value)
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
                if (Me.ManaPercent() >= ClearMenu["FlowersDarius.ClearMenu.JungleClearManaPercent"].Value)
                {
                    if (ClearMenu["FlowersDarius.ClearMenu.JungleClearQ"].Enabled && Q.Ready)
                    {
                        var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob()).ToArray();

                        if (mobs.Length > 0)
                        {
                            Q.Cast();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.JungleClearEvent." + ex);
            }
        }

        private static void OnDash(object sender, Dash.DashArgs Args)
        {
            try
            {
                if (Args.Unit == null || Args.Unit.IsAlly || Args.Unit.IsMe || 
                    Args.Unit.Type != GameObjectType.obj_AI_Hero || 
                    Args.IsBlink || Args.Unit.HaveShiled())
                {
                    return;
                }

                if (Orbwalker.Mode == OrbwalkingMode.Combo && ComboMenu["FlowersDarius.ComboMenu.EDash"].Enabled &&
                    E.Ready)
                {
                    if (Args.StartTick - Args.EndTick > E.Delay)
                    {
                        if (Args.EndPos.DistanceToPlayer() < E.Range)
                        {
                            E.Cast(Args.EndPos);
                        }

                        if (Args.Unit.IsValidTarget(E.Range))
                        {
                            E.Cast(Args.Unit.ServerPosition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnDash." + ex);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            try
            {
                if (sender == null || !sender.IsMe)
                {
                    return;
                }

                if (Args.SpellData.Name.Contains("DariusAxeGrabCone"))
                {
                    lastETime = Game.TickCount;
                }

                if (Args.SpellData.Name.Contains("ItemTiamatCleave"))
                {
                    MyDelayAction.Queue(5, () => Orbwalker.ResetAutoAttackTimer());
                    if (Orbwalker.Mode == OrbwalkingMode.Combo)
                    {
                        if (!GameObjects.EnemyHeroes.Any(x => x.IsValidAutoRange()))
                        {
                            return;
                        }

                        if (ComboMenu["FlowersDarius.ComboMenu.W"].Enabled && W.Ready)
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

        private static void OnPostAttack(object sender, PostAttackEventArgs Args)
        {
            try
            {
                Orbwalker.ForceTarget(null);

                if (Args.Target == null || Args.Target.IsDead)
                {
                    return;
                }

                switch (Args.Target.Type)
                {
                    case GameObjectType.obj_AI_Hero:
                        if (Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            UseItem();

                            if (ComboMenu["FlowersDarius.ComboMenu.W"].Enabled && W.Ready)
                            {
                                var target = (Obj_AI_Hero)Args.Target;

                                if (target != null && !target.IsDead)
                                {
                                    W.Cast();
                                    Orbwalker.ForceTarget(target);
                                }
                            }
                        }
                        else if (Orbwalker.Mode == OrbwalkingMode.Mixed)
                        {
                            if (Me.ManaPercent() >= HarassMenu["FlowersDarius.HarassMenu.HarassManaPercent"].Value)
                            {
                                if (HarassMenu["FlowersDarius.HarassMenu.W"].Enabled && W.Ready)
                                {
                                    var target = (Obj_AI_Hero)Args.Target;

                                    if (target != null && !target.IsDead)
                                    {
                                        W.Cast();
                                        Orbwalker.ForceTarget(target);
                                    }
                                }
                            }
                        }
                        break;
                    case GameObjectType.obj_AI_Minion:
                        if (MyManaManager.SpellFarm && Orbwalker.Mode == OrbwalkingMode.Laneclear)
                        {
                            if (Args.Target.IsMinion())
                            {
                                if (Me.ManaPercent() >= ClearMenu["FlowersDarius.ClearMenu.LaneClearManaPercent"].Value)
                                {
                                    if (ClearMenu["FlowersDarius.ClearMenu.LaneClearW"].Enabled && W.Ready)
                                    {
                                        var target = (Obj_AI_Minion)Args.Target;

                                        if (target != null && !target.IsDead && target.Health > 0 && target.Health < W.GetDamage(target))
                                        {
                                            W.Cast();
                                            Orbwalker.ForceTarget(target);
                                        }
                                    }
                                }
                            }
                            else if (Args.Target.IsMob())
                            {
                                UseItem();

                                if (Me.ManaPercent() >= ClearMenu["FlowersDarius.ClearMenu.JungleClearManaPercent"].Value)
                                {
                                    if (ClearMenu["FlowersDarius.ClearMenu.JungleClearW"].Enabled && W.Ready)
                                    {
                                        var target = (Obj_AI_Minion)Args.Target;

                                        if (target != null && !target.IsDead && target.Health > 0)
                                        {
                                            W.Cast();
                                            Orbwalker.ForceTarget(target);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case GameObjectType.obj_AI_Turret:
                    case GameObjectType.obj_Building:
                    case GameObjectType.obj_Barracks:
                    case GameObjectType.obj_BarracksDampener:
                    case GameObjectType.obj_HQ:
                        if (MyManaManager.SpellFarm && Orbwalker.Mode == OrbwalkingMode.Laneclear)
                        {
                            if (Me.ManaPercent() >= ClearMenu["FlowersDarius.ClearMenu.LaneClearManaPercent"].Value)
                            {
                                if (ClearMenu["FlowersDarius.ClearMenu.LaneClearW"].Enabled && W.Ready)
                                {
                                    var target = (Obj_AI_Turret)Args.Target;

                                    if (target != null && !target.IsDead)
                                    {
                                        W.Cast();
                                        Orbwalker.ForceTarget(target);
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
                if (Me.IsDead || MenuGUI.IsChatOpen() || MenuGUI.IsShopOpen())
                {
                    return;
                }

                if (DrawMenu["FlowersDarius.DrawMenu.Q"].Enabled && Q.Ready)
                {
                    Render.Circle(Me.Position, Q.Range, 23, Color.FromArgb(251, 0, 133));
                }

                if (DrawMenu["FlowersDarius.DrawMenu.E"].Enabled && E.Ready)
                {
                    Render.Circle(Me.Position, E.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersDarius.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.Position, R.Range, 23, Color.FromArgb(0, 255, 161));
                }

                Vector2 MePos = Vector2.Zero;
                Render.WorldToScreen(ObjectManager.GetLocalPlayer().ServerPosition, out MePos);

                if (DrawMenu["FlowersDarius.DrawMenu.ComboR"].Enabled)
                {
                    Render.Text(MePos.X - 57, MePos.Y + 68, Color.Orange,
                        "Combo R Status(" + ComboMenu["FlowersDarius.ComboMenu.R"].As<MenuKeyBind>().Key + "): " +
                        (ComboMenu["FlowersDarius.ComboMenu.R"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }

        internal static bool CanQHit(Obj_AI_Hero target)
        {
            if (target == null)
            {
                return false;
            }

            if (target.DistanceToPlayer() > Q.Range)
            {
                return false;
            }

            if (target.DistanceToPlayer() <= 240)
            {
                return false;
            }

            if (target.Health < R.GetDamage(target) && R.Ready && target.IsValidTarget(R.Range))
            {
                return false;
            }

            return true;
        }

        internal static int RMana => Me.GetSpell(SpellSlot.Q).Level == 0 || Me.GetSpell(SpellSlot.Q).Level == 3 ? 0 : 100;

        private static void UseItem()
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

        private static void UseYoumuu()
        {
            if (Me.CanUseItem("YoumusBlade"))
            {
                Me.UseItem("YoumusBlade");
            }
        }
    }
}