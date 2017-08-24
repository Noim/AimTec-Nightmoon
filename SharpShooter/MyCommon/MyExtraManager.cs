namespace SharpShooter.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Damage;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    internal static class MyExtraManager
    {
        public static double GetComboDamage(this Obj_AI_Base target, bool q, bool w, bool e, bool r, bool attack)
        {
            if (target == null || target.IsDead || !target.IsValidTarget())
            {
                return 0;
            }

            if (!q && !w && !e && !r && !attack)
            {
                return 0;
            }

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

            if (q && ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.Q).State == SpellState.Ready)
            {
                if (ObjectManager.GetLocalPlayer().ChampionName == "Jayce")
                {
                    damage += MyPlugin.Jayce.GetQDamage(target);
                }
                else
                {
                    damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.Q);
                }
            }

            if (w && ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.W).State == SpellState.Ready)
            {
                if (ObjectManager.GetLocalPlayer().ChampionName == "Jayce")
                {
                    damage += MyPlugin.Jayce.GetWDamage(target);
                }
                else if (ObjectManager.GetLocalPlayer().ChampionName == "Vayne")
                {
                    damage += MyPlugin.Vayne.GetWDamage(target);
                }
                else
                {
                    damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.W);
                }
            }

            if (e && ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.E).State == SpellState.Ready)
            {
                damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.E);

                if (ObjectManager.GetLocalPlayer().ChampionName == "Jayce")
                {
                    damage += MyPlugin.Jayce.GetEDamage(target);
                }
                else if (ObjectManager.GetLocalPlayer().ChampionName == "Kalista")
                {
                    damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.E) +
                              ObjectManager.GetLocalPlayer()
                                  .GetSpellDamage(target, SpellSlot.E, Aimtec.SDK.Damage.JSON.DamageStage.Buff);
                }
                else if (ObjectManager.GetLocalPlayer().ChampionName == "Twitch")
                {
                    damage += MyPlugin.Twitch.GetEDMGTwitch(target);
                }
                else if (ObjectManager.GetLocalPlayer().ChampionName == "Xayah")
                {
                    if (target.Type == GameObjectType.obj_AI_Minion)
                    {
                        damage += MyPlugin.Xayah.GetEDamageForMinion(target);
                    }
                    else
                    {
                        if (MyPlugin.Xayah.HitECount(target) > 0)
                        {
                            damage += MyPlugin.Xayah.GetEDMG(target, MyPlugin.Xayah.HitECount(target));
                        }
                    }
                }
                else
                {
                    damage += ObjectManager.GetLocalPlayer()
                        .GetSpellDamage(target, SpellSlot.E, Aimtec.SDK.Damage.JSON.DamageStage.Buff);
                }
            }

            if (r && ObjectManager.GetLocalPlayer().GetSpell(SpellSlot.R).State == SpellState.Ready)
            {
                damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.R);
            }

            if (attack)
            {
                damage += ObjectManager.GetLocalPlayer().GetAutoAttackDamage(target);
            }

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

        public static bool isBigMob(this Obj_AI_Base mob)
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

        public static SpellSlot GetSpellSlotFromName(this Obj_AI_Hero source, string name)
        {
            foreach (var spell in source.SpellBook.Spells.Where(spell => string.Equals(spell.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            {
                return spell.Slot;
            }

            return SpellSlot.Unknown;
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

        internal static bool IsWall(this Vector3 Position)
        {
            var CF = NavMesh.WorldToCell(Position).Flags;

            return CF.HasFlag(NavCellFlags.Wall) || CF.HasFlag(NavCellFlags.Building);
        }

        internal static bool IsWall(this Vector2 Position)
        {
            var CF = NavMesh.WorldToCell(Position.To3D()).Flags;

            return CF.HasFlag(NavCellFlags.Wall) || CF.HasFlag(NavCellFlags.Building);
        }

        internal static bool IsGrass(this Vector3 Position)
        {
            var CF = NavMesh.WorldToCell(Position).Flags;

            return CF.HasFlag(NavCellFlags.Grass);
        }

        internal static bool IsGrass(this Vector2 Position)
        {
            var CF = NavMesh.WorldToCell(Position.To3D()).Flags;

            return CF.HasFlag(NavCellFlags.Grass);
        }

        internal static bool HaveShiledBuff(this Obj_AI_Base target)
        {
            if (target == null || target.IsDead || target.Health <= 0 || !target.IsValidTarget())
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

        internal static Spell GetBasicSpell(this Aimtec.SDK.Spell spell)
        {
            return ObjectManager.GetLocalPlayer().SpellBook.GetSpell(spell.Slot);
        }

        internal static SpellData GetSpellData(this Aimtec.SDK.Spell spell)
        {
            return ObjectManager.GetLocalPlayer().SpellBook.GetSpell(spell.Slot).SpellData;
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

        internal static double GetRealDamage(double Damage, Obj_AI_Base target, bool havetoler = false, float tolerDMG = 0)
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

                damage += Damage + (havetoler ? tolerDMG : 0) - target.HPRegenRate;

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

        internal static double GetKalistaRealDamage(this Aimtec.SDK.Spell spell, Obj_AI_Base target, bool havetoler = false, float tolerDMG = 0, bool getrealDMG = false)
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
                    ? ObjectManager.GetLocalPlayer().GetSpellDamage(target, spell.Slot) +
                      ObjectManager.GetLocalPlayer()
                          .GetSpellDamage(target, spell.Slot, Aimtec.SDK.Damage.JSON.DamageStage.Buff) //Kalista E
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

        internal static float DistanceToPlayer(this Obj_AI_Base source)
        {
            return ObjectManager.GetLocalPlayer().ServerPosition.Distance(source);
        }

        internal static float DistanceToPlayer(this Vector3 position)
        {
            return position.To2D().DistanceToPlayer();
        }

        internal static float DistanceToPlayer(this Vector2 position)
        {
            return ObjectManager.GetLocalPlayer().ServerPosition.Distance(position);
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

        internal static TSource Find<TSource>(this IEnumerable<TSource> source, Predicate<TSource> match)
        {
            return (source as List<TSource> ?? source.ToList()).Find(match);
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

        internal static bool IsInFountainRange(this Obj_AI_Base hero, bool enemyFountain = false)
        {
            return hero.IsValid &&
                   ObjectManager.Get<GameObject>()
                       .Where(x => x.Type == GameObjectType.obj_SpawnPoint)
                       .Any(
                           x =>
                               (enemyFountain ? x.Team != hero.Team : x.Team == hero.Team) && x.Team != hero.Team &&
                               hero.ServerPosition.DistanceSqr(x.Position) <= 1200 * 1200);
        }

        internal static IEnumerable<Vector3> GetCirclePoints(float range)
        {
            var points = new List<Vector3>();

            for (var i = 1; i <= 360; i++)
            {
                var angle = i * 2 * Math.PI / 360;
                var point =
                    new Vector3(ObjectManager.GetLocalPlayer().ServerPosition.X + range * (float) Math.Cos(angle),
                        ObjectManager.GetLocalPlayer().ServerPosition.Y + range * (float) Math.Sin(angle),
                        ObjectManager.GetLocalPlayer().ServerPosition.Z);

                points.Add(point);
            }

            return points;
        }

        internal static IEnumerable<Vector3> GetCirclePoints(Vector3 position, float range)
        {
            var points = new List<Vector3>();

            for (var i = 1; i <= 360; i++)
            {
                var angle = i * 2 * Math.PI / 360;
                var point =
                    new Vector3(position.X + range * (float)Math.Cos(angle),
                        position.Y + range * (float)Math.Sin(angle),
                        position.Z);

                points.Add(point);
            }

            return points;
        }

        internal static IEnumerable<Vector3> GetCirclePoints(Vector2 position, float range)
        {
            var points = new List<Vector3>();

            for (var i = 1; i <= 360; i++)
            {
                var angle = i * 2 * Math.PI / 360;
                var point = new Vector2(position.X + range * (float)Math.Cos(angle), position.Y + range * (float)Math.Sin(angle));

                points.Add(point.To3D());
            }

            return points;
        }
    }
}
