namespace Flowers_Vladimir.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.TargetSelector;

    using Flowers_Library;

    using Flowers_Vladimir.MyBase;

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
                Orbwalker.PreAttack += PreAttack;

                SpellBook.OnCastSpell += OnCastSpell;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
                Game.OnUpdate += OnUpdate;
                Gapcloser.OnGapcloser += OnGapcloser;
                Render.OnRender += OnRender;

                MyDamageIndicator.OnDamageIndicator();
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
                if (sender.IsMe)
                {
                    if (Args.Slot == SpellSlot.Q)
                    {
                        if (Orbwalker.Mode != OrbwalkingMode.None)
                        {
                            Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                        }
                    }
                    else if (Args.Slot == SpellSlot.E)
                    {
                        lastETime = Game.TickCount;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCastSpell." + ex);
            }
        }

        private static void PreAttack(object sender, PreAttackEventArgs Args)
        {
            try
            {
                if (Orbwalker.Mode == OrbwalkingMode.Combo)
                {
                    if (Me.Buffs.Any(x => x.Name == "vladimirqbuild" && x.Count == 2) &&
                        Me.GetSpell(SpellSlot.Q).CooldownEnd - Game.ClockTime <= 1.5)
                    {
                        Args.Cancel = true;
                    }

                    if (isQActive)
                    {
                        Args.Cancel = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.PreAttack." + ex);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            try
            {
                if (sender.IsMe)
                {
                    if (Args.SpellSlot == SpellSlot.Q)
                    {
                        lastQTime = Game.TickCount;
                    }
                    else if (Args.SpellSlot == SpellSlot.E)
                    {
                        lastETime = Game.TickCount;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnProcessSpellCast." + ex);
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

                if (isWActive || isEActive)
                {
                    Orbwalker.AttackingEnabled = false;
                    Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);
                }
                else
                {
                    Orbwalker.AttackingEnabled = true;
                }

                KillStealEvent();

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

                if (Orbwalker.Mode == OrbwalkingMode.Lasthit)
                {
                    LastHitEvent();
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
                if (KillStealMenu["FlowersVladimir.KillStealMenu.Q"].Enabled && Q.Ready &&
                    Game.TickCount - lastETime > 2000 + Game.Ping)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                    {
                        if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                        {
                            Q.CastOnUnit(target);
                            return;
                        }
                    }
                }

                if (KillStealMenu["FlowersVladimir.KillStealMenu.E"].Enabled && E.Ready &&
                    Game.TickCount - lastQTime > 1500 + Game.Ping)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(E.Range) && x.Health < E.GetDamage(x) && WillbeHit(x)))
                    {
                        if (target.IsValidTarget(E.Range) && !target.IsUnKillable())
                        {
                            if (isEActive)
                            {
                                E.Cast();
                            }
                            else
                            {
                                Me.SpellBook.CastSpell(SpellSlot.E);
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
                var target = TargetSelector.GetOrderedTargets(R.Range).FirstOrDefault();

                if (target != null && target.IsValidTarget() && !isEActive)
                {
                    if (ComboMenu["FlowersVladimir.ComboMenu.Ignite"].Enabled && IgniteSlot != SpellSlot.Unknown &&
                        Ignite.Ready && target.IsValidTarget(600) &&
                        (target.Health < MyExtraManager.GetComboDamage(target) && target.IsValidTarget(400) ||
                         target.Health < Me.GetIgniteDamage(target)))
                    {
                        Ignite.CastOnUnit(target);
                    }

                    if (ComboMenu["FlowersVladimir.ComboMenu.R"].As<MenuKeyBind>().Enabled && R.Ready && target.IsValidTarget(R.Range))
                    {
                        if (ComboMenu["FlowersVladimir.ComboMenu.RAlways"].Enabled)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.HitChance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                                return;
                            }
                        }

                        if (ComboMenu["FlowersVladimir.ComboMenu.RKillAble"].Enabled && target.Health < R.GetDamage(target))
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.HitChance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                                return;
                            }
                        }

                        if (ComboMenu["FlowersVladimir.ComboMenu.RCountHit"].As<MenuSliderBool>().Enabled)
                        {
                            if (R.CastIfWillHit(target,
                                ComboMenu["FlowersVladimir.ComboMenu.RCountHit"].As<MenuSliderBool>().Value))
                            {
                                return;
                            }
                        }

                        if (ComboMenu["FlowersVladimir.ComboMenu.RBurstCombo"].Enabled &&
                            Me.CountEnemyHeroesInRange(600) == 1 && target.IsValidTarget(R.Range))
                        {
                            if (target.Health <
                                R.GetDamage(target) + E.GetDamage(target) + W.GetDamage(target) * 2 +
                                Q.GetDamage(target) * 2)
                            {
                                var rPred = R.GetPrediction(target);

                                if (rPred.HitChance >= HitChance.High)
                                {
                                    R.Cast(rPred.CastPosition);
                                    return;
                                }
                            }
                        }
                    }

                    if (ComboMenu["FlowersVladimir.ComboMenu.E"].Enabled && E.Ready && target.IsValidTarget(E.Range))
                    {
                        if (GameObjects.EnemyHeroes.Any(x => x.IsValidTarget(E.Range) && WillbeHit(x)))
                        {
                            if (isEActive)
                            {
                                if (Game.TickCount - lastETime > 1050 + Game.Ping && !isWActive)
                                {
                                    E.Cast();
                                }
                            }
                            else
                            {
                                Me.SpellBook.CastSpell(SpellSlot.E);
                            }
                        }
                    }

                    if (ComboMenu["FlowersVladimir.ComboMenu.W"].Enabled && W.Ready && target.IsValidTarget(450f))
                    {
                        if (ComboMenu["FlowersVladimir.ComboMenu.WECharge"].Enabled)
                        {
                            if (isEActive && target.IsValidTarget(450f))
                            {
                                W.Cast();
                            }
                        }
                        else
                        {
                            if (!isQActive && target.IsValidTarget(W.Range) && Me.HealthPercent() > 40 &&
                                Me.GetSpell(SpellSlot.E).CooldownEnd - Game.ClockTime > 2.3 + Game.Ping / 100)
                            {
                                W.Cast();
                            }
                        }
                    }

                    if (isEActive || isWActive)
                    {
                        return;
                    }

                    if (ComboMenu["FlowersVladimir.ComboMenu.Q"].Enabled && Q.Ready && target.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(target);
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
                if (HarassMenu["FlowersVladimir.HarassMenu.Q"].Enabled && Q.Ready)
                {
                    var target =
                        TargetSelector.GetOrderedTargets(Q.Range)
                            .FirstOrDefault(
                                x => HarassMenu["FlowersVladimir.HarassMenu.Target_" + x.ChampionName].Enabled);

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        Q.CastOnUnit(target);
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
                if (isEActive)
                {
                    return;
                }

                if (ClearMenu["FlowersVladimir.ClearMenu.LaneClearE"].As<MenuSliderBool>().Enabled && E.Ready)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion()).ToArray();

                    if (minions.Any() && minions.Length >=
                        ClearMenu["FlowersVladimir.ClearMenu.LaneClearE"].As<MenuSliderBool>().Value)
                    {
                        Me.SpellBook.CastSpell(SpellSlot.E);
                        return;
                    }
                }

                if (ClearMenu["FlowersVladimir.ClearMenu.LaneClearQ"].Enabled && Q.Ready)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        foreach (var minion in minions.Where(x => x.IsValidTarget(Q.Range)))
                        {
                            if (ClearMenu["FlowersVladimir.ClearMenu.LaneClearQFrenzy"].Enabled && isQActive)
                            {
                                return;
                            }

                            if (ClearMenu["FlowersVladimir.ClearMenu.LaneClearQLH"].Enabled && minion.Health > Q.GetDamage(minion))
                            {
                                return;
                            }

                            if (minion.IsValidTarget(Q.Range))
                            {
                                Q.CastOnUnit(minion);
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
                if (isEActive)
                {
                    return;
                }

                var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob()).OrderBy(x => x.MaxHealth).ToArray();

                if (mobs.Any())
                {
                    foreach (var mob in mobs)
                    {
                        if (ClearMenu["FlowersVladimir.ClearMenu.JungleClearQ"].Enabled && Q.Ready && mob.IsValidTarget(Q.Range))
                        {
                            Q.CastOnUnit(mob);
                        }

                        if (ClearMenu["FlowersVladimir.ClearMenu.JungleClearE"].Enabled && E.Ready)
                        {
                            if (mob.isBigMob() && mob.IsValidTarget(E.Range))
                            {
                                Me.SpellBook.CastSpell(SpellSlot.E);
                            }
                            else
                            {
                                var eMobs = mobs.Where(x => x.IsValidTarget(E.Range)).ToArray();

                                if (eMobs.Length >= 2)
                                {
                                    Me.SpellBook.CastSpell(SpellSlot.E);
                                }
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
                if (isEActive)
                {
                    return;
                }

                if (Q.Ready)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        foreach (var minion in minions.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                        {
                            if (isQActive)
                            {
                                if (LastHitMenu["FlowersVladimir.LastHitMenu.QFrenzy"].Enabled &&
                                    minion.IsValidTarget(Q.Range))
                                {
                                    Q.CastOnUnit(minion);
                                }
                            }
                            else
                            {
                                if (LastHitMenu["FlowersVladimir.LastHitMenu.Q"].Enabled &&
                                    minion.IsValidTarget(Q.Range))
                                {
                                    Q.CastOnUnit(minion);
                                }
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

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            try
            {
                if (W.Ready && target != null && target.IsValidTarget(W.Range))
                {
                    switch (Args.Type)
                    {
                        case SpellType.Melee:
                            if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
                            {
                                W.Cast();
                            }
                            break;
                        case SpellType.Dash:
                        case SpellType.SkillShot:
                        case SpellType.Targeted:
                            {
                                W.Cast();
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnGapcloser." + ex);
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

                if (DrawMenu["FlowersVladimir.DrawMenu.Q"].Enabled && Q.Ready)
                {
                    Render.Circle(Me.Position, Q.Range, 23, Color.FromArgb(251, 0, 133));
                }

                if (DrawMenu["FlowersVladimir.DrawMenu.E"].Enabled && E.Ready)
                {
                    Render.Circle(Me.Position, E.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersVladimir.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.Position, R.Range, 23, Color.FromArgb(0, 255, 161));
                }

                Vector2 MePos = Vector2.Zero;
                Render.WorldToScreen(ObjectManager.GetLocalPlayer().ServerPosition, out MePos);

                if (DrawMenu["FlowersVladimir.DrawMenu.ComboR"].Enabled)
                {
                    Render.Text(MePos.X - 57, MePos.Y + 88, Color.Orange,
                        "Combo R(" + ComboMenu["FlowersVladimir.ComboMenu.R"].As<MenuKeyBind>().Key + "): " +
                        (ComboMenu["FlowersVladimir.ComboMenu.R"].As<MenuKeyBind>().Enabled ? "On" : "Off"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }

        private static bool WillbeHit(Obj_AI_Hero target)
        {
            if (target == null || !target.IsValidTarget())
            {
                return false;
            }

            var minions =
                GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && (x.IsMinion() || x.IsMob())).ToArray();

            if (minions.Any())
            {
                var targetPosition = target.ServerPosition;
                var fromPosition = ObjectManager.GetLocalPlayer().ServerPosition;
                var width = 40 + target.BoundingRadius * 0.65f;
                var rect = new MyPolygon.Rectangle(fromPosition, targetPosition, width);

                return
                    minions.Select(
                            minion => new MyPolygon.Circle(minion.ServerPosition.To2D(), minion.BoundingRadius * 0.65f))
                        .All(circ => !circ.Points.Any(p => rect.IsInside(p)));
            }

            return true;
        }
    }
}