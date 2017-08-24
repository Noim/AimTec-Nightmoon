namespace Flowers_Kalista.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    internal static class MyExtraManager
    {
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
            if (target.MaxHealth > 0)
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
                    ? ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.E) +
                      ObjectManager.GetLocalPlayer()
                          .GetSpellDamage(target, SpellSlot.E, Aimtec.SDK.Damage.JSON.DamageStage.Buff) //Kalista E
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

        internal static bool isBigMob(this Obj_AI_Base mob)
        {
            switch (mob.UnitSkinName)
            {
                case "SRU_Baron":
                case "SRU_Blue":
                case "SRU_Dragon_Elder":
                case "SRU_Dragon_Fire":
                case "SRU_Dragon_Air":
                case "SRU_Dragon_Earth":
                case "SRU_Dragon_Water":
                case "SRU_Red":
                case "SRU_RiftHerald":
                /*case "SRU_Murkwolf":
                case "SRU_Gromp":
                case "Sru_Crab":
                case "SRU_Razorbeak":
                case "SRU_Krug":*/
                    return true;
                default:
                    return false;
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
    }
}
