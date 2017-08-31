namespace Flowers_Ryze.MyCommon
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

    using Flowers_Ryze.MyBase;

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
                SpellBook.OnCastSpell += OnCastSpell;
                GameObject.OnCreate += OnCreate;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
                Orbwalker.PreAttack += OnPreAttack;
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
                SetCoolDownTime();

                if (Me.IsDead || Me.IsRecalling())
                {
                    return;
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

        private static void SetCoolDownTime()
        {
            try
            {
                R.Range = Me.GetSpell(SpellSlot.R).Level > 0 ? Me.GetSpell(SpellSlot.R).Level * 1500f : 0f;

                QcdEnd = Me.GetSpell(SpellSlot.Q).CooldownEnd;

                WcdEnd = Me.GetSpell(SpellSlot.W).CooldownEnd;
                EcdEnd = Me.GetSpell(SpellSlot.E).CooldownEnd;

                Qcd = Me.GetSpell(SpellSlot.Q).Level > 0 ? CheckCD(QcdEnd) : -1;
                Wcd = Me.GetSpell(SpellSlot.W).Level > 0 ? CheckCD(WcdEnd) : -1;
                Ecd = Me.GetSpell(SpellSlot.E).Level > 0 ? CheckCD(EcdEnd) : -1;

                CanShield = Orbwalker.Mode == OrbwalkingMode.Combo &&
                            (ComboMenu["FlowersRyze.ComboMenu.Mode"].As<MenuList>().Value == 0 &&
                             ComboMenu["FlowersRyze.ComboMenu.ShieldHP"].As<MenuSliderBool>().Enabled &&
                             Me.HealthPercent() <=
                             ComboMenu["FlowersRyze.ComboMenu.ShieldHP"].As<MenuSliderBool>().Value ||
                             ComboMenu["FlowersRyze.ComboMenu.Mode"].As<MenuList>().Value == 1) &&
                            Me.GetSpell(SpellSlot.Q).Level > 0 && Me.GetSpell(SpellSlot.W).Level > 0 &&
                            Me.GetSpell(SpellSlot.E).Level > 0;

                Q.Collision = Orbwalker.Mode != OrbwalkingMode.Combo ||
                    ComboMenu["FlowersRyze.ComboMenu.QSmart"].Enabled == false || CanShield == false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.SetCoolDownTime." + ex);
            }
        }

        private static float CheckCD(float Expires)
        {
            try
            {
                var time = Expires - Game.ClockTime;

                if (time < 0)
                {
                    time = 0;

                    return time;
                }

                return time;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.CheckCD." + ex);
                return -1;
            }
        }

        private static void KillStealEvent()
        {
            try
            {
                if (Me.CountEnemyHeroesInRange(Q.Range) == 0)
                {
                    return;
                }

                if (KillStealMenu["FlowersRyze.KillStealMenu.Q"].Enabled && Q.Ready)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(Q.Range) &&
                                x.Health < x.GetRealQDamage(true) &&
                                !x.IsUnKillable()))
                    {
                        if (target != null && target.IsValidTarget(Q.Range))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.High)
                            {
                                Q.Cast(qPred.UnitPosition);
                                return;
                            }
                        }
                    }
                }

                if (KillStealMenu["FlowersRyze.KillStealMenu.W"].Enabled && W.Ready)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(W.Range) &&
                                x.Health < W.GetDamage(x) &&
                                !x.IsUnKillable()))
                    {
                        if (target != null && target.IsValidTarget(W.Range))
                        {
                            W.CastOnUnit(target);
                            return;
                        }
                    }
                }

                if (KillStealMenu["FlowersRyze.KillStealMenu.E"].Enabled && E.Ready)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(E.Range) &&
                                x.Health < E.GetDamage(x) &&
                                !x.IsUnKillable()))
                    {
                        if (target != null && target.IsValidTarget(E.Range))
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

        private static void ComboEvent()
        {
            try
            {
                var target = TargetSelector.GetTarget(Q.Range);

                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (ComboMenu["FlowersRyze.ComboMenu.Ignite"].Enabled && IgniteSlot != SpellSlot.Unknown &&
                        Ignite.Ready && target.IsValidTarget(600) &&
                        (target.Health < MyExtraManager.GetComboDamage(target) && target.IsValidTarget(400) ||
                         target.Health < Me.GetIgniteDamage(target)))
                    {
                        Ignite.Cast(target);
                    }

                    if (Game.TickCount - LastCastTime > 500)
                    {
                        switch (ComboMenu["FlowersRyze.ComboMenu.Mode"].As<MenuList>().Value)
                        {
                            case 0:
                                NormalCombo(target, ComboMenu["FlowersRyze.ComboMenu.Q"].Enabled,
                                    ComboMenu["FlowersRyze.ComboMenu.W"].Enabled,
                                    ComboMenu["FlowersRyze.ComboMenu.E"].Enabled);
                                break;
                            case 1:
                                ShieldCombo(target, ComboMenu["FlowersRyze.ComboMenu.Q"].Enabled,
                                    ComboMenu["FlowersRyze.ComboMenu.W"].Enabled,
                                    ComboMenu["FlowersRyze.ComboMenu.E"].Enabled);
                                break;
                            default:
                                BurstCombo(target, ComboMenu["FlowersRyze.ComboMenu.Q"].Enabled,
                                    ComboMenu["FlowersRyze.ComboMenu.W"].Enabled,
                                    ComboMenu["FlowersRyze.ComboMenu.E"].Enabled);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ComboEvent." + ex);
            }
        }

        private static void NormalCombo(Obj_AI_Hero target, bool useQ, bool useW, bool useE)
        {
            try
            {
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (Game.TickCount - LastCastTime > 500)
                    {
                        if (CanShield)
                        {
                            if (useQ && Q.Ready &&
                                (FullStack || NoStack ||
                                 HalfStack && !W.Ready && Wcd > 1 && !E.Ready && Ecd > 1) &&
                                target.IsValidTarget(Q.Range))
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.HitChance >= HitChance.VeryHigh)
                                {
                                    Q.Cast(qPred.UnitPosition);
                                }
                            }

                            if (useW && W.Ready && (!FullStack || HaveShield) &&
                                target.IsValidTarget(W.Range) &&
                                (Ecd >= 2 || target.HasBuff("ryzee")))
                            {
                                W.CastOnUnit(target);
                            }

                            if (useE && E.Ready && (!FullStack || HaveShield) &&
                                target.IsValidTarget(E.Range))
                            {
                                if (NoStack)
                                {
                                    E.CastOnUnit(target);
                                }

                                var minions =
                                    GameObjects.EnemyMinions.Where(
                                            x => x.IsValidTarget(E.Range) && (x.IsMinion() || x.IsMob()))
                                        .Where(
                                            x =>
                                                x.Health < E.GetDamage(x) &&
                                                GameObjects.EnemyHeroes.Any(a => a.Distance(x.Position) <= 290))
                                        .ToArray();

                                if (minions.Any())
                                {
                                    foreach (var minion in minions.Where(x => x.IsValidTarget(E.Range)).OrderByDescending(x => x.Distance(target)))
                                    {
                                        if (minion != null && minion.IsValidTarget(E.Range))
                                        {
                                            E.CastOnUnit(minion);
                                        }
                                    }
                                }
                                else if (target.IsValidTarget(E.Range))
                                {
                                    E.CastOnUnit(target);
                                }
                            }
                        }
                        else
                        {
                            if (useQ && Q.Ready && target.IsValidTarget(Q.Range))
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.HitChance >= HitChance.VeryHigh)
                                {
                                    Q.Cast(qPred.UnitPosition);
                                }
                            }

                            if (useE && E.Ready && target.IsValidTarget(E.Range))
                            {
                                E.CastOnUnit(target);
                            }

                            if (useQ && Q.Ready && target.IsValidTarget(Q.Range))
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.HitChance >= HitChance.VeryHigh)
                                {
                                    Q.Cast(qPred.UnitPosition);
                                }
                            }

                            if (useW && W.Ready && target.IsValidTarget(W.Range))
                            {
                                W.CastOnUnit(target);
                            }

                            if (useQ && Q.Ready && target.IsValidTarget(Q.Range))
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.HitChance >= HitChance.VeryHigh)
                                {
                                    Q.Cast(qPred.UnitPosition);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.NormalCombo." + ex);
            }
        }

        private static void ShieldCombo(Obj_AI_Hero target, bool useQ, bool useW, bool useE)
        {
            try
            {
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (Game.TickCount - LastCastTime > 500)
                    {
                        if (useQ && Q.Ready &&
                            (FullStack || NoStack || HalfStack &&
                            !W.Ready && Wcd > 1 && !E.Ready && Ecd > 1) &&
                             target.IsValidTarget(Q.Range))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.VeryHigh)
                            {
                                Q.Cast(qPred.UnitPosition);
                            }
                        }

                        if (useW && W.Ready && (!FullStack || HaveShield) &&
                            target.IsValidTarget(W.Range) &&
                            (Ecd >= 2 || target.HasBuff("ryzee")))
                        {
                            W.CastOnUnit(target);
                        }

                        if (useE && E.Ready && (!FullStack || HaveShield) &&
                            target.IsValidTarget(E.Range))
                        {
                            if (NoStack)
                            {
                                E.CastOnUnit(target);
                            }

                            var minions =
                                GameObjects.EnemyMinions.Where(
                                        x => x.IsValidTarget(E.Range) && (x.IsMinion() || x.IsMob()))
                                    .Where(
                                        x =>
                                            x.Health < E.GetDamage(x) &&
                                            GameObjects.EnemyHeroes.Any(a => a.Distance(x.Position) <= 290))
                                    .ToArray();

                            if (minions.Any())
                            {
                                foreach (var minion in minions.Where(x => x.IsValidTarget(E.Range)).OrderByDescending(x => x.Distance(target)))
                                {
                                    if (minion != null && minion.IsValidTarget(E.Range))
                                    {
                                        E.CastOnUnit(minion);
                                    }
                                }
                            }
                            else if (target.IsValidTarget(E.Range))
                            {
                                E.CastOnUnit(target);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.ShieldCombo." + ex);
            }
        }

        private static void BurstCombo(Obj_AI_Hero target, bool useQ, bool useW, bool useE)
        {
            try
            {
                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (Game.TickCount - LastCastTime > 500)
                    {
                        if (useQ && Q.Ready && target.IsValidTarget(Q.Range))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.VeryHigh)
                            {
                                Q.Cast(qPred.UnitPosition);
                            }
                        }

                        if (useE && E.Ready && target.IsValidTarget(E.Range))
                        {
                            E.CastOnUnit(target);
                        }

                        if (useQ && Q.Ready && target.IsValidTarget(Q.Range))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.VeryHigh)
                            {
                                Q.Cast(qPred.UnitPosition);
                            }
                        }

                        if (useW && W.Ready && target.IsValidTarget(W.Range))
                        {
                            W.CastOnUnit(target);
                        }

                        if (useQ && Q.Ready && target.IsValidTarget(Q.Range))
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.VeryHigh)
                            {
                                Q.Cast(qPred.UnitPosition);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.BurstCombo." + ex);
            }
        }

        private static void HarassEvent()
        {
            try
            {
                if (Me.ManaPercent() >= HarassMenu["FlowersRyze.HarassMenu.Mana"].Value)
                {
                    if (HarassMenu["FlowersRyze.HarassMenu.Q"].Enabled && Q.Ready && !FullStack)
                    {
                        var minions =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && (x.IsMinion() || x.IsMob()))
                                .Where(
                                    x =>
                                        x.HasBuff("RyzeE") && x.Health < x.GetRealQDamage() &&
                                        GameObjects.EnemyHeroes.Any(a => a.Distance(x) <= 290))
                                        .ToArray();

                        if (minions.Any())
                        {
                            foreach (var minion in minions.Where(x => x.IsValidTarget(Q.Range)))
                            {
                                if (minion != null && minion.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(minion);

                                    if (qPred.HitChance >= HitChance.Medium)
                                    {
                                        Q.Cast(qPred.UnitPosition);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var target = TargetSelector.GetTarget(Q.Range);

                            if (target != null && target.IsValidTarget(Q.Range))
                            {
                                var qPred = Q.GetPrediction(target);

                                if (qPred.HitChance >= HitChance.High)
                                {
                                    Q.Cast(qPred.UnitPosition);
                                }
                            }
                        }
                    }

                    if (HarassMenu["FlowersRyze.HarassMenu.E"].Enabled && E.Ready && !HalfStack)
                    {
                        var minions =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && (x.IsMinion() || x.IsMob()))
                                .Where(
                                    x =>
                                        x.Health < E.GetDamage(x) &&
                                        GameObjects.EnemyHeroes.Any(a => a.Distance(x.Position) <= 290))
                                        .ToArray();

                        if (minions.Any())
                        {
                            foreach (var minion in minions.Where(x => x.IsValidTarget(E.Range)))
                            {
                                if (minion != null && minion.IsValidTarget(E.Range))
                                {
                                    E.CastOnUnit(minion);
                                }
                            }
                        }
                        else
                        {
                            var target = TargetSelector.GetTarget(E.Range);

                            if (target != null && target.IsValidTarget(E.Range))
                            {
                                E.CastOnUnit(target);
                            }
                        }
                    }

                    if (HarassMenu["FlowersRyze.HarassMenu.W"].Enabled && W.Ready && !HalfStack)
                    {
                        var target = TargetSelector.GetOrderedTargets(W.Range).FirstOrDefault(x => x.IsValidTarget(W.Range) && !x.HasBuff("RyzeE"));

                        if (target != null && target.IsValidTarget(W.Range))
                        {
                            W.CastOnUnit(target);
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
                if (Me.ManaPercent() >= ClearMenu["FlowersRyze.ClearMenu.LaneClearMana"].Value)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        var eMinionsList = minions.Where(x => x.HasBuff("RyzeE")).ToArray();

                        if (eMinionsList.Any())
                        {
                            foreach (
                                var eMinion in
                                eMinionsList.Where(
                                    x =>
                                        GameObjects.EnemyMinions.Count(
                                            a =>
                                                a.IsValidTarget(300, false, false, x.ServerPosition) && a.IsMinion() &&
                                                a.NetworkId != x.NetworkId) >= 2))
                            {
                                if (eMinion != null && eMinion.IsValidTarget(Q.Range))
                                {
                                    if (ClearMenu["FlowersRyze.ClearMenu.LaneClearE"].Enabled && E.Ready && eMinion.IsValidTarget(E.Range))
                                    {
                                        E.CastOnUnit(eMinion);
                                    }

                                    if (ClearMenu["FlowersRyze.ClearMenu.LaneClearQ"].Enabled && Q.Ready)
                                    {
                                        var qPred = Q.GetPrediction(eMinion);

                                        if (qPred.HitChance >= HitChance.Low)
                                        {
                                            Q.Cast(qPred.UnitPosition);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var minion in minions)
                            {
                                if (ClearMenu["FlowersRyze.ClearMenu.LaneClearE"].Enabled && E.Ready)
                                {
                                    foreach (
                                        var eMinion in
                                        minions.Where(
                                                x =>
                                                    GameObjects.EnemyMinions.Count(
                                                        a =>
                                                            a.IsValidTarget(300, false, false, x.ServerPosition) &&
                                                            a.IsMinion() && a.NetworkId != x.NetworkId) >= 2)
                                            .OrderByDescending(
                                                x =>
                                                    GameObjects.EnemyMinions.Count(
                                                        a =>
                                                            a.IsValidTarget(300, false, false, x.ServerPosition) &&
                                                            a.IsMinion() && a.NetworkId != x.NetworkId)))
                                    {
                                        if (eMinion != null && eMinion.IsValidTarget(E.Range))
                                        {
                                            E.CastOnUnit(eMinion);
                                        }
                                    }
                                }

                                if (ClearMenu["FlowersRyze.ClearMenu.LaneClearQ"].Enabled && Q.Ready &&
                                    minion.IsValidTarget(Q.Range) && minion.Health < minion.GetRealQDamage())
                                {
                                    var qPred = Q.GetPrediction(minion);

                                    if (qPred.HitChance >= HitChance.Low)
                                    {
                                        Q.Cast(qPred.UnitPosition);
                                    }
                                }

                                if (ClearMenu["FlowersRyze.ClearMenu.LaneClearW"].Enabled && W.Ready &&
                                    minion.IsValidTarget(W.Range) && minion.Health < W.GetDamage(minion))
                                {
                                    W.CastOnUnit(minion);
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
                if (Me.ManaPercent() >= ClearMenu["FlowersRyze.ClearMenu.JungleClearMana"].Value)
                {
                    var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob()).ToArray();

                    foreach (var mob in mobs.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth))
                    {
                        if (mob != null && mob.IsValidTarget(Q.Range))
                        {
                            if (ClearMenu["FlowersRyze.ClearMenu.JungleClearQ"].Enabled && Q.Ready && mob.IsValidTarget(Q.Range))
                            {
                                Q.Cast(mob.ServerPosition);
                            }

                            if (ClearMenu["FlowersRyze.ClearMenu.JungleClearE"].Enabled && E.Ready && mob.IsValidTarget(E.Range))
                            {
                                E.CastOnUnit(mob);
                            }

                            if (ClearMenu["FlowersRyze.ClearMenu.JungleClearW"].Enabled && W.Ready && mob.IsValidTarget(W.Range))
                            {
                                W.CastOnUnit(mob);
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
                if (Me.ManaPercent() >= LastHitMenu["FlowersRyze.LastHitMenu.LastHitMana"].Value)
                {
                    if (LastHitMenu["FlowersRyze.LastHitMenu.LastHitQ"].Enabled && Q.Ready)
                    {
                        var qMinions =
                            GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && (x.IsMinion() || x.IsMob()))
                                .ToArray();

                        if (qMinions.Any())
                        {
                            foreach (var minion in qMinions.Where(x => x.IsValidTarget(Q.Range) && x.Health < Q.GetDamage(x)))
                            {
                                if (minion.IsValidTarget(Q.Range))
                                {
                                    var qPred = Q.GetPrediction(minion);

                                    if (qPred.HitChance >= HitChance.Low)
                                    {
                                        Q.Cast(qPred.UnitPosition);
                                    }
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

        private static void OnCastSpell(Obj_AI_Base sender, SpellBookCastSpellEventArgs Args)
        {
            try
            {
                if (sender.IsMe)
                {
                    if (Args.Slot == SpellSlot.Q || Args.Slot == SpellSlot.W || Args.Slot == SpellSlot.E)
                    {
                        LastCastTime = Game.TickCount;
                        Me.IssueOrder(OrderType.MoveTo, Me.ServerPosition.Extend(Game.CursorPos, 200));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnCastSpell." + ex);
            }
        }

        private static void OnCreate(GameObject sender)
        {
            try
            {
                if (W.Ready)
                {
                    var Rengar = GameObjects.EnemyHeroes.Find(heros => heros.ChampionName.Equals("Rengar"));
                    var Khazix = GameObjects.EnemyHeroes.Find(heros => heros.ChampionName.Equals("Khazix"));

                    if (MiscMenu["FlowersRyze.MiscMenu.WRengar"].Enabled && Rengar != null)
                    {
                        if (sender.Name == "Rengar_LeapSound.troy" && Rengar.IsValidTarget(W.Range))
                        {
                            W.CastOnUnit(Rengar);
                        }
                    }

                    if (MiscMenu["FlowersRyze.MiscMenu.WKhazix"].Enabled && Khazix != null)
                    {
                        if (sender.Name == "Khazix_Base_E_Tar.troy" && Khazix.IsValidTarget(300f))
                        {
                            W.CastOnUnit(Khazix);
                        }
                    }
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
                if (Me.IsDead || sender == null || !sender.IsEnemy || !MiscMenu["FlowersRyze.MiscMenu.WMelee"].Enabled || !W.Ready)
                {
                    return;
                }

                if (Args.Target != null && Args.Target.IsMe && sender.Type == GameObjectType.obj_AI_Hero &&
                    sender.IsMelee && sender.IsValidTarget(W.Range) && !sender.HaveShiled())
                {
                    W.CastOnUnit(sender);
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
                if (ComboMenu["FlowersRyze.ComboMenu.DisableAttack"].As<MenuList>().Value == 2 ||
                    Orbwalker.Mode != OrbwalkingMode.Combo || Args.Target == null ||
                    Args.Target.Type != GameObjectType.obj_AI_Hero ||
                    Args.Target.Health < Me.GetAutoAttackDamage((Obj_AI_Hero)Args.Target))
                {
                    return;
                }

                switch (ComboMenu["FlowersRyze.ComboMenu.DisableAttack"].As<MenuList>().Value)
                {
                    case 0:
                        if (W.Ready || E.Ready)
                        {
                            Args.Cancel = true;
                        }
                        break;
                    case 1:
                        Args.Cancel = true;
                        break;
                    default:
                        Args.Cancel = false;
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnPreAttack." + ex);
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

                if (DrawMenu["FlowersRyze.DrawMenu.Q"].Enabled && Q.Ready)
                {
                    Render.Circle(Me.Position, Q.Range, 23, Color.FromArgb(251, 0, 133));
                }

                if (DrawMenu["FlowersRyze.DrawMenu.W"].Enabled && W.Ready)
                {
                    Render.Circle(Me.Position, W.Range, 23, Color.FromArgb(86, 0, 255));
                }

                if (DrawMenu["FlowersRyze.DrawMenu.E"].Enabled && E.Ready)
                {
                    Render.Circle(Me.Position, E.Range, 23, Color.FromArgb(0, 136, 255));
                }

                if (DrawMenu["FlowersRyze.DrawMenu.R"].Enabled && R.Ready)
                {
                    Render.Circle(Me.Position, R.Range, 23, Color.FromArgb(0, 255, 161));
                }

                Vector2 MePos = Vector2.Zero;
                Render.WorldToScreen(ObjectManager.GetLocalPlayer().ServerPosition, out MePos);

                if (DrawMenu["FlowersRyze.DrawMenu.Combo"].Enabled)
                {
                    Render.Text(MePos.X - 57, MePos.Y + 88, Color.Orange,
                        "Combo Mode(" + ComboMenu["FlowersRyze.ComboMenu.ModeKey"].As<MenuKeyBind>().Key + "): " +
                        ComboMenu["FlowersRyze.ComboMenu.Mode"].As<MenuList>().SelectedItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyEventManager.OnRender." + ex);
            }
        }
    }
}