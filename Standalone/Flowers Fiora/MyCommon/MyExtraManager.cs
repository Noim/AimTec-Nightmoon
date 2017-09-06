namespace Flowers_Fiora.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Fiora.MyBase;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    internal static class MyExtraManager
    {
        private static readonly string[] Attacks =
            {
                "caitlynheadshotmissile", "frostarrow", "garenslash2",
                "kennenmegaproc", "masteryidoublestrike", "quinnwenhanced",
                "renektonexecute", "renektonsuperexecute",
                "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust",
                "xenzhaothrust2", "xenzhaothrust3", "viktorqbuff",
                "lucianpassiveshot"
            };

        private static readonly string[] NoAttacks =
            {
                "volleyattack", "volleyattackwithsound",
                "jarvanivcataclysmattack", "monkeykingdoubleattack",
                "shyvanadoubleattack", "shyvanadoubleattackdragon",
                "zyragraspingplantattack", "zyragraspingplantattack2",
                "zyragraspingplantattackfire", "zyragraspingplantattack2fire",
                "viktorpowertransfer", "sivirwattackbounce", "asheqattacknoonhit",
                "elisespiderlingbasicattack", "heimertyellowbasicattack",
                "heimertyellowbasicattack2", "heimertbluebasicattack",
                "annietibbersbasicattack", "annietibbersbasicattack2",
                "yorickdecayedghoulbasicattack", "yorickravenousghoulbasicattack",
                "yorickspectralghoulbasicattack", "malzaharvoidlingbasicattack",
                "malzaharvoidlingbasicattack2", "malzaharvoidlingbasicattack3",
                "kindredwolfbasicattack", "gravesautoattackrecoil"
            };

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

        internal static IEnumerable<Vector3> FlashPoints()
        {
            var points = new List<Vector3>();

            for (var i = 1; i <= 360; i++)
            {
                var angle = i * 2 * Math.PI / 360;
                var point = new Vector3(ObjectManager.GetLocalPlayer().Position.X + 425f * (float)Math.Cos(angle),
                    ObjectManager.GetLocalPlayer().Position.Y + 425f * (float)Math.Sin(angle), ObjectManager.GetLocalPlayer().Position.Z);

                points.Add(point);
            }

            return points;
        }

        internal static bool CanCastR(Obj_AI_Hero target)
        {
            return target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Knockback);
        }

        public static bool IsAutoAttack(this SpellData spellData)
        {
            return IsAutoAttack(spellData.Name);
        }

        public static bool IsAutoAttack(string name)
        {
            return name.ToLower().Contains("attack") && !NoAttacks.Contains(name.ToLower())
                   || Attacks.Contains(name.ToLower());
        }

        public static bool IsValid<T>(this GameObject obj) where T : GameObject
        {
            return obj is T && obj.IsValid;
        }

        public static bool IsReady(this Aimtec.SDK.Spell spell, int t = 0)
        {
            return spell != null && spell.Slot != SpellSlot.Unknown && t == 0
                ? ObjectManager.GetLocalPlayer().SpellBook.GetSpellState(spell.Slot) == SpellState.Ready
                : ObjectManager.GetLocalPlayer().SpellBook.GetSpellState(spell.Slot) == SpellState.Ready
                  ||
                  ObjectManager.GetLocalPlayer().SpellBook.GetSpellState(spell.Slot) == SpellState.Cooldown &&
                  ObjectManager.GetLocalPlayer().SpellBook.GetSpell(spell.Slot).CooldownEnd - Game.ClockTime <=
                  t / 1000f;
        }

        internal static Obj_AI_Base GetNearObj()
        {
            var pos = Game.CursorPos;
            var obj = new List<Obj_AI_Base>();

            obj.AddRange(GameObjects.EnemyMinions.Where(x => x.IsValidTarget(475) && x.MaxHealth > 5));
            obj.AddRange(GameObjects.EnemyHeroes.Where(i => i.IsValidTarget(475)));

            return obj.Where(i => pos.Distance(i.ServerPosition) < ObjectManager.GetLocalPlayer().Distance(pos) && IsSafePosition(i.ServerPosition))
                    .MinOrDefault(i => pos.Distance(ObjectManager.GetLocalPlayer()));
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

        public static bool IsSafePosition(this Vector2 pos)
        {
            return MyLogic.MiscMenu["FlowersFiora.MiscMenu.CheckSafe"].Enabled == false ||
                   MyEvade.EvadeManager.IsSafe(pos).IsSafe;
        }

        public static bool IsSafePosition(this Vector3 pos)
        {
            return MyLogic.MiscMenu["FlowersFiora.MiscMenu.CheckSafe"].Enabled == false ||
                   MyEvade.EvadeManager.IsSafe(pos.To2D()).IsSafe;
        }

        public static bool IsInRange(this Aimtec.SDK.Spell spell, GameObject obj, float range = -1)
        {
            return spell.IsInRange(
                (obj as Obj_AI_Base)?.ServerPosition.To2D() ?? obj.ServerPosition.To2D(),
                range);
        }

        public static bool IsInRange(this Aimtec.SDK.Spell spell, Vector3 point, float range = -1)
        {
            return spell.IsInRange(point.To2D(), range);
        }

        public static bool IsInRange(this Aimtec.SDK.Spell spell, Vector2 point, float range = -1)
        {
            return ObjectManager.GetLocalPlayer().ServerPosition.To2D().Distance(point, true) < range * range;
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

        internal static bool UnderTower(Vector3 pos)
        {
            return pos.PointUnderEnemyTurret();
        }

        public static double GetPassiveDamage(Obj_AI_Hero target, int passiveCount = 0)
        {
            var passive = (0.03f +
                           Math.Min(
                               Math.Max(0.028f,
                                   0.027 +
                                   0.001f * ObjectManager.GetLocalPlayer().Level *
                                   ObjectManager.GetLocalPlayer().FlatPhysicalDamageMod / 100f), 0.45f)) *
                          target.MaxHealth;

            return passiveCount * passive;
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
                damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.Q) +
                    ObjectManager.GetLocalPlayer().GetAutoAttackDamage(target);
            }

            if (MyLogic.W.Ready)
            {
                damage += ObjectManager.GetLocalPlayer().GetSpellDamage(target, SpellSlot.W);
            }

            if (MyLogic.E.Ready)
            {
                damage += ObjectManager.GetLocalPlayer().GetAutoAttackDamage(target) +
                          ObjectManager.GetLocalPlayer().GetAutoAttackDamage(target) * 1.6f;
            }

            if (MyLogic.R.Ready)
            {
                if (target.Type == GameObjectType.obj_AI_Hero)
                {
                    damage += GetPassiveDamage(target, 4) + ObjectManager.GetLocalPlayer().GetAutoAttackDamage(target) * 2.5;
                }
            }

            if (MyPassiveManager.PassiveCount(target) > 0)
            {
                damage += GetPassiveDamage(target, 1) + ObjectManager.GetLocalPlayer().GetAutoAttackDamage(target);
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
    }
}