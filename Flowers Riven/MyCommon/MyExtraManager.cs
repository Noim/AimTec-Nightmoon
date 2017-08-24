namespace Flowers_Riven.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;

    using Flowers_Riven.MyBase;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    internal static class MyExtraManager
    {
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

        internal static int GameTimeTickCount
        {
            get { return (int) (Game.ClockTime * 1000); }
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

        public static double GetIgniteDamage(this Obj_AI_Hero source, Obj_AI_Hero target)
        {
            return 50 + 20 * source.Level - target.HPRegenRate / 5 * 3;
        }

        public static SpellSlot GetSpellSlotFromName(this Obj_AI_Hero source, string name)
        {
            foreach (var spell in source.SpellBook.Spells.Where(spell => string.Equals(spell.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            {
                return spell.Slot;
            }

            return SpellSlot.Unknown;
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

        internal static bool HaveShiled(this Obj_AI_Hero target)
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

        internal static bool CastQ(Obj_AI_Base target)
        {
            if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.Q).Level == 0 || !MyLogic.Q.Ready)
            {
                return false;
            }

            switch (MyLogic.MiscMenu["FlowersRiven.MiscMenu.QMode"].As<MenuList>().Value)
            {
                case 0:
                    if (target == null || target.IsDead || !target.IsValidTarget())
                    {
                        return false;
                    }

                    return MyLogic.Q.Cast(target);//target.ServerPosition
                case 1:
                    return MyLogic.Q.Cast(Game.CursorPos);
                default:
                    return false;
            }
        }

        internal static bool R1Logic(Obj_AI_Hero target)
        {
            if (target == null || !target.IsValidTarget(500) || MyLogic.isRActive ||
                !MyLogic.ComboMenu["FlowersRiven.ComboMenu.R"].As<MenuKeyBind>().Enabled ||
                !MyLogic.ComboMenu["FlowersRiven.ComboMenu.RTargetFor" + target.ChampionName].Enabled)
            {
                return false;
            }

            return MyLogic.R.Cast();
        }

        internal static bool R2Logic(Obj_AI_Hero target)
        {
            if (target == null || !target.IsValidTarget() || !MyLogic.isRActive ||
                MyLogic.ComboMenu["FlowersRiven.ComboMenu.RMode"].As<MenuList>().Value == 3 ||
                !MyLogic.ComboMenu["FlowersRiven.ComboMenu.RTargetFor" + target.ChampionName].Enabled)
            {
                return false;
            }

            switch (MyLogic.ComboMenu["FlowersRiven.ComboMenu.RMode"].As<MenuList>().Value)
            {
                case 0:
                    if (target.HealthPercent() < 20 ||
                        target.Health >
                        ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.R) +
                        ObjectManager.GetLocalPlayer().GetAutoAttackDamage(target) * 2 && target.HealthPercent() < 40 ||
                        target.Health <= ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.R) ||
                        target.Health <= GetComboDamage(target))
                    {
                        return MyLogic.R.Cast(target.ServerPosition);
                    }
                    break;
                case 1:
                    if (ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.R) > target.Health && target.DistanceToPlayer() < 600)
                    {
                        return MyLogic.R.Cast(target.ServerPosition);
                    }
                    break;
                case 2:
                    if (target.DistanceToPlayer() < 600)
                    {
                        return MyLogic.R.Cast(target.ServerPosition);
                    }
                    break;
            }

            return false;
        }

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
                damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.Q);
            }

            if (MyLogic.W.Ready)
            {
                damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.W);
            }

            if (MyLogic.E.Ready)
            {
                damage += ObjectManager.GetLocalPlayer().CanMoveMent() ? ObjectManager.GetLocalPlayer().GetAutoAttackDamage(target) : 0;
            }

            if (MyLogic.R.Ready)
            {
                damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.R);
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

        internal static double GetRivenPassive
        {
            get
            {
                if (ObjectManager.GetLocalPlayer().Level == 18)
                {
                    return 0.5;
                }

                if (ObjectManager.GetLocalPlayer().Level >= 15)
                {
                    return 0.45;
                }

                if (ObjectManager.GetLocalPlayer().Level >= 12)
                {
                    return 0.4;
                }

                if (ObjectManager.GetLocalPlayer().Level >= 9)
                {
                    return 0.35;
                }

                if (ObjectManager.GetLocalPlayer().Level >= 6)
                {
                    return 0.3;
                }

                if (ObjectManager.GetLocalPlayer().Level >= 3)
                {
                    return 0.25;
                }

                return 0.2;
            }
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
                if (!slot.SpellName.ToLower().Contains("noscript"))
                {
                    Console.WriteLine(slot.SpellName);
                }
            }
        }

        internal static bool IsBuilding(this AttackableUnit target)
        {
            return target != null &&
                   (target.Type == GameObjectType.obj_AI_Turret || target.Type == GameObjectType.obj_Building ||
                    target.Type == GameObjectType.obj_Barracks ||
                    target.Type == GameObjectType.obj_BarracksDampener ||
                    target.Type == GameObjectType.obj_HQ);
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

        internal static bool IsWall(this Vector3 position)
        {
            if (position == Vector3.Zero)
            {
                return false;
            }

            var flag = NavMesh.WorldToCell(position).Flags;
            return flag == NavCellFlags.Wall || flag == NavCellFlags.Building;
        }
    }
}