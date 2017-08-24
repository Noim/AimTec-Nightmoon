namespace Flowers_Library
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;

    using System.Linq;

    #endregion

    public class Item
    {
        public uint itemID { get; set; }
        public float range { get; set; }
        public Obj_AI_Hero Owner { get; set; }

        public Item(uint id, float range = float.MaxValue, Obj_AI_Hero owner = null)
        {
            this.itemID = id;
            this.range = range;
            this.Owner = owner ?? ObjectManager.GetLocalPlayer();
        }

        public bool IsMine
        {
            get
            {
                return Owner.HasItem(itemID);
            }
        }

        public SpellSlot Slot
        {
            get
            {
                return
                    Owner.Inventory.Slots.Where(x => x.ItemId == this.itemID).Select(x => x.SpellSlot).FirstOrDefault();
            }
        }

        public bool Cast()
        {
            return Owner.UseItem(itemID);
        }

        public bool CastOnPosition(Vector3 pos)
        {
            return Owner.UseItem(itemID, pos);
        }

        public bool CastOnPosition(Vector2 pos)
        {
            Vector3 newPos = new Vector3(pos, ObjectManager.GetLocalPlayer().ServerPosition.Z);
            return this.CastOnPosition(newPos);
        }

        public bool CastOnUnit(Obj_AI_Base target)
        {
            return Owner.UseItem(itemID, target);
        }

        public bool Ready
        {
            get { return Owner.CanUseItem(itemID); }
        }

        public bool IsInRange(Vector3 pos)
        {
            return Owner.ServerPosition.DistanceSquared(pos) <= this.range * this.range;
        }

        public bool IsInRange(Vector2 pos)
        {
            Vector3 newPos = new Vector3(pos, ObjectManager.GetLocalPlayer().ServerPosition.Z);
            return this.IsInRange(newPos);
        }

        public double GetDamage(Obj_AI_Hero target)
        {
            return Owner.GetItemDamage(itemID, target);
        }
    }
}
