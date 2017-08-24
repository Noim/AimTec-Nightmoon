namespace SharpShooter.MyUtility
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.TargetSelector;

    using Flowers_Library;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System.Linq;

    #endregion

    internal class MyItemManager
    {
        #region Potion

        private static Item HealthPotion { get; set; }
        private static Item TotalBiscuitofRejuvenation { get; set; }
        private static Item RefillablePotion { get; set; }
        private static Item HuntersPotion { get; set; }
        private static Item CorruptingPotion { get; set; }

        #endregion

        #region Offensive

        private static Item Botrk { get; set; }
        private static Item Cutlass { get; set; }
        private static Item Youmuus { get; set; }
        private static Item Hextech { get; set; }

        #endregion

        #region Defence

        private static Item Randuin { get; set; }

        #endregion

        #region Cleanse

        private static Item QSS { get; set; }
        private static Item Scimitar { get; set; }

        #endregion

        #region Tear

        private static Item Tear { get; set; }
        private static Item TearQuick { get; set; }
        private static Item Archangels { get; set; }
        private static Item ArchangelsQuick { get; set; }
        private static Item Manamune { get; set; }
        private static Item ManamuneQuick { get; set; }

        #endregion

        #region Menu

        private static Menu Menu;
        private static Menu AutoPotionsMenu;
        private static Menu ItemsMenu;
        private static Menu CleanseMenu;
        private static Menu AutoStackMenu;
        
        #endregion

        public MyItemManager()
        {
            Initializer();
        }

        private static void Initializer()
        {
            HealthPotion = new Item(2003);
            TotalBiscuitofRejuvenation = new Item(2010);
            RefillablePotion = new Item(2031);
            HuntersPotion = new Item(2032);
            CorruptingPotion = new Item(2033);

            Botrk = new Item(3153, 550f);
            Cutlass = new Item(3144, 550f);
            Youmuus = new Item(3142, 650f);
            Hextech = new Item(3146, 700f);

            Randuin = new Item(3143, 400f);

            QSS = new Item(3140);
            Scimitar = new Item(3139);

            Tear = new Item(3070);
            TearQuick = new Item(3073);
            Archangels = new Item(3003);
            ArchangelsQuick = new Item(3007);
            Manamune = new Item(3003);
            ManamuneQuick = new Item(3007);

            Menu = MyMenuExtensions.UtilityMenu;

            #region Auto Potions

            AutoPotionsMenu = new Menu("SharpShooter.UtilityMenu.AutoPotionsMenu", "Auto Potions")
            {
                new MenuBool("SharpShooter.UtilityMenu.AutoPotionsMenu.Enabled", "Enabled"),
                new MenuSlider("SharpShooter.UtilityMenu.AutoPotionsMenu.MyHp", "When Player HealthPercent <= x%", 35)
            };
            Menu.Add(AutoPotionsMenu);

            AutoPotionsClass.Initializer();

            #endregion

            #region Items

            ItemsMenu = new Menu("SharpShooter.UtilityMenu.ItemsMenu", "Items")
            {
                new MenuBool("SharpShooter.UtilityMenu.ItemsMenu.Enabled", "Enabled"),

                new MenuSeperator("SharpShooter.UtilityMenu.ItemsMenu.YoumuusSettings", "Youmuus Ghostblade"),
                new MenuBool("SharpShooter.UtilityMenu.ItemsMenu.YoumuusEnabled", "Enabled"),

                new MenuSeperator("SharpShooter.UtilityMenu.ItemsMenu.CutlassSettings", "Bilgewater Cutlass"),
                new MenuBool("SharpShooter.UtilityMenu.ItemsMenu.CutlassEnabled", "Enabled"),

                new MenuSeperator("SharpShooter.UtilityMenu.ItemsMenu.BOTRKSettings", "Blade of the Ruined King"),
                new MenuBool("SharpShooter.UtilityMenu.ItemsMenu.BOTRKEnabled", "Enabled"),

                new MenuSeperator("SharpShooter.UtilityMenu.ItemsMenu.GunbladeSettings", "Hextech Gunblade"),
                new MenuBool("SharpShooter.UtilityMenu.ItemsMenu.GunbladeEnabled", "Enabled"),

                new MenuSeperator("SharpShooter.UtilityMenu.ItemsMenu.RanduinsOmenSettings", "Randuins Omen"),
                new MenuBool("SharpShooter.UtilityMenu.ItemsMenu.RanduinsOmenEnabled", "Enabled"),
            };
            Menu.Add(ItemsMenu);

            ItemsClass.Initializer();

            #endregion

            #region Cleanse

            CleanseMenu = new Menu("SharpShooter.UtilityMenu.CleanseMenu", "Cleanse")
            {
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.Enabled", "Enabled"),
                new MenuSlider("SharpShooter.UtilityMenu.CleanseMenu.MyHp", "When Player HealthPercent <= x%", 90, 1, 101),
                new MenuSlider("SharpShooter.UtilityMenu.CleanseMenu.Duration", "Debuff Duration (ms)", 800, 0, 3000),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.OnlyCombo", "Only Combo Active"),
                new MenuSeperator("SharpShooter.UtilityMenu.CleanseMenu.CCType", "CC Type"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseBlind", "Cleanse Blind"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseCharm", "Cleanse Charm"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseDisarm", "Cleanse Disarm"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseExhaust", "Cleanse Exhaust"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseFear", "Cleanse Fear"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseFlee", "Cleanse Flee"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleansePolymorph", "Cleanse Polymorph"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseSnare", "Cleanse Snare"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseStun", "Cleanse Stun"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseSuppression", "Cleanse Suppression"),
                new MenuBool("SharpShooter.UtilityMenu.CleanseMenu.CleanseTaunt", "Cleanse Taunt")
            };
            Menu.Add(CleanseMenu);

            CleanseClass.Initializer();

            #endregion

            #region Auto Stack

            AutoStackMenu = new Menu("SharpShooter.UtilityMenu.AutoStackMenu", "Auto Stack")
            {
                new MenuBool("SharpShooter.UtilityMenu.AutoStackMenu.Enabled"+ ObjectManager.GetLocalPlayer().ChampionName, "Enabled", false),
                new MenuSlider("SharpShooter.UtilityMenu.AutoStackMenu.MyMp" + ObjectManager.GetLocalPlayer().ChampionName, "When Player ManaPercent => x%", 80),
                new MenuBool("SharpShooter.UtilityMenu.AutoStackMenu.BuleBuff" + ObjectManager.GetLocalPlayer().ChampionName, "If Have Bule Buff Ignore Mana Check"),
                new MenuBool("SharpShooter.UtilityMenu.AutoStackMenu.InFountain" + ObjectManager.GetLocalPlayer().ChampionName, "If In Fountain Ignore Mana Check"),
                new MenuBool("SharpShooter.UtilityMenu.AutoStackMenu.Q" + ObjectManager.GetLocalPlayer().ChampionName, "Use Q Stack", false),
                new MenuBool("SharpShooter.UtilityMenu.AutoStackMenu.W" + ObjectManager.GetLocalPlayer().ChampionName, "Use W Stack", false),
                new MenuBool("SharpShooter.UtilityMenu.AutoStackMenu.E" + ObjectManager.GetLocalPlayer().ChampionName, "Use E Stack", false),
                new MenuBool("SharpShooter.UtilityMenu.AutoStackMenu.R" + ObjectManager.GetLocalPlayer().ChampionName, "Use R Stack", false)
            };
            Menu.Add(AutoStackMenu);

            AutoStackClass.Initializer();

            #endregion
        }

        public class AutoPotionsClass
        {
            private static readonly string[] buffName =
            {
                "recall", "teleport", "regenerationpotion", "itemminiregenpotion",
                "itemcrystalflask", "itemcrystalflaskjungle", "itemdarkcrystalflask"
            };

            internal static void Initializer()
            {
                Game.OnUpdate += Potions;
            }

            private static void Potions()
            {
                if (ObjectManager.GetLocalPlayer().IsDead || ObjectManager.GetLocalPlayer().IsInFountainRange() || 
                    ObjectManager.GetLocalPlayer().HasBuff("recall"))
                {
                    return;
                }

                if (!AutoPotionsMenu["SharpShooter.UtilityMenu.AutoPotionsMenu.Enabled"].Enabled ||
                    ObjectManager.GetLocalPlayer().HealthPercent() > AutoPotionsMenu["SharpShooter.UtilityMenu.AutoPotionsMenu.MyHp"].Value ||
                    ObjectManager.GetLocalPlayer().CountEnemyHeroesInRange(1200) <= 0)
                {
                    return;
                }

                if (ObjectManager.GetLocalPlayer().Buffs.Any(x => x != null && buffName.Contains(x.Name.ToLower()) && x.IsActive))
                {
                    return;
                }

                if (HealthPotion != null && HealthPotion.IsMine && HealthPotion.Ready)
                {
                    HealthPotion.Cast();
                }
                else if (TotalBiscuitofRejuvenation != null && TotalBiscuitofRejuvenation.IsMine && TotalBiscuitofRejuvenation.Ready)
                {
                    TotalBiscuitofRejuvenation.Cast();
                }
                else if (RefillablePotion != null && RefillablePotion.IsMine && RefillablePotion.Ready)
                {
                    RefillablePotion.Cast();
                }
                else if (HuntersPotion != null && HuntersPotion.IsMine && HuntersPotion.Ready)
                {
                    HuntersPotion.Cast();
                }
                else if (CorruptingPotion != null && CorruptingPotion.IsMine && CorruptingPotion.Ready)
                {
                    CorruptingPotion.Cast();
                }
            }
        }

        public class ItemsClass
        {
            internal static void Initializer()
            {
                Game.OnUpdate += Items;
                SpellBook.OnCastSpell += OnCastSpell;
            }

            private static void OnCastSpell(Obj_AI_Base sender, SpellBookCastSpellEventArgs Args)
            {
                if (sender.IsMe)
                {
                    switch (ObjectManager.GetLocalPlayer().ChampionName)
                    {
                        case "Lucian":
                        case "Twitch":
                            {
                                if (Args.Slot == SpellSlot.R)
                                {
                                    if (ObjectManager.GetLocalPlayer().IsDead ||
                                        ObjectManager.GetLocalPlayer().IsRecalling() ||
                                        ObjectManager.GetLocalPlayer().IsInFountainRange() ||
                                        !ItemsMenu["SharpShooter.UtilityMenu.ItemsMenu.Enabled"].Enabled)
                                    {
                                        return;
                                    }

                                    if (ItemsMenu["SharpShooter.UtilityMenu.ItemsMenu.YoumuusEnabled"].Enabled &&
                                        Youmuus.IsMine && Youmuus.Ready)
                                    {
                                        Youmuus.Cast();
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            private static void Items()
            {
                if (ObjectManager.GetLocalPlayer().IsDead || ObjectManager.GetLocalPlayer().IsRecalling() ||
                    ObjectManager.GetLocalPlayer().IsInFountainRange() ||
                    !ItemsMenu["SharpShooter.UtilityMenu.ItemsMenu.Enabled"].Enabled)
                {
                    return;
                }

                #region Youmuu

                if (ItemsMenu["SharpShooter.UtilityMenu.ItemsMenu.YoumuusEnabled"].Enabled && Youmuus.IsMine && Youmuus.Ready)
                {
                    if (ObjectManager.GetLocalPlayer().ChampionName == "Twitch")
                    {
                        if (ObjectManager.GetLocalPlayer().Buffs.Any(x => x.Name.ToLower() == "twitchfullautomatic"))
                        {
                            if (MyLogic.Orbwalker.GetOrbwalkingTarget() != null)
                            {
                                Youmuus.Cast();
                                return;
                            }
                        }
                    }

                    var target = GetTarget(800);

                    if (target != null && target.IsValidAutoRange() && MyLogic.Orbwalker.Mode == OrbwalkingMode.Combo)
                    {
                        Youmuus.Cast();
                        return;
                    }
                }

                #endregion

                #region Cutlass

                if (ItemsMenu["SharpShooter.UtilityMenu.ItemsMenu.CutlassEnabled"].Enabled && Cutlass.IsMine && Cutlass.Ready)
                {
                    var target = GetTarget(Cutlass.Range);

                    if (target != null && target.IsValidTarget(Cutlass.Range))
                    {
                        if (Cutlass.GetDamage(target) > target.Health)
                        {
                            Cutlass.CastOnUnit(target);
                            return;
                        }

                        if (MyLogic.Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            if (target.IsValidAutoRange())
                            {
                                Cutlass.CastOnUnit(target);
                                return;
                            }
                        }
                    }
                }

                #endregion

                #region Botrk

                if (ItemsMenu["SharpShooter.UtilityMenu.ItemsMenu.BOTRKEnabled"].Enabled && Botrk.IsMine && Botrk.Ready)
                {
                    var target = GetTarget(Botrk.Range);

                    if (target != null && target.IsValidTarget(Botrk.Range))
                    {
                        if (Botrk.GetDamage(target) > target.Health)
                        {
                            Botrk.CastOnUnit(target);
                            return;
                        }

                        if (MyLogic.Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            if (target.IsValidAutoRange())
                            {
                                Botrk.CastOnUnit(target);
                                return;
                            }
                        }
                    }
                }

                #endregion

                #region Hextech

                if (ItemsMenu["SharpShooter.UtilityMenu.ItemsMenu.GunbladeEnabled"].Enabled && Hextech.IsMine && Hextech.Ready)
                {
                    var target = GetTarget(Hextech.Range);

                    if (target != null && target.IsValidTarget(Hextech.Range))
                    {
                        if (Hextech.GetDamage(target) > target.Health)
                        {
                            Hextech.CastOnUnit(target);
                            return;
                        }

                        if (MyLogic.Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            if (target.IsValidAutoRange())
                            {
                                if (target.HealthPercent() <= 80)
                                {
                                    Hextech.CastOnUnit(target);
                                    return;
                                }
                            }
                        }
                    }
                }

                #endregion

                #region Randuin

                if (ItemsMenu["SharpShooter.UtilityMenu.ItemsMenu.RanduinsOmenEnabled"].Enabled && Randuin.IsMine && Randuin.Ready)
                {
                    if (MyLogic.Orbwalker.Mode == OrbwalkingMode.Combo)
                    {
                        if (ObjectManager.GetLocalPlayer().CountEnemyHeroesInRange(Randuin.Range) >= 3)
                        {
                            Randuin.Cast();
                        }
                    }
                }

                #endregion
            }

            private static Obj_AI_Hero GetTarget(float range)
            {
                return TargetSelector.GetTarget(range, true);
            }
        }

        public class CleanseClass
        {
            private static bool CanUse;

            internal static void Initializer()
            {
                Game.OnUpdate += OnUpdate;
                BuffManager.OnAddBuff += OnAddBuff;
            }

            private static void OnUpdate()
            {
                if (ObjectManager.GetLocalPlayer().IsDead || ObjectManager.GetLocalPlayer().IsRecalling() ||
                    ObjectManager.GetLocalPlayer().IsInFountainRange())
                {
                    return;
                }

                if (!CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.Enabled"].Enabled ||
                    ObjectManager.GetLocalPlayer().HealthPercent() >
                    CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.MyHp"].Value)
                {
                    return;
                }

                if (CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.OnlyCombo"].Enabled &&
                    MyLogic.Orbwalker.Mode != OrbwalkingMode.Combo)
                {
                    return;
                }

                if (CanUse)
                {
                    if (QSS.IsMine && QSS.Ready)
                    {
                        QSS.Cast();
                        CanUse = false;
                    }
                    else if (Scimitar.IsMine && Scimitar.Ready)
                    {
                        Scimitar.Cast();
                        CanUse = false;
                    }
                    else
                    {
                        CanUse = false;
                    }
                }
            }

            private static void OnAddBuff(Obj_AI_Base sender, Buff buff)
            {
                if (!sender.IsMe || !CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.Enabled"].Enabled)
                {
                    return;
                }

                if ((buff.EndTime - buff.StartTime) * 1000 >=
                    CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.Duration"].Value)
                {
                    if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Blind) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseBlind"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Charm) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseCharm"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Disarm) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseDisarm"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuff("SummonerExhaust") && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseExhaust"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Fear) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseFear"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Flee) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseFlee"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Polymorph) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleansePolymorph"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Snare) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseSnare"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Stun) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseStun"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Suppression) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseSuppression"].Enabled)
                    {
                        CanUse = true;
                    }
                    else if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.Taunt) && CleanseMenu["SharpShooter.UtilityMenu.CleanseMenu.CleanseTaunt"].Enabled)
                    {
                        CanUse = true;
                    }
                }
            }
        }

        public class AutoStackClass
        {
            private static int lastCastTime;

            internal static void Initializer()
            {
                Game.OnUpdate += AutoStack;
            }

            private static void AutoStack()
            {
                if (ObjectManager.GetLocalPlayer().IsDead || ObjectManager.GetLocalPlayer().IsRecalling() ||
                    !AutoStackMenu[
                            "SharpShooter.UtilityMenu.AutoStackMenu.Enabled" + ObjectManager.GetLocalPlayer().ChampionName]
                        .Enabled)
                {
                    return;
                }

                if (Game.TickCount - lastCastTime < 4100)
                {
                    return;
                }

                if (ObjectManager.GetLocalPlayer().ManaPercent() >=
                    AutoStackMenu[
                            "SharpShooter.UtilityMenu.AutoStackMenu.MyMp" + ObjectManager.GetLocalPlayer().ChampionName]
                        .Value ||
                    ObjectManager.GetLocalPlayer().HasBuff("crestoftheancientgolem") &&
                    AutoStackMenu[
                        "SharpShooter.UtilityMenu.AutoStackMenu.BuleBuff" + ObjectManager.GetLocalPlayer().ChampionName
                    ].Enabled ||
                    ObjectManager.GetLocalPlayer().IsInFountainRange() &&
                    AutoStackMenu[
                        "SharpShooter.UtilityMenu.AutoStackMenu.InFountain" +
                        ObjectManager.GetLocalPlayer().ChampionName].Enabled)
                {
                    if (ObjectManager.Get<Obj_AI_Base>().Any(x => x.IsValidTarget(1800) && x.IsEnemy))
                    {
                        return;
                    }

                    if (
                        AutoStackMenu[
                                "SharpShooter.UtilityMenu.AutoStackMenu.Q" + ObjectManager.GetLocalPlayer().ChampionName]
                            .Enabled && ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.Q).Level > 0 &&
                        ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.Q) &&
                        Game.TickCount - lastCastTime > 4100)
                    {
                        ObjectManager.GetLocalPlayer()
                            .SpellBook.CastSpell(SpellSlot.Q,
                                ObjectManager.GetLocalPlayer().ServerPosition.Extend(Game.CursorPos, 300));
                        lastCastTime = Game.TickCount;
                        return;
                    }

                    if (
                        AutoStackMenu[
                                "SharpShooter.UtilityMenu.AutoStackMenu.W" + ObjectManager.GetLocalPlayer().ChampionName
                            ]
                            .Enabled && ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.W).Level > 0 &&
                        ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.W) &&
                        Game.TickCount - lastCastTime > 4100)
                    {
                        ObjectManager.GetLocalPlayer()
                            .SpellBook.CastSpell(SpellSlot.W,
                                ObjectManager.GetLocalPlayer().ServerPosition.Extend(Game.CursorPos, 300));
                        lastCastTime = Game.TickCount;
                        return;
                    }

                    if (
                        AutoStackMenu[
                                "SharpShooter.UtilityMenu.AutoStackMenu.E" + ObjectManager.GetLocalPlayer().ChampionName
                            ]
                            .Enabled && ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.E).Level > 0 &&
                        ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.E) &&
                        Game.TickCount - lastCastTime > 4100)
                    {
                        ObjectManager.GetLocalPlayer()
                            .SpellBook.CastSpell(SpellSlot.E,
                                ObjectManager.GetLocalPlayer().ServerPosition.Extend(Game.CursorPos, 300));
                        lastCastTime = Game.TickCount;
                        return;
                    }

                    if (
                        AutoStackMenu[
                                "SharpShooter.UtilityMenu.AutoStackMenu.R" + ObjectManager.GetLocalPlayer().ChampionName
                            ]
                            .Enabled && ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.R).Level > 0 &&
                        ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.R) &&
                        Game.TickCount - lastCastTime > 4100)
                    {
                        ObjectManager.GetLocalPlayer()
                            .SpellBook.CastSpell(SpellSlot.R,
                                ObjectManager.GetLocalPlayer().ServerPosition.Extend(Game.CursorPos, 300));
                        lastCastTime = Game.TickCount;
                    }
                }
            }
        }
    }
}
