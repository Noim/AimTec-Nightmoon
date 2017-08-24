namespace Flowers_Katarina.MyBase
{
    #region

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Extensions;

    using System.Collections.Generic;

    #endregion

    internal class MyLogic
    {
        internal static Orbwalker Orbwalker { get; set; }

        internal static Aimtec.SDK.Spell Q { get; set; }
        internal static Aimtec.SDK.Spell W { get; set; }
        internal static Aimtec.SDK.Spell E { get; set; }
        internal static Aimtec.SDK.Spell R { get; set; }
        internal static Aimtec.SDK.Spell Ignite { get; set; }

        internal static SpellSlot IgniteSlot { get; set; } = SpellSlot.Unknown;

        internal static Obj_AI_Hero Me = ObjectManager.GetLocalPlayer();

        internal static Menu Menu { get; set; }
        internal static Menu ComboMenu { get; set; }
        internal static Menu HarassMenu { get; set; }
        internal static Menu ClearMenu { get; set; }
        internal static Menu LastHitMenu { get; set; }
        internal static Menu FleeMenu { get; set; }
        internal static Menu KillStealMenu { get; set; }
        internal static Menu MiscMenu { get; set; }
        internal static Menu DrawMenu { get; set; }

        internal static int lastWTime { get; set; }
        internal static int lastETime { get; set; }
        internal static int lastCancelTime { get; set; }
        internal const int PassiveRange = 340;
        internal static bool isCastingUlt => ObjectManager.GetLocalPlayer().HasBuff("katarinar");

        internal static List<MyDaggerManager> Daggers = new List<MyDaggerManager>();

        public class MyDaggerManager
        {
            public GameObject obj { get; set; }
            public Vector3 pos { get; set; }
            public int time { get; set; }

            public MyDaggerManager(GameObject dagger, Vector3 position, int thetime)
            {
                this.obj = dagger;
                this.pos = position;
                this.time = thetime;
            }
        }

        //科技枪 HextechGunblade - 3146
        //冰冻枪 ItemWillBoltSpellBase - 3030
        //沙漏 ZhonyasHourglass - 3157
        //冰霜女王指令 ItemWraithCollar - 3092
        //推推棒 ItemSoFBoltSpellBase - 3152
        //皇冠 shurelyascrest - 3069
        //黄色视频lv11 TrinketTotemLv1 - 3340
        //扫描 TrinketSweeperLv1 - 3341 - TrinketSweeperLvl3 - 3364
        //灯泡 TrinketOrbLvl3 - 3363
        //150水晶瓶 ItemCrystalFlask - 2031
        //猎人药水 ItemCrystalFlaskJungle - 2032
        //腐蚀药水 ItemDarkCrystalFlask - 2033
        //巫术药剂 ElixirOfSorcery - 2139
        //嗜血药剂 ElixirOfWrath - 2140
        //钢铁药剂 ElixirOfIron - 2138
        //水银腰带 QuicksilverSash - 3140
        //比尔沃特吉弯刀 BilgewaterCutlass - 3144
        //大眼石 ItemGhostWard - 2045
        //皇冠升级为眼石 ItemGhostWard - 2302
        //救赎 ItemRedemption - 3107
        //坩埚 ItemMorellosBane - 3222
        //号令之旗 ItemPromote - 3060
        //幽梦 YoumusBlade - 3142
        //破败 ItemSwordOfFeastAndFamine - 3153
        //九头蛇 ItemTiamatCleave - 3074
        //巨人九头蛇 ItemTitanicHydraCleave - 3748
        //水银弯刀 ItemMercurial - 3139
        //提亚马特 ItemTiamatCleave - 3077
        //传送门 ItemVoidGate - 3512
        //蓝盾 RanduinsOmen - 3143
        //护盾(加攻击的) ItemVeilChannel - 3814
        //石像鬼铠甲 Item3193Active - 3193
    }
}