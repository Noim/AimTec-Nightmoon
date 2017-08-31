namespace Flowers_Ryze.MyCommon
{
    #region 

    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;

    using Flowers_Ryze.MyBase;

    using System;

    #endregion

    internal class MyMenuManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Menu = new Menu("FlowersRyze", "Flowers Ryze", true);
                {
                    MyLogic.Menu.Add(new MenuSeperator("CreditName", "Made by NightMoon"));
                    MyLogic.Menu.Add(new MenuSeperator("willbeRemove", "This Assembly will be Remove"));
                    MyLogic.Menu.Add(new MenuSeperator("inthisassembly", "Use SharpAIO (new Version)"));
                    MyLogic.Menu.Add(new MenuSeperator("ASDASDF"));
                }

                MyLogic.Orbwalker = new Aimtec.SDK.Orbwalking.Orbwalker();
                MyLogic.Orbwalker.Attach(MyLogic.Menu);

                MyLogic.ComboMenu = new Menu("FlowersRyze.ComboMenu", ":: Combo Settings");
                {
                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRyze.ComboMenu.QSettings", "-- Q Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRyze.ComboMenu.Q", "Use Q"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRyze.ComboMenu.QSmart", "Use Q| Smart Ignore Collsion"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRyze.ComboMenu.WSettings", "-- W Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRyze.ComboMenu.W", "Use W"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRyze.ComboMenu.ESettings", "-- E Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRyze.ComboMenu.E", "Use E"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRyze.ComboMenu.ModeSettings", "-- Mode Settings"));
                    MyLogic.ComboMenu.Add(new MenuList("FlowersRyze.ComboMenu.Mode", "Combo Mode: ",
                        new[] {"Smart", "Shield", "Burst"}, 0));
                    MyLogic.ComboMenu.Add(new MenuKeyBind("FlowersRyze.ComboMenu.ModeKey", "Switch Mode Key: ",
                            Aimtec.SDK.Util.KeyCode.G, KeybindType.Press)).OnValueChanged +=
                        delegate(MenuComponent iMenuComponent, ValueChangedArgs Args)
                        {
                            if (Args.GetNewValue<MenuKeyBind>().Enabled)
                            {
                                switch (MyLogic.ComboMenu["FlowersRyze.ComboMenu.Mode"].As<MenuList>().Value)
                                {
                                    case 0:
                                        MyLogic.ComboMenu["FlowersRyze.ComboMenu.Mode"].As<MenuList>().Value = 1;
                                        break;
                                    case 1:
                                        MyLogic.ComboMenu["FlowersRyze.ComboMenu.Mode"].As<MenuList>().Value = 2;
                                        break;
                                    case 2:
                                        MyLogic.ComboMenu["FlowersRyze.ComboMenu.Mode"].As<MenuList>().Value = 0;
                                        break;
                                }
                            }
                        };
                    MyLogic.ComboMenu.Add(new MenuSliderBool("FlowersRyze.ComboMenu.ShieldHP",
                        "Smart Mode Auto Shield| When Player HealthPercent <= x%", true, 60, 0, 100));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRyze.ComboMenu.OtherSettings", "-- Other Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRyze.ComboMenu.Ignite", "Use Ignite"));
                    MyLogic.ComboMenu.Add(new MenuList("FlowersRyze.ComboMenu.DisableAttack",
                        "Disable Attack in Combo: ",
                        new[] {"Smart Mode", "Always", "No"}, 0));
                }
                MyLogic.Menu.Add(MyLogic.ComboMenu);

                MyLogic.HarassMenu = new Menu("FlowersRyze.HarassMenu", ":: Harass Settings");
                {
                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersRyze.HarassMenu.QSettings", "-- Q Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersRyze.HarassMenu.Q", "Use Q"));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersRyze.HarassMenu.WSettings", "-- W Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersRyze.HarassMenu.W", "Use W", false));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersRyze.HarassMenu.ESettings", "-- E Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersRyze.HarassMenu.E", "Use E"));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersRyze.HarassMenu.ManaSettings", "-- Mana Settings"));
                    MyLogic.HarassMenu.Add(new MenuSlider("FlowersRyze.HarassMenu.Mana",
                        "When Player ManaPercent >= x%", 60, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.HarassMenu);

                MyLogic.ClearMenu = new Menu("FlowersRyze.ClearMenu", ":: Clear Settings");
                {
                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersRyze.ClearMenu.LaneClearSettings", "-- LaneClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRyze.ClearMenu.LaneClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRyze.ClearMenu.LaneClearW", "Use W"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRyze.ClearMenu.LaneClearE", "Use E"));
                    MyLogic.ClearMenu.Add(new MenuSlider("FlowersRyze.ClearMenu.LaneClearMana",
                        "When Player ManaPercent >= x%", 30, 1, 99));

                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersRyze.ClearMenu.JungleClearSettings", "-- JungleClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRyze.ClearMenu.JungleClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRyze.ClearMenu.JungleClearW", "Use W"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRyze.ClearMenu.JungleClearE", "Use E"));
                    MyLogic.ClearMenu.Add(new MenuSlider("FlowersRyze.ClearMenu.JungleClearMana",
                        "When Player ManaPercent >= x%", 30, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.ClearMenu);

                MyLogic.LastHitMenu = new Menu("FlowersRyze.LastHitMenu", ":: LastHit Settings");
                {
                    MyLogic.LastHitMenu.Add(new MenuSeperator("FlowersRyze.LastHitMenu.QSettings", "-- Q Settings"));
                    MyLogic.LastHitMenu.Add(new MenuBool("FlowersRyze.LastHitMenu.LastHitQ", "Use Q"));
                    MyLogic.LastHitMenu.Add(new MenuSlider("FlowersRyze.LastHitMenu.LastHitMana",
                        "When Player ManaPercent >= x%", 30, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.LastHitMenu);

                MyLogic.KillStealMenu = new Menu("FlowersRyze.KillStealMenu", ":: KillSteal Settings");
                {
                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersRyze.KillStealMenu.QSettings", "-- Q Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersRyze.KillStealMenu.Q", "Use Q"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersRyze.KillStealMenu.WSettings", "-- W Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersRyze.KillStealMenu.W", "Use W"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersRyze.KillStealMenu.ESettings", "-- E Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersRyze.KillStealMenu.E", "Use E"));
                }
                MyLogic.Menu.Add(MyLogic.KillStealMenu);

                MyLogic.MiscMenu = new Menu("FlowersRyze.MiscMenu", ":: Misc Settings");
                {
                    MyManaManager.AddFarmToMenu(MyLogic.MiscMenu);
                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersRyze.MiscMenu.WSettings", "-- W Settings"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersRyze.MiscMenu.WMelee", "Auto W| Anti Melee"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersRyze.MiscMenu.WRengar", "Auto W| Anti Rengar"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersRyze.MiscMenu.WKhazix", "Auto W| Anti Khazix"));
                }
                MyLogic.Menu.Add(MyLogic.MiscMenu);

                MyLogic.DrawMenu = new Menu("FlowersRyze.DrawMenu", ":: Draw Settings");
                {
                    MyManaManager.AddDrawToMenu(MyLogic.DrawMenu);
                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersRyze.DrawMenu.RangeSettings", "-- Spell Range"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRyze.DrawMenu.Q", "Draw Q Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRyze.DrawMenu.W", "Draw W Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRyze.DrawMenu.E", "Draw E Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRyze.DrawMenu.R", "Draw R Range", false));

                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersRyze.DrawMenu.StatusSettings", "-- Logic Status"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRyze.DrawMenu.Combo", "Draw Combo Status"));
                }
                MyLogic.Menu.Add(MyLogic.DrawMenu);

                MyLogic.Menu.Attach();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyMenuManager.Initializer." + ex);
            }
        }
    }
}