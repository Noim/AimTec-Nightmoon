namespace Flowers_Ryze.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;

    using Flowers_Ryze.MyBase;

    using System;
    using System.Collections.Generic;
    using System.Linq;

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
                damage += GetRealQDamage(target);
            }

            if (MyLogic.W.Ready)
            {
                damage += MyLogic.W.GetDamage(target);
            }

            if (MyLogic.E.Ready)
            {
                damage += MyLogic.E.GetDamage(target);
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
                    dmg = GetRealQDamage(target);
                    break;
                case SpellSlot.W:
                    dmg =
                        new double[] {0, 80, 100, 120, 140, 160}[ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.W).Level] + 
                        0.2 * ObjectManager.GetLocalPlayer().TotalAbilityDamage +
                        0.01 *
                        (ObjectManager.GetLocalPlayer().MaxMana - 400 - 50 * ObjectManager.GetLocalPlayer().Level);
                    break;
                case SpellSlot.E:
                    dmg =
                        new double[] {0, 50, 75, 100, 125, 150}[ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.E).Level] +
                        0.3 * ObjectManager.GetLocalPlayer().TotalAbilityDamage +
                        0.02 *
                        (ObjectManager.GetLocalPlayer().MaxMana - 400 - 50 * ObjectManager.GetLocalPlayer().Level);
                    break;
                case SpellSlot.R:
                    dmg = 0d;
                    break;
            }

            if (dmg > 0)
            {
                return ObjectManager.GetLocalPlayer().CalculateDamage(target, DamageType.Physical, dmg);
            }

            return 0d;
        }

        internal static double GetRealQDamage(this Obj_AI_Base target, bool calculateOthers = false)
        {
            var basicDMG =
                new double[] {0, 60, 85, 110, 135, 160, 185}[ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.Q).Level] +
                0.45 * ObjectManager.GetLocalPlayer().TotalAbilityDamage +
                0.3 * (ObjectManager.GetLocalPlayer().MaxMana - 400 - 50 * ObjectManager.GetLocalPlayer().Level);

            if (target.HasBuff("RyzeE"))
            {
                basicDMG += new double[] {0, 40, 55, 70, 85, 100}[
                    ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.E)
                        .Level] / 100;
            }

            var realDMG = ObjectManager.GetLocalPlayer().CalculateDamage(target, DamageType.Magical, basicDMG);

            if (calculateOthers)
            {
                realDMG = GetRealDamage(target, realDMG);
            }

            return realDMG;
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
