namespace Flowers_Library
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;

    using System.Linq;

    #endregion

    public class Item
    {
        public uint ItemID { get; set; }
        public float Range { get; set; }
        public Obj_AI_Hero Owner { get; set; }

        public Item(uint id, float range = float.MaxValue, Obj_AI_Hero owner = null)
        {
            this.ItemID = id;
            this.Range = range;
            this.Owner = owner ?? ObjectManager.GetLocalPlayer();
        }

        public bool IsMine => Owner.HasItem(ItemID);

        public SpellSlot Slot
        {
            get
            {
                return
                    Owner.Inventory.Slots.Where(x => x.ItemId == this.ItemID).Select(x => x.SpellSlot).FirstOrDefault();
            }
        }

        public bool Cast()
        {
            return Owner.UseItem(ItemID);
        }

        public bool CastOnPosition(Vector3 pos)
        {
            return Owner.UseItem(ItemID, pos);
        }

        public bool CastOnPosition(Vector2 pos)
        {
            var newPos = new Vector3(pos, ObjectManager.GetLocalPlayer().ServerPosition.Z);
            return this.CastOnPosition(newPos);
        }

        public bool CastOnUnit(Obj_AI_Base target)
        {
            return Owner.UseItem(ItemID, target);
        }

        public bool Ready
        {
            get { return Owner.CanUseItem(ItemID); }
        }

        public bool IsInRange(Vector3 pos)
        {
            return Owner.ServerPosition.DistanceSquared(pos) <= this.Range * this.Range;
        }

        public bool IsInRange(Vector2 pos)
        {
            Vector3 newPos = new Vector3(pos, ObjectManager.GetLocalPlayer().ServerPosition.Z);
            return this.IsInRange(newPos);
        }

        public double GetDamage(Obj_AI_Hero target)
        {
            return Owner.GetItemDamage(ItemID, target);
        }
    }
}
