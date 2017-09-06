namespace Flowers_Riven.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Events;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Riven.MyBase;

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
                Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
                Game.OnUpdate += OnUpdate;
                SpellBook.OnCastSpell += OnCastSpell;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
                Obj_AI_Base.OnPerformCast += OnPerformCast;
                Obj_AI_Base.OnProcessAutoAttack += OnProcessAutoAttack;
                Orbwalker.PostAttack += OnPostAttack;
                Render.OnRender += OnRender;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.Initializer." + ex);
            }
        }

        private static void OnProcessAutoAttack(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (!sender.IsMe || Me.IsDead)
            {
                return;
            }

            var ping = Game.Ping;

            if (ping > 50)
            {
                DelayAction.Queue(ping, () => { OnProcessAutoAttackDelayEvent(Args); });
            }
            else
            {
                DelayAction.Queue(50 - ping, () => { OnProcessAutoAttackDelayEvent(Args); });
            }
        }

        private static void OnProcessAutoAttackDelayEvent(Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            var target = Args.Target as AttackableUnit;

            if (target == null || target.IsDead || !target.IsValidTarget())
            {
                return;
            }

            switch (Orbwalker.Mode)
            {
                case OrbwalkingMode.Combo:
                    if (BurstMenu["FlowersRiven.BurstMenu.Key"].As<MenuKeyBind>().Enabled)
                    {
                        BurstAfterAttackEvent();
                    }
                    else
                    {
                        ComboAfterAttackEvent(target);
                    }
                    break;
                case OrbwalkingMode.Mixed:
                    HarassAfterAttackEvent(target);
                    break;
                case OrbwalkingMode.Laneclear:
                    ClearFarmAfterAttackEvent(target);
                    break;
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

                if (FleeMenu["FlowersRiven.FleeMenu.FleeKey"].As<MenuKeyBind>().Enabled && Me.CanMoveMent())
                {
                    FleeEvent();
                }

                if (FleeMenu["FlowersRiven.FleeMenu.WallJumpKey"].As<MenuKeyBind>().Enabled && Me.CanMoveMent())
                {
                    WallJumpEvent();
                }

                KillStealEvent();
                AutoUseEvent();

                switch (Orbwalker.Mode)
                {
                    case OrbwalkingMode.Combo:
                        if (BurstMenu["FlowersRiven.BurstMenu.Key"].As<MenuKeyBind>().Enabled)
                        {
                            BurstEvent();
                        }
                        else
                        {
                            ComboEvent();
                        }
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
                var selectTarget = TargetSelector.GetSelectedTarget();
                var orbTarget = Orbwalker.GetOrbwalkingTarget() as Obj_AI_Hero;

                if (selectTarget != null && !selectTarget.IsDead && selectTarget.IsValid && selectTarget.IsValidTarget())
                {
                    myTarget = selectTarget;
                }
                else if (orbTarget != null && !orbTarget.IsDead && orbTarget.IsValid && orbTarget.Type == GameObjectType.obj_AI_Hero &&
                    orbTarget.IsValidTarget())
                {
                    myTarget = orbTarget;
                }
                else
                {
                    myTarget = null;
                }

                if (Me.SpellBook.GetSpell(SpellSlot.W).Level > 0)
                {
                    W.Range = Me.HasBuff("RivenFengShuiEngine") ? 330f : 260f;
                }

                if (qStack != 0 && Game.TickCount - lastQTime > 3800)
                {
                    qStack = 0;
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

                if (FleeMenu["FlowersRiven.FleeMenu.W"].Enabled && W.Ready && Me.CountEnemyHeroesInRange(W.Range) > 0)
                {
                    W.Cast();
                }

                if (FleeMenu["FlowersRiven.FleeMenu.Q"].Enabled && Q.Ready && !Me.IsDashing())
                {
                    Q.Cast(Game.CursorPos);
                }

                if (FleeMenu["FlowersRiven.FleeMenu.E"].Enabled && E.Ready && (!Q.Ready && qStack == 0 || qStack == 2))
                {
                    E.Cast(Game.CursorPos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.FleeEvent." + ex);
            }
        }

        private static void WallJumpEvent()
        {
            if (true)
            {
                if (!Q.Ready)
                {
                    Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                }
                else
                {
                    if (qStack < 2)
                    {
                        if (Game.TickCount - lastQTime < 500)
                        {
                            return;
                        }

                        Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                        Q.Cast(Game.CursorPos);
                    }
                    else
                    {
                        Vector3 Position = Game.CursorPos;
                        Vector3 JumpPosition = Me.ServerPosition.Extend(Game.CursorPos, 65);//65 = Riven.BoundRadius 
                        if (JumpPosition.IsWall())
                        {
                            Position = JumpPosition;
                        }

                        Me.IssueOrder(OrderType.MoveTo, Position);

                        Vector3 EDashPosition = Me.ServerPosition.Extend(Game.CursorPos, 300);// 300 = E.Range - less
                        Vector3 QDashPosition = Me.ServerPosition.Extend(Game.CursorPos, 162);// 162 = Q.Range/2, this is real dash distance
                        Vector3 EQDashPosition = Me.ServerPosition.Extend(Game.CursorPos, 462);// 300 + 162

                        if (EDashPosition.IsWall() && E.Ready)
                        {
                            E.Cast(EDashPosition);
                        }

                        if (QDashPosition.IsWall() && Q.Ready)
                        {
                            Q.Cast(QDashPosition);
                        }

                        if (EQDashPosition.IsWall() && E.Ready && Q.Ready)
                        {
                            E.Cast(EQDashPosition);
                            Q.Cast(EQDashPosition);
                        }
                    }
                }
            }
        }

        private static void KillStealEvent()
        {
            try
            {
                if (KillStealMenu["FlowersRiven.KillStealMenu.R"].Enabled && R.Ready && isRActive)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(R.Range) &&
                                KillStealMenu["FlowersRiven.KillStealMenu.RTargetFor" + x.ChampionName].Enabled &&
                                x.Health < Me.GetSpellDamage(x, SpellSlot.R)))
                    {
                        if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                        {
                            R.Cast(target);
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
                KeepQAliveEvent();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.AutoUseEvent." + ex);
            }
        }

        private static void KeepQAliveEvent()
        {
            try
            {
                if (MiscMenu["FlowersRiven.MiscMenu.KeepQ"].Enabled && Me.HasBuff("RivenTriCleave"))
                {
                    if (Me.GetBuff("RivenTriCleave").EndTime - Game.ClockTime < 0.3)
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.KeepQAliveEvent." + ex);
            }
        }

        private static void ComboEvent()
        {
            try
            {
                var target = TargetSelector.GetTarget(800);

                if (target != null && target.IsValidTarget(800f))
                {
                    if (ComboMenu["FlowersRiven.ComboMenu.Ignite"].Enabled && IgniteSlot != SpellSlot.Unknown &&
                        Ignite.Ready && target.IsValidTarget(600) &&
                        (target.Health < MyExtraManager.GetComboDamage(target) && target.IsValidTarget(400) ||
                         target.Health < Me.GetIgniteDamage(target)))
                    {
                        Ignite.Cast(target);
                    }

                    if (ComboMenu["FlowersRiven.ComboMenu.Youmuu"].Enabled)
                    {
                        UseYoumuu();
                    }

                    if (ComboMenu["FlowersRiven.ComboMenu.R"].As<MenuKeyBind>().Enabled && R.Ready && !isRActive &&
                        ComboMenu["FlowersRiven.ComboMenu.RTargetFor" + target.ChampionName].Enabled &&
                        target.Health <= MyExtraManager.GetComboDamage(target) * 1.3 && target.IsValidTarget(600f))
                    {
                        MyExtraManager.R1Logic(target);
                    }

                    if (ComboMenu["FlowersRiven.ComboMenu.RMode"].As<MenuList>().Value != 3 && R.Ready && isRActive &&
                        ComboMenu["FlowersRiven.ComboMenu.RTargetFor" + target.ChampionName].Enabled)
                    {
                        MyExtraManager.R2Logic(target);
                    }

                    if (ComboMenu["FlowersRiven.ComboMenu.QGapcloser"].Enabled && Q.Ready &&
                        Game.TickCount - lastQTime > 1200 && !Me.IsDashing() && target.IsValidTarget(500f) &&
                        target.DistanceToPlayer() > Me.GetFullAttackRange(target) + 50 &&
                        Q.GetPrediction(target).UnitPosition.DistanceToPlayer() >
                        Me.GetFullAttackRange(target) + 50)
                    {
                        Q.Cast(target);
                        return;
                    }


                    if (ComboMenu["FlowersRiven.ComboMenu.EGapcloser"].Enabled && E.Ready && target.IsValidTarget(600) &&
                        target.DistanceToPlayer() > Me.GetFullAttackRange(target) + 50)
                    {
                        E.Cast(target.ServerPosition);
                    }


                    if (W.Ready && target.IsValidTarget(W.Range))
                    {
                        if (qStack == 0 && W.Cast())
                        {
                            return;
                        }

                        if (Q.Ready && qStack > 1 && W.Cast())
                        {
                            return;
                        }

                        if (Me.HasBuff("RivenFeint") && W.Cast())
                        {
                            return;
                        }

                        if (!target.IsFacing(Me))
                        {
                            W.Cast();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ComboEvent." + ex);
            }
        }

        private static void BurstEvent()
        {
            try
            {
                if (!BurstMenu["FlowersRiven.BurstMenu.Key"].As<MenuKeyBind>().Enabled)
                {
                    return;
                }

                var target = TargetSelector.GetSelectedTarget();

                if (target == null || !target.IsValidTarget())
                {
                    return;
                }

                if (BurstMenu["FlowersRiven.BurstMenu.Ignite"].Enabled && IgniteSlot != SpellSlot.Unknown &&
                    Ignite.Ready && target.IsValidTarget(600))
                {
                    Ignite.Cast(target);
                }

                switch (BurstMenu["FlowersRiven.BurstMenu.Mode"].As<MenuList>().Value)
                {
                    case 0:
                        BurstShyModeEvent(target, BurstMenu["FlowersRiven.BurstMenu.Flash"].Enabled);
                        break;
                    case 1:
                        BurstEQ3ModeEvent(target, BurstMenu["FlowersRiven.BurstMenu.Flash"].Enabled);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.BurstEvent." + ex);
            }
        }

        private static void BurstShyModeEvent(Obj_AI_Hero target, bool UseFlash)
        {
            try
            {
                if (E.Ready && R.Ready && W.Ready && !isRActive)
                {
                    if (target.IsValidTarget(E.Range + Me.BoundingRadius - 30))
                    {
                        E.Cast(target.ServerPosition);
                        DelayAction.Queue(10, () => R.Cast());
                        DelayAction.Queue(60, () => W.Cast());
                        DelayAction.Queue(150, () => Q.Cast(target.ServerPosition));
                        return;
                    }

                    if (UseFlash && FlashSlot != SpellSlot.Unknown && Flash.Ready)
                    {
                        if (target.IsValidTarget(E.Range + Me.BoundingRadius + 425 - 50))
                        {
                            E.Cast(target.ServerPosition);
                            DelayAction.Queue(10, () => R.Cast());
                            DelayAction.Queue(60, () => W.Cast());
                            DelayAction.Queue(61, () => Flash.Cast(target.ServerPosition));
                            DelayAction.Queue(150, () => Q.Cast(target.ServerPosition));
                        }
                    }
                }
                else
                {
                    if (W.Ready && target.IsValidTarget(W.Range))
                    {
                        W.Cast();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.BurstShyModeEvent." + ex);
            }
        }

        private static void BurstEQ3ModeEvent(Obj_AI_Hero target, bool UseFlash)
        {
            try
            {

                if (UseFlash && FlashSlot != SpellSlot.Unknown && Flash.Ready)
                {
                    if (target.IsValidTarget(E.Range + 425 + Q.Range - 150) && qStack > 0 && E.Ready && R.Ready && !isRActive && W.Ready)
                    {
                        E.Cast(target.ServerPosition);
                        DelayAction.Queue(10, () => R.Cast());
                        DelayAction.Queue(50, () => Flash.Cast(target.ServerPosition));
                        DelayAction.Queue(61, () => Q.Cast(target.ServerPosition));
                        DelayAction.Queue(62, UseItem);
                        DelayAction.Queue(70, () => W.Cast());
                        DelayAction.Queue(71, () => R.Cast(target.ServerPosition));
                        return;
                    }

                    if (qStack < 2 && Game.TickCount - lastQTime >= 850)
                    {
                        Q.Cast(Game.CursorPos);
                    }
                }
                else
                {
                    if (target.IsValidTarget(E.Range + Q.Range - 150) && qStack == 2 && E.Ready && R.Ready && !isRActive && W.Ready)
                    {
                        E.Cast(target.ServerPosition);
                        DelayAction.Queue(10, () => R.Cast());
                        DelayAction.Queue(50, () => Q.Cast(target.ServerPosition));
                        DelayAction.Queue(61, UseItem);
                        DelayAction.Queue(62, () => W.Cast());
                        DelayAction.Queue(70, () => R.Cast(target.ServerPosition));
                        return;
                    }

                    if (target.IsValidTarget(E.Range + Q.Range + Q.Range + Q.Range))
                    {
                        if (qStack < 2 && Game.TickCount - lastQTime >= 850)
                        {
                            Q.Cast(Game.CursorPos);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.BurstEQ3ModeEvent." + ex);
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

                var target = TargetSelector.GetTarget(E.Range + Me.BoundingRadius);

                if (target.IsValidTarget())
                {
                    if (HarassMenu["FlowersRiven.HarassMenu.Mode"].As<MenuList>().Value == 0)
                    {
                        if (E.Ready && qStack == 2)
                        {
                            var pos = Me.ServerPosition + (Me.ServerPosition - target.ServerPosition).Normalized() * E.Range;

                            if (pos != Vector3.Zero)
                            {
                                E.Cast(pos);
                            }
                        }

                        if (Q.Ready && qStack == 2)
                        {
                            var pos = Me.ServerPosition + (Me.ServerPosition - target.ServerPosition).Normalized() * E.Range;

                            if (pos != Vector3.Zero)
                            {
                                DelayAction.Queue(100, () => Q.Cast(pos));
                            }
                        }

                        if (W.Ready && target.IsValidTarget(W.Range) && qStack == 1)
                        {
                            W.Cast();
                        }

                        if (Q.Ready)
                        {
                            if (qStack == 0)
                            {
                                MyExtraManager.CastQ(target);
                                Orbwalker.ForceTarget(target);
                            }

                            if (qStack == 1 && Environment.TickCount - lastQTime > 600)
                            {
                                MyExtraManager.CastQ(target);
                                Orbwalker.ForceTarget(target);
                            }
                        }
                    }
                    else
                    {
                        if (E.Ready && HarassMenu["FlowersRiven.HarassMenu.E"].Enabled &&
                            target.DistanceToPlayer() <= E.Range + (Q.Ready ? Q.Range : Me.AttackRange))
                        {
                            E.Cast(target.ServerPosition);
                        }

                        if (Q.Ready && HarassMenu["FlowersRiven.HarassMenu.Q"].Enabled &&
                            target.IsValidTarget(Q.Range) && qStack == 0 && Game.TickCount - lastQTime > 500)
                        {
                            MyExtraManager.CastQ(target);
                            Orbwalker.ForceTarget(target);
                        }

                        if (W.Ready && HarassMenu["FlowersRiven.HarassMenu.W"].Enabled &&
                            target.IsValidTarget(W.Range) && (!Q.Ready || qStack == 1))
                        {
                            W.Cast();
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
                if (Me.IsUnderEnemyTurret() || Game.TickCount - lastCastTime < 1200)
                {
                    return;
                }

                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(400) && x.Health > 5).ToArray();

                if (minions.Any())
                {
                    if (ClearMenu["FlowersRiven.ClearMenu.LaneClearItem"].Enabled && minions.Length >= 3)
                    {
                        UseItem();
                    }

                    if (ClearMenu["FlowersRiven.ClearMenu.LaneClearQSmart"].Enabled && Q.Ready &&
                        minions.Count(x => Me.IsFacing(x) && x.IsValidTarget()) >= 3)
                    {
                        var minion = minions.FirstOrDefault();

                        if (minion != null)
                        {
                            Q.Cast(minion.ServerPosition);
                        }
                    }

                    if (ClearMenu["FlowersRiven.ClearMenu.LaneClearW"].As<MenuSliderBool>().Enabled && W.Ready)
                    {
                        if (minions.Count(x => x.IsValidTarget(W.Range)) >=
                            ClearMenu["FlowersRiven.ClearMenu.LaneClearW"].As<MenuSliderBool>().Value)
                        {
                            W.Cast();
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
                var mobs =
                    GameObjects.EnemyMinions.Where(
                        x =>
                            x.IsValidTarget(Q.Range + Me.AttackRange + Me.BoundingRadius) && x.Health > 5 &&
                            x.Team == GameObjectTeam.Neutral).ToArray();

                if (mobs.Any())
                {
                    var mob = mobs.OrderBy(x => x.MaxHealth).FirstOrDefault();

                    if (mob != null)
                    {
                        if (ClearMenu["FlowersRiven.ClearMenu.JungleClearItem"].Enabled && mob.IsValidTarget(400))
                        {
                            UseItem();
                        }

                        if (ClearMenu["FlowersRiven.ClearMenu.JungleClearE"].Enabled && E.Ready)
                        {
                            if (!Q.Ready && qStack == 0 && !W.Ready)
                            {
                                E.Cast(Game.CursorPos);
                            }

                            if (!mobs.Any(x => x.DistanceToPlayer() <= E.Range))
                            {
                                E.Cast(mob.ServerPosition);
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

        private static void OnCastSpell(Obj_AI_Base sender, SpellBookCastSpellEventArgs Args)
        {
            try
            {
                if (sender.IsMe)
                {
                    if (Args.Slot == SpellSlot.Q || Args.Slot == SpellSlot.W || Args.Slot == SpellSlot.E)
                    {
                        lastCastTime = Game.TickCount;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCastSpell." + ex);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            try
            {
                if (sender.IsMe && Orbwalker.Mode == OrbwalkingMode.Combo)
                {
                    RivenDoubleCastEvent(Args.SpellData.Name, BurstMenu["FlowersRiven.BurstMenu.Key"].As<MenuKeyBind>().Enabled);
                }
                else if (sender.IsEnemy && sender.Type == GameObjectType.obj_AI_Hero &&
                    !string.IsNullOrEmpty(Args.SpellData.Name) && !Args.SpellData.Name.Contains("attack"))
                {
                    if (EvadeMenu["FlowersRiven.EvadeMenu.Use E"].Enabled && E.Ready)
                    {
                        if (Args.SpellData.TargettingType == 1 && Args.Target != null && Args.Target.IsMe)
                        {
                            E.Cast(Game.CursorPos);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnProcessSpellCast." + ex);
            }
        }

        private static void RivenDoubleCastEvent(string spellName, bool isBurstMode)
        {
            var target = myTarget.IsValidTarget(600f) ? myTarget : TargetSelector.GetTarget(600);

            if (target != null && target.IsValidTarget())
            {
                switch (spellName.ToLower())
                {
                    case "itemtiamatcleave":
                        if (W.Ready && target.IsValidTarget(W.Range))
                        {
                            W.Cast();
                        }
                        else if (Q.Ready && target.IsValidTarget(400))
                        {
                            MyExtraManager.CastQ(target);
                        }
                        break;
                    case "rivenmartyr":
                        if (isBurstMode)
                        {
                            if (R.Ready && isRActive)
                            {
                                R.Cast(target.ServerPosition);
                            }
                            else if (Q.Ready && target.IsValidTarget(400))
                            {
                                MyExtraManager.CastQ(target);
                            }
                        }
                        else
                        {
                            if (Q.Ready && target.IsValidTarget(400))
                            {
                                MyExtraManager.CastQ(target);
                            }
                            else if (ComboMenu["FlowersRiven.ComboMenu.R"].As<MenuKeyBind>().Enabled && R.Ready && !isRActive &&
                                     ComboMenu["FlowersRiven.ComboMenu.RTargetFor" + target.ChampionName].Enabled)
                            {
                                MyExtraManager.R1Logic(target);
                            }
                        }
                        break;
                    case "rivenfeint":
                        if (isBurstMode)
                        {
                            if (BurstMenu["FlowersRiven.BurstMenu.Mode"].As<MenuList>().Value == 0)
                            {
                                UseYoumuu();

                                if (R.Ready && !isRActive)
                                {
                                    R.Cast();
                                }
                            }
                        }
                        else
                        {
                            if (ComboMenu["FlowersRiven.ComboMenu.R"].As<MenuKeyBind>().Enabled && R.Ready && !isRActive &&
                                target.IsValidTarget(500f) &&
                                ComboMenu["FlowersRiven.ComboMenu.RTargetFor" + target.ChampionName].Enabled)
                            {
                                MyExtraManager.R1Logic(target);
                            }
                        }
                        break;
                    case "rivenfengshuiengine":
                        if (!isBurstMode)
                        {
                            if (ComboMenu["FlowersRiven.ComboMenu.WCancel"].Enabled && W.Ready && target.IsValidTarget(W.Range))
                            {
                                W.Cast();
                            }
                        }
                        break;
                    case "rivenizunablade":
                        if (Q.Ready && target.IsValidTarget(400))
                        {
                            Q.Cast(target.ServerPosition);
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        private static void OnPerformCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            try
            {
                if (sender.IsMe && Orbwalker.Mode == OrbwalkingMode.Combo)
                {
                    RivenDoubleCastEvent(Args.SpellData.Name, BurstMenu["FlowersRiven.BurstMenu.Key"].As<MenuKeyBind>().Enabled);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnPerformCast." + ex);
            }
        }

        private static void OnPlayAnimation(Obj_AI_Base sender, Obj_AI_BasePlayAnimationEventArgs Args)
        {
            try
            {
                if (sender.IsMe)
                {
                    //var time = 0;
                    bool cancel = false;

                    switch (Args.Animation)
                    {
                        case "Spell1a":
                            cancel = true;//MiscMenu["FlowersRiven.MiscMenu.Q1Delay"].Value;
                            qStack = 1;
                            lastQTime = Game.TickCount;
                            break;
                        case "Spell1b":
                            cancel = true;//MiscMenu["FlowersRiven.MiscMenu.Q2Delay"].Value;
                            qStack = 2;
                            lastQTime = Game.TickCount;
                            break;
                        case "Spell1c":
                            cancel = true;//MiscMenu["FlowersRiven.MiscMenu.Q3Delay"].Value;
                            qStack = 0;
                            lastQTime = Game.TickCount;
                            break;
                        default:
                            cancel = false;
                            break;
                    }

                    if (FleeMenu["FlowersRiven.FleeMenu.FleeKey"].As<MenuKeyBind>().Enabled ||
                        FleeMenu["FlowersRiven.FleeMenu.WallJumpKey"].As<MenuKeyBind>().Enabled)
                    {
                        return;
                    }

                    if (cancel == true)
                    {
                        //var delay1 = 400 + delay3;
                        //var delay2 = Game.Ping + delay3;
                        var delay3 = 1000 / (ObjectManager.GetLocalPlayer().AttackSpeedMod * 3.75);
                        //var delay4 = ObjectManager.GetLocalPlayer().AttackSpeedMod * 12;
                        //var delay5 = Game.Ping - Me.GetSpell(SpellSlot.Q).Level * 2;
                        //var delay6 = ObjectManager.GetLocalPlayer().AttackSpeedMod;
                        //var delay7 = 1 / ObjectManager.GetLocalPlayer().AttackSpeedMod;
                        //var delay8 = ObjectManager.GetLocalPlayer().AttackDelay;
                        //var delay9 = 1 / ObjectManager.GetLocalPlayer().AttackDelay;
                        //var delay10 = ObjectManager.GetLocalPlayer().AttackCastDelay;
                        //var delay11 = 1 / ObjectManager.GetLocalPlayer().AttackCastDelay;

                        //Console.WriteLine("Delay1: " + delay1);
                        //Console.WriteLine("Delay2: " + delay2);
                        //Console.WriteLine("Delay3: " + delay3);
                        //Console.WriteLine("Delay4: " + delay4);
                        //Console.WriteLine("Delay5: " + delay5);
                        //Console.WriteLine("Delay6: " + delay6);
                        //Console.WriteLine("Delay7: " + delay7);
                        //Console.WriteLine("Delay8: " + delay8);
                        //Console.WriteLine("Delay9: " + delay9);
                        //Console.WriteLine("Delay10: " + delay10);
                        //Console.WriteLine("Delay11: " + delay11);

                        var time = qStack != 0
                            ? (int) (delay3 + (Game.Ping > 50 ? Game.Ping : 50) + Me.GetSpell(SpellSlot.Q).Level * 2)
                            : (int)
                            (delay3 + (Game.Ping > 50 ? Game.Ping : 50) +
                             ((Game.Ping > 50 ? Game.Ping : 50) + Me.GetSpell(SpellSlot.Q).Level * 2));

                       // Console.WriteLine(time);
                        if (MiscMenu["FlowersRiven.MiscMenu.SemiCancel"].Enabled || Orbwalker.Mode != OrbwalkingMode.None)
                        {
                            if (MiscMenu["FlowersRiven.MiscMenu.CalculatePing"].Enabled)
                            {
                                if (time - Game.Ping > 0)
                                {
                                    DelayAction.Queue(time - Game.Ping, Cancel);
                                }
                                else
                                {
                                    DelayAction.Queue(1, Cancel);
                                }
                            }
                            else
                            {
                                DelayAction.Queue(time, Cancel);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnPlayAnimation." + ex);
            }
        }

        private static void Cancel()
        {
            try
            {
                //Me.ServerPosition.Extend(Game.CursorPos, Me.BoundingRadius * 2);
                MyOrbwalkerManager.Reset();

                if (myTarget != null && myTarget.IsValidTarget())
                {
                    var pos = Me.ServerPosition.Extend(myTarget.ServerPosition, Me.BoundingRadius * 2);
                    //var pos = Me.ServerPosition +
                    //          (Me.ServerPosition - myTarget.ServerPosition).Normalized() *
                    //          (Me.BoundingRadius * 2 + myTarget.BoundingRadius);
                    //var newPos = new Vector3(pos.X, pos.Y, myTarget.ServerPosition.Z);

                    Me.IssueOrder(OrderType.MoveTo, pos);
                    DelayAction.Queue(10, () => { Orbwalker.Orbwalk(); });
                }
                else
                {
                    Orbwalker.Move(Me.ServerPosition.Extend(Game.CursorPos, Me.BoundingRadius * 2));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.Cancel." + ex);
            }
        }

        private static void OnPostAttack(object sender, PostAttackEventArgs Args)
        {
            try
            {
                Orbwalker.ForceTarget(null);
                
                var target = Args.Target;// as Obj_AI_Base;

                if (target == null || target.IsDead || !target.IsValidTarget())
                {
                    return;
                }

                switch (Orbwalker.Mode)
                {
                    case OrbwalkingMode.Combo:
                        if (BurstMenu["FlowersRiven.BurstMenu.Key"].As<MenuKeyBind>().Enabled)
                        {
                            BurstAfterAttackEvent();
                        }
                        else
                        {
                            if (ComboMenu["FlowersRiven.ComboMenu.Item"].Enabled)
                            {
                                UseItem();
                            }

                            ComboAfterAttackEvent(target);
                        }
                        break;
                    case OrbwalkingMode.Mixed:
                        HarassAfterAttackEvent(target);
                        break;
                    case OrbwalkingMode.Laneclear:
                        ClearFarmAfterAttackEvent(target);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnPostAttack." + ex);
            }
        }

        private static void BurstAfterAttackEvent()
        {
            try
            {
                var target = TargetSelector.GetSelectedTarget();

                if (target == null || !target.IsValidTarget())
                {
                    return;
                }

                UseItem();

                if (R.Ready && isRActive)
                {
                    R.Cast(target.ServerPosition);
                    return;
                }

                if (Q.Ready && MyExtraManager.CastQ(target))
                {
                    return;
                }

                if (W.Ready && target.IsValidTarget(W.Range) && W.Cast())
                {
                    return;
                }

                if (E.Ready)
                {
                    E.Cast(target.ServerPosition);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.BurstAfterAttackEvent." + ex);
            }
        }

        private static void ComboAfterAttackEvent(AttackableUnit orbTarget)
        {
            try
            {
                Obj_AI_Hero target = null;

                if (myTarget.IsValidTarget(600))
                {
                    target = myTarget;
                }
                else if (orbTarget is Obj_AI_Hero)
                {
                    target = (Obj_AI_Hero)orbTarget;
                }

                if (target != null && target.IsValidTarget(400))
                {
                    if (Q.Ready && target.IsValidTarget(400))
                    {
                        MyExtraManager.CastQ(target);
                        return;
                    }

                    if (ComboMenu["FlowersRiven.ComboMenu.RMode"].As<MenuList>().Value != 3 &&
                        R.Ready && isRActive && qStack == 2 && Q.Ready &&
                        ComboMenu["FlowersRiven.ComboMenu.RMode"].As<MenuList>().Value != 1 &&
                        MyExtraManager.R2Logic(target))
                    {
                        return;
                    }

                    if (ComboMenu["FlowersRiven.ComboMenu.WCancel"].Enabled && W.Ready &&
                        target.IsValidTarget(W.Range) && !target.HaveShiled())
                    {
                        W.Cast();
                        return;
                    }

                    if (!Q.Ready && !W.Ready && E.Ready && target.IsValidTarget(400))
                    {
                        E.Cast(target.ServerPosition);
                        return;
                    }

                    if (ComboMenu["FlowersRiven.ComboMenu.R"].As<MenuKeyBind>().Enabled &&
                        R.Ready && !isRActive &&
                        ComboMenu["FlowersRiven.ComboMenu.RTargetFor" + target.ChampionName].Enabled)
                    {
                        MyExtraManager.R1Logic(target);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ComboAfterAttackEvent." + ex);
            }
        }

        private static void HarassAfterAttackEvent(AttackableUnit orbTarget)
        {
            try
            {
                Obj_AI_Hero target = null;

                if (myTarget.IsValidTarget())
                {
                    target = myTarget;
                }
                else if (orbTarget is Obj_AI_Hero)
                {
                    target = (Obj_AI_Hero)orbTarget;
                }

                if (target == null || !target.IsValidTarget())
                {
                    return;
                }

                if (HarassMenu["FlowersRiven.HarassMenu.Mode"].As<MenuList>().Value == 0)
                {
                    if (qStack == 1)
                    {
                        MyExtraManager.CastQ(target);
                    }
                }
                else
                {
                    if (HarassMenu["FlowersRiven.HarassMenu.Q"].Enabled && Q.Ready)
                    {
                        MyExtraManager.CastQ(target);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.HarassAfterAttackEvent." + ex);
            }
        }

        private static void ClearFarmAfterAttackEvent(AttackableUnit target)
        {
            try
            {
                if (!MyManaManager.SpellFarm)
                {
                    return;
                }

                if (target.IsBuilding())
                {
                    if (ClearMenu["FlowersRiven.ClearMenu.LaneClearQTurret"].Enabled && Q.Ready && Me.CountEnemyHeroesInRange(800) == 0)
                    {
                        Q.Cast(target.ServerPosition);
                    }
                }
                else if (target.Type == GameObjectType.obj_AI_Minion)
                {
                    if (target.IsMinion())
                    {
                        if (ClearMenu["FlowersRiven.ClearMenu.LaneClearQ"].Enabled && Q.Ready)
                        {
                            var minions =
                                GameObjects.EnemyMinions.Where(x => x.IsValidTarget(400, false, false, target.ServerPosition) && x.IsMinion()).ToArray();

                            if (minions.Length >= 2)
                            {
                                Q.Cast(target.ServerPosition);
                            }
                        }
                    }
                    else if (target.IsMob())
                    {
                        var mob = target as Obj_AI_Minion;

                        if (mob != null && mob.IsValidTarget() && target.Health > Me.GetAutoAttackDamage(mob) * 2)
                        {
                            if (ClearMenu["FlowersRiven.ClearMenu.JungleClearQ"].Enabled && Q.Ready)
                            {
                                Q.Cast(mob.ServerPosition);
                            }
                            else if (ClearMenu["FlowersRiven.ClearMenu.JungleClearW"].Enabled && W.Ready && target.IsValidTarget(W.Range))
                            {
                                W.Cast();
                            }
                            else if (ClearMenu["FlowersRiven.ClearMenu.JungleClearE"].Enabled && E.Ready)
                            {
                                E.Cast(mob.ServerPosition);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ClearFarmAfterAttackEvent." + ex);
            }
        }

        private static void OnRender()
        {
            try
            {
                if (ObjectManager.GetLocalPlayer().IsDead || ObjectManager.GetLocalPlayer().IsRecalling() ||
                MenuGUI.IsChatOpen() || MenuGUI.IsShopOpen())
                {
                    return;
                }

                Vector2 MePos = Vector2.Zero;
                Render.WorldToScreen(ObjectManager.GetLocalPlayer().ServerPosition, out MePos);

                if (DrawMenu["FlowersRiven.DrawMenu.E"].Enabled && E.Ready)
                {
                    Render.Circle(Me.ServerPosition, E.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersRiven.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.ServerPosition, R.Range, 23, Color.FromArgb(251, 0, 133));
                }

                if (DrawMenu["FlowersRiven.DrawMenu.ComboR"].Enabled)
                {
                    Render.Text(MePos.X - 57, MePos.Y + 68, Color.Orange,
                        "Combo R(" + ComboMenu["FlowersRiven.ComboMenu.R"].As<MenuKeyBind>().Key + "): " +
                        (ComboMenu["FlowersRiven.ComboMenu.R"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }

                if (DrawMenu["FlowersRiven.DrawMenu.Burst"].Enabled)
                {
                    var target = TargetSelector.GetSelectedTarget();

                    Render.Text(MePos.X - 57, MePos.Y + 88, Color.Orange,
                        "Burst Combo(" + BurstMenu["FlowersRiven.BurstMenu.Key"].As<MenuKeyBind>().Key + "): " +
                        (BurstMenu["FlowersRiven.BurstMenu.Key"].As<MenuKeyBind>().Enabled ? (target != null ? "Lock: " + target.ChampionName : "No Target") : "Off"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }

        private static void UseItem()
        {
            if (Me.CanUseItem(TiamatHrdra))
            {
                Me.UseItem(TiamatHrdra);
            }

            if (Me.CanUseItem(Titanic))
            {
                Me.UseItem(Titanic);
            }
        }

        private static void UseYoumuu()
        {
            if (Me.CanUseItem(Youmuu))
            {
                Me.UseItem(Youmuu);
            }
        }
    }
}
