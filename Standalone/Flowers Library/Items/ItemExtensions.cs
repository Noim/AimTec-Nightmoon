namespace Flowers_Library
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;

    using System;
    using System.Linq;

    #endregion

    public static class ItemExtensions
    {
        public static bool HasItem(this Obj_AI_Hero source, uint itemID)
        {
            if (source == null)
            {
                return false;
            }

            var slot = source.Inventory.Slots.FirstOrDefault(x => x.ItemId == itemID);
            if (slot != null && slot.SpellSlot != SpellSlot.Unknown)
            {
                return true;
            }

            return false;
        }

        public static bool HasItem(this Obj_AI_Hero source, string itemName)
        {
            if (source == null || string.IsNullOrEmpty(itemName))
            {
                return false;
            }

            var slot =
                source.Inventory.Slots.FirstOrDefault(
                    x => string.Equals(itemName, x.SpellName, StringComparison.CurrentCultureIgnoreCase));
            if (slot != null && slot.SpellSlot != SpellSlot.Unknown)
            {
                return true;
            }

            return false;
        }

        public static SpellSlot GetItemSlot(this Obj_AI_Hero source, string itemName)
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

        public static bool CanUseItem(this Obj_AI_Hero source, string itemName)
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

        public static void UseItem(this Obj_AI_Hero source, Obj_AI_Hero target, string itemName)
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

        public static void UseItem(this Obj_AI_Hero source, Vector3 position, string itemName)
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

        public static void UseItem(this Obj_AI_Hero source, string itemName)
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

        public static SpellSlot GetItemSlot(this Obj_AI_Hero source, uint itemID)
        {
            if (source == null)
            {
                return SpellSlot.Unknown;
            }

            var slot = source.Inventory.Slots.FirstOrDefault(x => x.ItemId == itemID);
            if (slot != null && slot.SpellSlot != SpellSlot.Unknown)
            {
                return slot.SpellSlot;
            }

            return SpellSlot.Unknown;
        }

        public static bool CanUseItem(this Obj_AI_Hero source, uint itemID)
        {
            if (source == null)
            {
                return false;
            }

            var slot = source.GetItemSlot(itemID);
            if (slot != SpellSlot.Unknown)
            {
                return source.SpellBook.GetSpellState(slot) == SpellState.Ready;
            }

            return false;
        }

        public static bool UseItem(this Obj_AI_Hero source, uint itemID, Obj_AI_Base target)
        {
            if (source == null || target == null || !target.IsValidTarget())
            {
                return false;
            }

            var slot = source.GetItemSlot(itemID);
            if (slot != SpellSlot.Unknown && source.CanUseItem(itemID))
            {
                return source.SpellBook.CastSpell(slot, target);
            }

            return false;
        }

        public static bool UseItem(this Obj_AI_Hero source, uint itemID, Vector3 position)
        {
            if (source == null || position == Vector3.Zero)
            {
                return false;
            }

            var slot = source.GetItemSlot(itemID);
            if (slot != SpellSlot.Unknown && source.CanUseItem(itemID))
            {
                return source.SpellBook.CastSpell(slot, position);
            }

            return false;
        }

        public static bool UseItem(this Obj_AI_Hero source, uint itemID)
        {
            if (source == null)
            {
                return false;
            }

            var slot = source.GetItemSlot(itemID);
            if (slot != SpellSlot.Unknown && source.CanUseItem(itemID))
            {
                return source.SpellBook.CastSpell(slot);
            }

            return false;
        }

        public static double GetItemDamage(this Obj_AI_Hero source, uint itemID, Obj_AI_Hero target)
        {
            if (!source.HasItem(itemID) || !source.CanUseItem(itemID) || 
                source.IsDead || target == null || !target.IsValidTarget())
            {
                return 0d;
            }

            switch (itemID)
            {
                case 3153: //BladeoftheRuinedKing 破败
                    return source.CalculateDamage(target, DamageType.Magical, 100);
                case 3030: //HextechGLP800 冰冻枪
                    var GLP800DMG = 100 + source.Level * 5.5 + 0.35 * source.FlatMagicDamageMod + 1;
                    return source.CalculateDamage(target, DamageType.Magical, GLP800DMG);
                case 3144: //BilgewaterCutlass 比尔沃特吉弯刀
                    return source.CalculateDamage(target, DamageType.Magical, 100);
                case 3146: //HextechGunblade 科技枪
                    var HexGunDMG = 175 + source.Level * 4.1 + 0.30 * source.FlatMagicDamageMod + 1;
                    return source.CalculateDamage(target, DamageType.Magical, HexGunDMG);
                case 3152: //HextechProtobelt01 推推棒
                    var HexProDMG = 150 + 0.25 * source.FlatMagicDamageMod;
                    return source.CalculateDamage(target, DamageType.Magical, HexProDMG);
                default:
                    return 0d;
            }
        }
    }
}
