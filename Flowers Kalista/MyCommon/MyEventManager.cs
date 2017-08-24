namespace Flowers_Kalista.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.TargetSelector;

    using Flowers_Kalista.MyBase;

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
                var healthPrediction = new Aimtec.SDK.Prediction.Health.HealthPrediction();

                Game.OnUpdate += OnUpdate;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
                Orbwalker.OnNonKillableMinion += OnNonKillableMinion;
                Orbwalker.PreAttack += OnPreAttack;
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
                AutoUltEvent();
                AutoEStealEvent();

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

        private static void KillStealEvent()
        {
            try
            {
                if (KillStealMenu["FlowersKalista.KillStealMenu.E"].Enabled && E.Ready)
                {
                    if (
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(E.Range) &&
                                x.Health <
                                E.GetRealDamage(x,
                                    MiscMenu["FlowersKalista.MiscMenu.EToler"].As<MenuSliderBool>().Enabled,
                                    MiscMenu["FlowersKalista.MiscMenu.EToler"].As<MenuSliderBool>().Value) &&
                                !x.IsUnKillable()).Any(target => target.IsValidTarget(E.Range)))
                    {
                        E.Cast();
                    }
                }

                if (KillStealMenu["FlowersKalista.KillStealMenu.Q"].Enabled && Q.Ready && Game.TickCount - lastETime > 1000)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q) && !x.IsUnKillable()))
                    {
                        if (target != null && target.IsValidTarget(Q.Range))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
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

        private static void AutoUltEvent()
        {
            try
            {
                if (Me.SpellBook.GetSpell(SpellSlot.R).Level > 0 && R.Ready)
                {
                    var ally = GameObjects.AllyHeroes.FirstOrDefault(
                        x => !x.IsMe && !x.IsDead && x.Buffs.Any(a => a.Name.ToLower().Contains("kalistacoopstrikeally")));

                    if (ally != null && ally.IsVisible && ally.DistanceToPlayer() <= R.Range)
                    {
                        if (MiscMenu["FlowersKalista.MiscMenu.AutoRAlly"].As<MenuSliderBool>().Enabled && Me.CountEnemyHeroesInRange(R.Range) > 0 &&
                            ally.CountEnemyHeroesInRange(R.Range) > 0 &&
                            ally.HealthPercent() <= MiscMenu["FlowersKalista.MiscMenu.AutoRAlly"].As<MenuSliderBool>().Value)
                        {
                            R.Cast();
                        }

                        if (MiscMenu["FlowersKalista.MiscMenu.Balista"].Enabled && ally.ChampionName == "Blitzcrank")
                        {
                            if (GameObjects.EnemyHeroes.Any(x => !x.IsDead && x.IsValidTarget() && x.Buffs.Any(a => a.Name.ToLower().Contains("rocketgrab2"))))
                            {
                                R.Cast();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.AutoUltEvent." + ex);
            }
        }

        private static void AutoEStealEvent()
        {
            try
            {
                if (E.Ready && MiscMenu["FlowersKalista.MiscMenu.AutoESteal"].Enabled)
                {
                    foreach (
                        var mob in
                        GameObjects.EnemyMinions.Where(
                            x =>
                                x != null && x.IsValidTarget(E.Range) && x.MaxHealth > 5 && x.isBigMob()))
                    {
                        if (mob.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) && mob.IsValidTarget(E.Range))
                        {
                            if (mob.Health < E.GetRealDamage(mob))
                            {
                                E.Cast();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.AutoEStealEvent." + ex);
            }
        }

        private static void ComboEvent()
        {
            try
            {
                ForcusAttack();

                var target = TargetSelector.GetTarget(Q.Range);

                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (ComboMenu["FlowersKalista.ComboMenu.Q"].Enabled && Q.Ready && target.IsValidTarget(Q.Range) && !target.IsValidAutoRange())
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }

                    if (ComboMenu["FlowersKalista.ComboMenu.E"].Enabled && E.Ready && target.IsValidTarget(E.Range) &&
                        Game.TickCount - lastETime > 500 + Game.Ping)
                    {
                        if (target.Health < E.GetRealDamage(target,
                                MiscMenu["FlowersKalista.MiscMenu.EToler"].As<MenuSliderBool>().Enabled,
                                MiscMenu["FlowersKalista.MiscMenu.EToler"].As<MenuSliderBool>().Value) &&
                            !target.IsUnKillable())
                        {
                            E.Cast();
                        }

                        if (ComboMenu["FlowersKalista.ComboMenu.ESlow"].Enabled && 
                            target.DistanceToPlayer() > Me.AttackRange + Me.BoundingRadius + 100 &&
                            target.IsValidTarget(E.Range))
                        {
                            var EKillMinion = GameObjects.Minions.Where(x => x.IsValidTarget(Me.GetFullAttackRange(x)))
                                .FirstOrDefault(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) &&
                                                     x.DistanceToPlayer() <= E.Range && x.Health < E.GetRealDamage(x));

                            if (EKillMinion != null && EKillMinion.IsValidTarget(E.Range) &&
                                target.IsValidTarget(E.Range))
                            {
                                E.Cast();
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

        private static void ForcusAttack()
        {
            try
            {
                if (GameObjects.EnemyHeroes.All(x => !x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + x.BoundingRadius)) &&
                    GameObjects.EnemyHeroes.Any(x => x.IsValidTarget((float)(Me.AttackRange * 1.65) + x.BoundingRadius)))
                {

                    var AttackUnit =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.GetFullAttackRange(x)))
                            .OrderBy(x => x.Distance(Game.CursorPos))
                            .FirstOrDefault();

                    if (AttackUnit != null && !AttackUnit.IsDead && AttackUnit.IsValidAutoRange())
                    {
                        Orbwalker.ForceTarget(AttackUnit);
                    }
                }
                else
                {
                    Orbwalker.ForceTarget(null);
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
                if (Me.ManaPercent() >= HarassMenu["FlowersKalista.HarassMenu.Mana"].Value)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(Q.Range) &&
                                HarassMenu["FlowersKalista.HarassMenu.HarassTarget_" + x.ChampionName].Enabled))
                    {
                        if (target != null && target.IsValidTarget(Q.Range))
                        {
                            if (HarassMenu["FlowersKalista.HarassMenu.Q"].Enabled && Q.Ready)
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.HitChance >= HitChance.High)
                                {
                                    Q.Cast(qPred.CastPosition);
                                }
                            }

                            if (E.Ready && Game.TickCount - lastETime > 500 + Game.Ping)
                            {
                                if (HarassMenu["FlowersKalista.HarassMenu.ESlow"].Enabled &&
                                    target.IsValidTarget(E.Range) &&
                                    target.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                                {
                                    var EKillMinion = GameObjects.Minions.Where(x => x.IsValidTarget(Me.GetFullAttackRange(x)))
                                        .FirstOrDefault(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")) &&
                                                             x.DistanceToPlayer() <= E.Range && x.Health < E.GetRealDamage(x));

                                    if (EKillMinion != null && EKillMinion.IsValidTarget(E.Range) &&
                                        target.IsValidTarget(E.Range))
                                    {
                                        E.Cast();
                                    }
                                }

                                if (HarassMenu["FlowersKalista.HarassMenu.ELeave"].As<MenuSliderBool>().Enabled &&
                                    target.DistanceToPlayer() >= 800 &&
                                    target.Buffs.Find(a => a.Name.ToLower().Contains("kalistaexpungemarker")).Count >=
                                    HarassMenu["FlowersKalista.HarassMenu.ELeave"].As<MenuSliderBool>().Value)
                                {
                                    E.Cast();
                                }
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
                if (MyManaManager.SpellHarass && Me.CountEnemyHeroesInRange(Q.Range) > 0)
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
                if (Me.ManaPercent() >= ClearMenu["FlowersKalista.ClearMenu.LaneClearMana"].Value && E.Ready)
                {
                    if (ClearMenu["FlowersKalista.ClearMenu.LaneClearE"].As<MenuSliderBool>().Enabled)
                    {
                        var KSCount =
                            GameObjects.EnemyMinions.Where(
                                    x => x.IsValidTarget(E.Range) && x.Team != GameObjectTeam.Neutral)
                                .Where(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                                .Count(x => x.Health < E.GetRealDamage(x));

                        if (KSCount > 0 &&
                            KSCount >= ClearMenu["FlowersKalista.ClearMenu.LaneClearE"].As<MenuSliderBool>().Value)
                        {
                            E.Cast();
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
                if (Me.ManaPercent() >= ClearMenu["FlowersKalista.ClearMenu.JungleClearMana"].Value)
                {
                    if (ClearMenu["FlowersKalista.ClearMenu.JungleClearE"].Enabled && E.Ready &&
                        Game.TickCount - lastETime > 500 + Game.Ping)
                    {
                        var KSCount =
                            GameObjects.EnemyMinions.Where(
                                    x => x.IsValidTarget(E.Range) && x.Team == GameObjectTeam.Neutral && x.MaxHealth > 5)
                                .Where(x => x.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
                                .Count(x => x.Health < E.GetRealDamage(x));

                        if (KSCount > 0)
                        {
                            E.Cast();
                        }
                    }

                    if (ClearMenu["FlowersKalista.ClearMenu.JungleClearQ"].Enabled && Q.Ready)
                    {
                        var qMob =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.Team == GameObjectTeam.Neutral && x.MaxHealth > 5)
                                .OrderByDescending(x => x.Health)
                                .FirstOrDefault();

                        if (qMob != null && qMob.IsValidTarget(Q.Range))
                        {
                            Q.Cast(qMob);
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
                if (Me.ManaPercent() >= LastHitMenu["FlowersKalista.LastHitMenu.Mana"].Value &&
                    LastHitMenu["FlowersKalista.LastHitMenu.E"].Enabled && E.Ready)
                {
                    if (GameObjects.EnemyMinions.Any(
                            x =>
                                x.IsValidTarget(E.Range) &&
                                x.Buffs.Any(
                                    a =>
                                        a.Name.ToLower().Contains("kalistaexpungemarker") &&
                                        x.Health < E.GetRealDamage(x))))
                    {
                        E.Cast();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.LastHitEvent." + ex);
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            try
            {
                if (Me.IsDead || !sender.IsMe)
                {
                    return;
                }

                switch (Args.SpellData.Name.ToLower())
                {
                    case "kalistaw":
                        lastWTime = Game.TickCount;
                        break;
                    case "kalistaexpunge":
                    case "kalistaexpungewrapper":
                    case "kalistadummyspell":
                        lastETime = Game.TickCount;
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnProcessSpellCast." + ex);
            }
        }

        private static void OnNonKillableMinion(object sender, NonKillableMinionEventArgs Args)
        {
            try
            {
                if (Me.IsDead || Me.IsRecalling() || !Me.CanMoveMent())
                {
                    return;
                }

                if (Orbwalker.Mode == OrbwalkingMode.Combo)
                {
                    return;
                }

                if (LastHitMenu["FlowersKalista.LastHitMenu.Auto"].Enabled && E.Ready &&
                    Me.ManaPercent() >= LastHitMenu["FlowersKalista.LastHitMenu.Mana"].Value)
                {
                    var minion = Args.Target as Obj_AI_Minion;

                    if (minion != null && minion.IsValidTarget(E.Range) && Me.CountEnemyHeroesInRange(600) == 0 &&
                        minion.Health < E.GetRealDamage(minion))
                    {
                        E.Cast();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnNonKillableMinion." + ex);
            }
        }

        private static void OnPreAttack(object sender, PreAttackEventArgs Args)
        {
            try
            {
                if (MiscMenu["FlowersKalista.MiscMenu.ForcusAttack"].Enabled && Me.CanMoveMent() && Args.Target != null &&
                    !Args.Target.IsDead && Args.Target.Health > 0)
                {
                    if (Orbwalker.Mode == OrbwalkingMode.Combo || Orbwalker.Mode == OrbwalkingMode.Mixed)
                    {
                        foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead &&
                                                                                  x.IsValidAutoRange() &&
                                                                                  x.Buffs.Any(
                                                                                      a =>
                                                                                          a.Name.ToLower()
                                                                                              .Contains(
                                                                                                  "kalistacoopstrikemarkally"))))
                        {
                            if (target != null && !target.IsDead && target.IsValidTarget(Me.GetFullAttackRange(target)))
                            {
                                Orbwalker.ForceTarget(target);
                            }
                        }
                    }
                    else if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                    {
                        foreach (var target in GameObjects.Minions.Where(x => !x.IsDead && x.IsEnemy &&
                                                      x.IsValidAutoRange() &&
                                                      x.Buffs.Any(
                                                          a =>
                                                              a.Name.ToLower()
                                                                  .Contains(
                                                                      "kalistacoopstrikemarkally"))))
                        {
                            if (target != null && !target.IsDead && target.IsValidTarget(Me.GetFullAttackRange(target)))
                            {
                                Orbwalker.ForceTarget(target);
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

        private static void OnPostAttack(object sender, PostAttackEventArgs Args)
        {
            try
            {
                Orbwalker.ForceTarget(null);

                if (Args.Target == null || Args.Target.IsDead || Args.Target.Health <= 0 || Me.IsDead || !Q.Ready)
                {
                    return;
                }

                switch (Args.Target.Type)
                {
                    case GameObjectType.obj_AI_Hero:
                        var target = Args.Target as Obj_AI_Hero;

                        if (target != null && !target.IsDead && target.IsValidTarget(Q.Range))
                        {
                            if (Orbwalker.Mode == OrbwalkingMode.Combo)
                            {
                                if (ComboMenu["FlowersKalista.ComboMenu.Q"].Enabled)
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                            }
                            else if (Me.ManaPercent() >= HarassMenu["FlowersKalista.HarassMenu.Mana"].Value &&
                                     (Orbwalker.Mode == OrbwalkingMode.Mixed ||
                                      Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellHarass))
                            {
                                if (HarassMenu["FlowersKalista.HarassMenu.Q"].Enabled)
                                {
                                    var qPred = Q.GetPrediction(target);

                                    if (qPred.HitChance >= HitChance.High)
                                    {
                                        Q.Cast(qPred.CastPosition);
                                    }
                                }
                            }
                        }
                        break;
                    case GameObjectType.obj_AI_Minion:
                        if (MyManaManager.SpellFarm && Orbwalker.Mode == OrbwalkingMode.Laneclear &&
                            Me.ManaPercent() >= ClearMenu["FlowersKalista.ClearMenu.JungleClearMana"].Value)
                        {
                            var mob = Args.Target as Obj_AI_Minion;

                            if (mob != null && !mob.IsDead && mob.IsValidTarget(Q.Range) && mob.Team == GameObjectTeam.Neutral)
                            {
                                if (ClearMenu["FlowersKalista.ClearMenu.JungleClearQ"].Enabled)
                                {
                                    Q.Cast(mob);
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

                if (DrawMenu["FlowersKalista.DrawMenu.Q"].Enabled && Q.Ready)
                {
                    Render.Circle(Me.Position, Q.Range, 23, Color.FromArgb(0, 255, 161));
                }

                if (DrawMenu["FlowersKalista.DrawMenu.W"].Enabled && W.Ready)
                {
                    Render.Circle(Me.Position, W.Range, 23, Color.FromArgb(86, 0, 255));
                }

                if (DrawMenu["FlowersKalista.DrawMenu.E"].Enabled && E.Ready)
                {
                    Render.Circle(Me.Position, E.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersKalista.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.Position, R.Range, 23, Color.FromArgb(251, 0, 133));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }
    }
}