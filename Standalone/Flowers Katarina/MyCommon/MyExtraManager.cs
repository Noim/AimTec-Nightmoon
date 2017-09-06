using Aimtec.SDK.Menu;
using Aimtec.SDK.Menu.Components;
using Aimtec.SDK.Orbwalking;
using Aimtec.SDK.TargetSelector;

namespace Flowers_Katarina.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Util.Cache;
    using Flowers_Katarina.MyBase;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Aimtec.SDK;

    #endregion

    internal static class MyExtraManager
    {
        internal static float GetComboDamage(Obj_AI_Hero target)
        {
            if (target == null || target.IsDead || !target.IsValidTarget())
            {
                return 0;
            }

            if (target.IsUnKillable())
            {
                return 0;
            }

            var damage = 0d;

            if (MyLogic.IgniteSlot != SpellSlot.Unknown && MyLogic.Ignite.Ready)
            {
                damage += ObjectManager.GetLocalPlayer().GetIgniteDamage(target);
            }

            if (MyLogic.Q.Ready)
            {
                damage += GetDamage(MyLogic.Q, target);
            }

            if (MyLogic.W.Ready)
            {
                damage += GetDamage(MyLogic.W, target);
            }

            if (MyLogic.E.Ready)
            {
                damage += GetDamage(MyLogic.E, target);
            }

            if (MyLogic.R.Ready)
            {
                damage += GetDamage(MyLogic.R, target);
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Katarina")
            {
                var targetDagger =
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Where(
                            x =>
                                x.UnitSkinName == "testcuberender" && x.Health > 1 && x.IsValid &&
                                x.Distance(target) <= 340).ToArray();

                if (targetDagger.Any())
                {
                    damage += GetKataPassiveDamage(target) * targetDagger.Count();
                }
            }

            if (ObjectManager.GetLocalPlayer().HasBuff("SummonerExhaust"))
            {
                damage = damage * 0.6f;
            }

            if (target.UnitSkinName == "Morderkaiser")
            {
                damage -= target.Mana;
            }

            if (target.HasBuff("GarenW"))
            {
                damage = damage * 0.7f;
            }

            if (target.HasBuff("ferocioushowl"))
            {
                damage = damage * 0.7f;
            }

            if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
            {
                damage -= target.Mana / 2f;
            }

            return (float)damage;
        }

        internal static double GetDamage(this Aimtec.SDK.Spell spell, Obj_AI_Base target)
        {
            if (!spell.Ready)
            {
                return 0;
            }

            double dmg = 0d;

            switch (spell.Slot)
            {
                case SpellSlot.Q:
                    dmg =
                    new double[] { 75, 105, 135, 165, 195 }[ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.Q).Level - 1
                    ] + 0.3 * ObjectManager.GetLocalPlayer().TotalAbilityDamage;
                    break;
                case SpellSlot.W:
                    dmg = 0d;
                    break;
                case SpellSlot.E:
                    dmg =
                        new double[] {30, 45, 60, 75, 90}[ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.E).Level - 1
                        ] +
                        0.25 * ObjectManager.GetLocalPlayer().TotalAbilityDamage +
                        0.65 * ObjectManager.GetLocalPlayer().TotalAttackDamage;
                    break;
                case SpellSlot.R:
                    dmg =
                        new[] {375, 562.5, 750}[ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.R).Level - 1
                        ] +
                        2.85 * ObjectManager.GetLocalPlayer().TotalAbilityDamage +
                        3.30 * ObjectManager.GetLocalPlayer().TotalAttackDamage;
                    break;
            }

            if (dmg > 0)
            {
                return ObjectManager.GetLocalPlayer().CalculateDamage(target, DamageType.Magical, dmg);
            }

            return 0d;
        }


        internal static float GetKataPassiveDamage(Obj_AI_Base target)
        {
            var hant = ObjectManager.GetLocalPlayer().Level < 6
                ? 0
                : (ObjectManager.GetLocalPlayer().Level < 11
                    ? 1
                    : (ObjectManager.GetLocalPlayer().Level < 16 ? 2 : 3));
            var damage = new double[]
                             {75, 78, 83, 88, 95, 103, 112, 122, 133, 145, 159, 173, 189, 206, 224, 243, 264, 245}[
                             ObjectManager.GetLocalPlayer().Level - 1]
                         + ObjectManager.GetLocalPlayer().FlatPhysicalDamageMod
                         + new[] { 0.55, 0.70, 0.85, 1 }[hant] * ObjectManager.GetLocalPlayer().TotalAbilityDamage;

            return (float)ObjectManager.GetLocalPlayer().CalculateDamage(target, DamageType.Magical, damage);
        }

        internal static float ManaPercent(this Obj_AI_Base target)
        {
            if (target.MaxMana > 0)
            {
                return target.Mana / target.MaxMana * 100f;
            }

            return 100;
        }

        internal static float HealthPercent(this Obj_AI_Base target)
        {
            if (target.MaxHealth > 5)
            {
                return target.Health / target.MaxHealth * 100f;
            }

            return 100;
        }

        internal static bool CanMoveMent(this Obj_AI_Base target)
        {
            return !(target.MoveSpeed < 50) && !target.HasBuffOfType(BuffType.Stun) &&
                   !target.HasBuffOfType(BuffType.Fear) && !target.HasBuffOfType(BuffType.Snare) &&
                   !target.HasBuffOfType(BuffType.Knockup) && !target.HasBuff("recall") &&
                   !target.HasBuffOfType(BuffType.Knockback)
                   && !target.HasBuffOfType(BuffType.Charm) && !target.HasBuffOfType(BuffType.Taunt) &&
                   !target.HasBuffOfType(BuffType.Suppression) &&
                   !target.HasBuff("zhonyasringshield") && !target.HasBuff("bardrstasis");
        }

        internal static bool IsUnKillable(this Obj_AI_Base target)
        {
            if (target == null || target.IsDead || target.Health <= 0)
            {
                return true;
            }

            if (target.HasBuff("KindredRNoDeathBuff"))
            {
                return true;
            }

            if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.ClockTime > 0.3 &&
                target.Health <= target.MaxHealth * 0.10f)
            {
                return true;
            }

            if (target.HasBuff("JudicatorIntervention"))
            {
                return true;
            }

            if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.ClockTime > 0.3 &&
                target.Health <= target.MaxHealth * 0.10f)
            {
                return true;
            }

            if (target.HasBuff("VladimirSanguinePool"))
            {
                return true;
            }

            if (target.HasBuff("ShroudofDarkness"))
            {
                return true;
            }

            if (target.HasBuff("SivirShield"))
            {
                return true;
            }

            if (target.HasBuff("itemmagekillerveil"))
            {
                return true;
            }

            return target.HasBuff("FioraW");
        }

        public static double GetIgniteDamage(this Obj_AI_Hero source, Obj_AI_Hero target)
        {
            return 50 + 20 * source.Level - target.HPRegenRate / 5 * 3;
        }

        internal static double GetRealDamage(this Aimtec.SDK.Spell spell, Obj_AI_Base target, bool havetoler = false, float tolerDMG = 0)
        {
            if (target != null && !target.IsDead && target.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.ClockTime > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.ClockTime > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0d;

                damage += spell.Ready
                    ? ObjectManager.GetLocalPlayer().GetSpellDamage(target, spell.Slot)
                    : 0d + (havetoler ? tolerDMG : 0) - target.HPRegenRate;

                if (target.UnitSkinName == "Morderkaiser")
                {
                    damage -= target.Mana;
                }

                if (ObjectManager.GetLocalPlayer().HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0d;
        }

        internal static double GetRealDamage(this Obj_AI_Base target, double DMG)
        {
            if (target != null && !target.IsDead && target.Buffs.Any(a => a.Name.ToLower().Contains("kalistaexpungemarker")))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.ClockTime > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.ClockTime > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0d;

                damage += DMG - target.HPRegenRate;

                if (target.UnitSkinName == "Morderkaiser")
                {
                    damage -= target.Mana;
                }

                if (ObjectManager.GetLocalPlayer().HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0d;
        }

        internal static void QEWLogic(Obj_AI_Hero target, bool useQ, bool useW, bool useE)
        {
            if (target == null || !target.IsValidTarget())
            {
                target = TargetSelector.GetTarget(MyLogic.E.Range + 300f);
            }

            if (target == null || !target.IsValidTarget() || !target.IsValidTarget())
            {
                return;
            }

            if (useQ && MyLogic.Q.Ready)
            {
                if (target.IsValidTarget(MyLogic.Q.Range))
                {
                    if (!(MyLogic.W.Ready && MyLogic.E.Ready) && target.DistanceToPlayer() <= 300 ||
                        !target.IsFacingUnit(ObjectManager.GetLocalPlayer()) && target.DistanceToPlayer() > 300)
                    {
                        MyLogic.Q.CastOnUnit(target);
                    }
                }
                else if (target.IsValidTarget(MyLogic.E.Range) &&
                         (MyLogic.Orbwalker.Mode == OrbwalkingMode.Combo &&
                          MyLogic.ComboMenu["FlowersKatarina.ComboMenu.QOnMinion"].Enabled ||
                          MyLogic.Orbwalker.Mode == OrbwalkingMode.Mixed &&
                          MyLogic.HarassMenu["FlowersKatarina.HarassMenu.QOnMinion"].Enabled))
                {
                    var extraDagger = target.ServerPosition.Extend(ObjectManager.GetLocalPlayer().ServerPosition, 350f);
                    var min = ObjectManager.Get<Obj_AI_Base>().Aggregate((x, y) => x.Distance(extraDagger) < y.Distance(extraDagger) ? x : y);

                    if (min.Distance(extraDagger) < 130)
                    {
                        MyLogic.Q.CastOnUnit(min);
                    }
                }
            }

            if (useE && MyLogic.E.Ready && target.IsValidTarget(MyLogic.E.Range + 300f) && !MyLogic.Q.Ready)
            {
                var ePos = GetEPosition(target);

                if (ePos != Vector3.Zero && ePos.DistanceToPlayer() <= MyLogic.E.Range && CanCastE(ePos, target))
                {
                    if (MyLogic.MiscMenu["FlowersKatarina.MiscMenu.EHumanizer"].As<MenuSliderBool>().Enabled)
                    {
                        MyDelayAction.Queue(
                            MyLogic.MiscMenu["FlowersKatarina.MiscMenu.EHumanizer"].As<MenuSliderBool>().Value,
                            () => MyLogic.E.Cast(ePos));
                    }
                    else
                    {
                        MyLogic.E.Cast(ePos);
                    }
                }
            }

            if (useW && MyLogic.W.Ready && target.IsValidTarget(MyLogic.W.Range))
            {
                MyLogic.W.Cast();
            }
        }

        internal static void EQWLogic(Obj_AI_Hero target, bool useQ, bool useW, bool useE)
        {
            if (target == null || !target.IsValidTarget())
            {
                target = TargetSelector.GetTarget(MyLogic.E.Range + 300f);
            }

            if (target == null || !target.IsValidTarget() || !target.IsValidTarget())
            {
                return;
            }

            if (useE && MyLogic.E.Ready && target.IsValidTarget(MyLogic.E.Range + 300f))
            {
                var ePos = GetEPosition(target);

                if (ePos != Vector3.Zero && ePos.DistanceToPlayer() <= MyLogic.E.Range && CanCastE(ePos, target))
                {
                    if (MyLogic.MiscMenu["FlowersKatarina.MiscMenu.EHumanizer"].As<MenuSliderBool>().Enabled)
                    {
                        MyDelayAction.Queue(
                            MyLogic.MiscMenu["FlowersKatarina.MiscMenu.EHumanizer"].As<MenuSliderBool>().Value,
                            () => MyLogic.E.Cast(ePos));
                    }
                    else
                    {
                        MyLogic.E.Cast(ePos);
                    }
                }
            }

            if (useQ && MyLogic.Q.Ready && target.IsValidTarget(MyLogic.Q.Range) && !MyLogic.E.Ready)
            {
                if (target.IsValidTarget(MyLogic.Q.Range))
                {
                    if (!(MyLogic.W.Ready && MyLogic.E.Ready) && target.DistanceToPlayer() <= 300 ||
                        !target.IsFacingUnit(ObjectManager.GetLocalPlayer()) && target.DistanceToPlayer() > 300)
                    {
                        MyLogic.Q.CastOnUnit(target);
                    }
                }
                else if (target.IsValidTarget(MyLogic.E.Range) &&
                         (MyLogic.Orbwalker.Mode == OrbwalkingMode.Combo &&
                          MyLogic.ComboMenu["FlowersKatarina.ComboMenu.QOnMinion"].Enabled ||
                          MyLogic.Orbwalker.Mode == OrbwalkingMode.Mixed &&
                          MyLogic.HarassMenu["FlowersKatarina.HarassMenu.QOnMinion"].Enabled))
                {
                    var extraDagger = target.ServerPosition.Extend(ObjectManager.GetLocalPlayer().ServerPosition, 350f);
                    var min = ObjectManager.Get<Obj_AI_Base>().Aggregate((x, y) => x.Distance(extraDagger) < y.Distance(extraDagger) ? x : y);

                    if (min.Distance(extraDagger) < 130)
                    {
                        MyLogic.Q.CastOnUnit(min);
                    }
                }
            }

            if (useW && MyLogic.W.Ready && target.IsValidTarget(MyLogic.W.Range))
            {
                MyLogic.W.Cast();
            }
        }

        internal static Vector3 GetEPosition(Obj_AI_Hero target)
        {
            var pos = Vector3.Zero;

            if (!target.IsValidTarget(MyLogic.E.Range + MyLogic.PassiveRange))
            {
                pos = Vector3.Zero;
            }
            else
            {
                if (MyLogic.Daggers.Any(
                    x =>
                        GameObjects.EnemyHeroes.Any(a => a.Distance(x.pos) <= MyLogic.PassiveRange) &&
                        x.pos.DistanceToPlayer() <= MyLogic.E.Range))
                {
                    foreach (
                        var obj in
                        MyLogic.Daggers.Where(x => x.pos.Distance(target.ServerPosition) <= MyLogic.PassiveRange)
                            .OrderByDescending(x => x.pos.Distance(target.ServerPosition)))
                    {
                        if (obj.obj != null && obj.obj.IsValid && obj.pos.DistanceToPlayer() <= MyLogic.E.Range)
                        {
                            pos = obj.pos;
                        }
                    }
                }
                else if (
                 MyLogic.Daggers.Any(
                        x =>
                            GameObjects.EnemyHeroes.Any(a => a.Distance(x.pos) <= MyLogic.E.Range) &&
                            x.pos.DistanceToPlayer() <= MyLogic.E.Range))
                {
                    foreach (
                        var obj in
                      MyLogic.Daggers.Where(x => x.pos.Distance(target.ServerPosition) <= MyLogic.E.Range)
                            .OrderBy(x => x.pos.Distance(target.ServerPosition)))
                    {
                        if (obj.obj != null && obj.obj.IsValid && obj.pos.DistanceToPlayer() <= MyLogic.E.Range)
                        {
                            pos = obj.pos;
                        }
                    }
                }
                else if (target.DistanceToPlayer() <= MyLogic.E.Range - 130)
                {
                    pos = ObjectManager.GetLocalPlayer().ServerPosition.Extend(target.ServerPosition, target.DistanceToPlayer() + 130);
                }
                else if (target.IsValidTarget(MyLogic.E.Range))
                {
                    pos = target.ServerPosition;
                }
                else
                {
                    pos = Vector3.Zero;
                }
            }

            return pos;
        }

        internal static bool CanCastE(Vector3 pos, Obj_AI_Hero target)
        {
            if (pos == Vector3.Zero || target == null || target.IsDead)
            {
                return false;
            }

            if (MyLogic.MiscMenu["FlowersKatarina.MiscMenu.ETurret"].As<MenuList>().Value == 0 && pos.PointUnderEnemyTurret())
            {
                return false;
            }

            if (MyLogic.MiscMenu["FlowersKatarina.MiscMenu.ETurret"].As<MenuList>().Value == 1 && pos.PointUnderEnemyTurret())
            {
                if (ObjectManager.GetLocalPlayer().HealthPercent() <=
                    MyLogic.MiscMenu["FlowersKatarina.MiscMenu.ETurretHP"].Value &&
                    target.Health > GetComboDamage(target) * 0.85)
                {
                    return false;
                }
            }

            if (MyLogic.ComboMenu["FlowersKatarina.ComboMenu.EKillAble"].As<MenuKeyBind>().Enabled)
            {
                if (GameObjects.EnemyHeroes.Count(x => x.Distance(pos) <= MyLogic.R.Range) >= 3)
                {
                    if (GameObjects.EnemyHeroes.Count(x => x.Distance(pos) <= MyLogic.R.Range) == 3)
                    {
                        if (GameObjects.EnemyHeroes.Count(x => x.Health < GetComboDamage(target) * 1.45) <= 2)
                        {
                            return false;
                        }
                    }
                    else if (GameObjects.EnemyHeroes.Count(x => x.Distance(pos) <= MyLogic.R.Range) == 4)
                    {
                        if (GameObjects.EnemyHeroes.Count(x => x.Health < GetComboDamage(target) * 1.45) < 2)
                        {
                            return false;
                        }
                    }
                    else if (GameObjects.EnemyHeroes.Count(x => x.Distance(pos) <= MyLogic.R.Range) == 5)
                    {
                        if (GameObjects.EnemyHeroes.Count(x => x.Health < GetComboDamage(target) * 1.45) < 3)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (MyLogic.Orbwalker.Mode == OrbwalkingMode.Combo)
                    {
                        if (target.Health >
                            (GameObjects.AllyHeroes.Any(x => x.DistanceToPlayer() <= MyLogic.E.Range)
                                ? GetComboDamage(target) + ObjectManager.GetLocalPlayer().Level * 45
                                : GetComboDamage(target)))
                        {
                            return false;
                        }
                    }
                    else if (MyLogic.Orbwalker.Mode == OrbwalkingMode.Mixed)
                    {
                        if (pos.PointUnderEnemyTurret())
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        internal static void CancelUlt(bool ignoreCheck = false)
        {
            if (!ignoreCheck && GameObjects.Heroes.Any(x => !x.IsDead && x.DistanceToPlayer() <= MyLogic.R.Range))
            {
                return;
            }

            if (Game.TickCount - MyLogic.lastCancelTime > 5000)
            {
                ObjectManager.GetLocalPlayer()
                    .IssueOrder(OrderType.MoveTo,
                        ObjectManager.GetLocalPlayer().ServerPosition.Extend(Game.CursorPos, 100));
                MyLogic.lastCancelTime = Game.TickCount;
            }
        }


        internal static float GetDariusPassiveCount(Obj_AI_Base target)
        {
            return target.GetBuffCount("DariusHemo") > 0 ? target.GetBuffCount("DariusHemo") : 0;
        }

        internal static SpellSlot GetItemSlot(this Obj_AI_Hero source, string itemName)
        {
            if (source == null || string.IsNullOrEmpty(itemName))
            {
                return SpellSlot.Unknown;
            }

            var slot =
                source.Inventory.Slots.FirstOrDefault(
                    x => string.Equals(itemName, x.SpellName, StringComparison.CurrentCultureIgnoreCase));
            if (slot != null && slot.SpellSlot != SpellSlot.Unknown)
            {
                return slot.SpellSlot;
            }

            return SpellSlot.Unknown;
        }

        internal static bool CanUseItem(this Obj_AI_Hero source, string itemName)
        {
            if (source == null || string.IsNullOrEmpty(itemName))
            {
                return false;
            }

            var slot = source.GetItemSlot(itemName);
            if (slot != SpellSlot.Unknown)
            {
                return source.SpellBook.GetSpellState(slot) == SpellState.Ready;
            }

            return false;
        }

        internal static void UseItem(this Obj_AI_Hero source, Obj_AI_Hero target, string itemName)
        {
            if (source == null || target == null || !target.IsValidTarget() || string.IsNullOrEmpty(itemName))
            {
                return;
            }

            var slot = source.GetItemSlot(itemName);
            if (slot != SpellSlot.Unknown && source.CanUseItem(itemName))
            {
                source.SpellBook.CastSpell(slot, target);
            }
        }

        internal static void UseItem(this Obj_AI_Hero source, Vector3 position, string itemName)
        {
            if (source == null || position == Vector3.Zero || string.IsNullOrEmpty(itemName))
            {
                return;
            }

            var slot = source.GetItemSlot(itemName);
            if (slot != SpellSlot.Unknown && source.CanUseItem(itemName))
            {
                source.SpellBook.CastSpell(slot, position);
            }
        }

        internal static void UseItem(this Obj_AI_Hero source, string itemName)
        {
            if (source == null || string.IsNullOrEmpty(itemName))
            {
                return;
            }

            var slot = source.GetItemSlot(itemName);
            if (slot != SpellSlot.Unknown && source.CanUseItem(itemName))
            {
                source.SpellBook.CastSpell(slot);
            }
        }

        internal static void GetNames(this Obj_AI_Hero source)
        {
            foreach (var slot in source.Inventory.Slots)
            {
                if (!slot.SpellName.ToLower().Contains("no script"))
                {
                    Console.WriteLine(slot.SpellName + " - " + slot.ItemId);
                }
            }
        }

        internal static IEnumerable<GameObject> badaoFleeLogic
        {
            get
            {
                var Vinasun = new List<GameObject>();
                Vinasun.AddRange(GameObjects.Minions.Where(x => x.IsValidTarget(MyLogic.E.Range) && (x.IsMinion() || x.IsMob())).ToArray());
                Vinasun.AddRange(GameObjects.Heroes.Where(x => x != null && x.IsTargetable && x.IsValidTarget(MyLogic.E.Range)).ToArray());
                Vinasun.AddRange(MyLogic.Daggers.Where(x => Game.TickCount - x.time < 3850).Select(x => x.obj).ToArray());
                return Vinasun;
            }
        }

        internal static float DistanceToPlayer(this Obj_AI_Base source)
        {
            return ObjectManager.GetLocalPlayer().Distance(source);
        }

        internal static float DistanceToPlayer(this Vector3 position)
        {
            return position.To2D().DistanceToPlayer();
        }

        internal static float DistanceToPlayer(this Vector2 position)
        {
            return ObjectManager.GetLocalPlayer().Distance(position);
        }

        internal static float DistanceToMouse(this Obj_AI_Base source)
        {
            return Game.CursorPos.Distance(source.Position);
        }

        internal static float DistanceToMouse(this Vector3 position)
        {
            return position.To2D().DistanceToMouse();
        }

        internal static float DistanceToMouse(this Vector2 position)
        {
            return Game.CursorPos.Distance(position.To3D());
        }

        public static TSource Find<TSource>(this IEnumerable<TSource> source, Predicate<TSource> match)
        {
            return (source as List<TSource> ?? source.ToList()).Find(match);
        }

        internal static bool HaveShiled(this Obj_AI_Base target)
        {
            if (target == null || target.IsDead || target.Health <= 0)
            {
                return false;
            }

            if (target.HasBuff("BlackShield"))
            {
                return true;
            }

            if (target.HasBuff("bansheesveil"))
            {
                return true;
            }

            if (target.HasBuff("SivirE"))
            {
                return true;
            }

            if (target.HasBuff("NocturneShroudofDarkness"))
            {
                return true;
            }

            if (target.HasBuff("itemmagekillerveil"))
            {
                return true;
            }

            if (target.HasBuffOfType(BuffType.SpellShield))
            {
                return true;
            }

            return false;
        }

        public static T MinOrDefault<T, TR>(this IEnumerable<T> container, Func<T, TR> valuingFoo)
            where TR : IComparable
        {
            var enumerator = container.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return default(T);
            }

            var minElem = enumerator.Current;
            var minVal = valuingFoo(minElem);

            while (enumerator.MoveNext())
            {
                var currVal = valuingFoo(enumerator.Current);

                if (currVal.CompareTo(minVal) < 0)
                {
                    minVal = currVal;
                    minElem = enumerator.Current;
                }
            }

            return minElem;
        }

        internal static bool IsMob(this AttackableUnit target)
        {
            return target != null && target.IsValidTarget() && target.Type == GameObjectType.obj_AI_Minion &&
                !target.Name.ToLower().Contains("plant") && target.Team == GameObjectTeam.Neutral;
        }

        internal static bool IsMinion(this AttackableUnit target)
        {
            return target != null && target.IsValidTarget() && target.Type == GameObjectType.obj_AI_Minion &&
                !target.Name.ToLower().Contains("plant") && target.Team != GameObjectTeam.Neutral;
        }
    }
}
