namespace Flowers_Darius.MyCommon
{
    #region 

    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Darius.MyBase;

    using System;

    #endregion

    internal class MyMenuManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Menu = new Menu("FlowersDarius", "Flowers Darius", true);
                {
                    MyLogic.Menu.Add(new MenuSeperator("CreditName", "Made by NightMoon"));
                    MyLogic.Menu.Add(new MenuSeperator("willbeRemove", "This Assembly will be Remove"));
                    MyLogic.Menu.Add(new MenuSeperator("inthisassembly", "Use SharpAIO (new Version)"));
                    MyLogic.Menu.Add(new MenuSeperator("ASDASDF"));
                }

                MyLogic.Orbwalker = new Aimtec.SDK.Orbwalking.Orbwalker();
                MyLogic.Orbwalker.Attach(MyLogic.Menu);

                MyLogic.ComboMenu = new Menu("FlowersDarius.ComboMenu", ":: Combo Settings");
                {
                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersDarius.ComboMenu.QSettings", "-- Q Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersDarius.ComboMenu.Q", "Use Q"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersDarius.ComboMenu.WSettings", "-- W Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersDarius.ComboMenu.W", "Use W"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersDarius.ComboMenu.ESettings", "-- E Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersDarius.ComboMenu.E", "Use E"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersDarius.ComboMenu.EDash", "Use E| When target is Dashing"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersDarius.ComboMenu.RSettings", "-- R Settings"));
                    MyLogic.ComboMenu.Add(new MenuKeyBind("FlowersDarius.ComboMenu.R", "Use R",
                        Aimtec.SDK.Util.KeyCode.G, KeybindType.Toggle, true));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersDarius.ComboMenu.ModeSettings", "-- Other Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersDarius.ComboMenu.SaveMana", "Save Mana to Cast R"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersDarius.ComboMenu.Ignite", "Use Ignite"));
                }
                MyLogic.Menu.Add(MyLogic.ComboMenu);

                MyLogic.HarassMenu = new Menu("FlowersDarius.HarassMenu", ":: Harass Settings");
                {
                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersDarius.HarassMenu.QSettings", "-- Q Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersDarius.HarassMenu.Q", "Use Q"));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersDarius.HarassMenu.WSettings", "-- W Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersDarius.HarassMenu.W", "Use W", false));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersDarius.HarassMenu.ESettings", "-- E Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersDarius.HarassMenu.E", "Use E", false));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersDarius.HarassMenu.ManaSettings", "-- Mana Settings"));
                    MyLogic.HarassMenu.Add(new MenuSlider("FlowersDarius.HarassMenu.HarassManaPercent", 
                        "When Player ManaPercent >= x%", 60, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.HarassMenu);

                MyLogic.ClearMenu = new Menu("FlowersDarius.ClearMenu", ":: Clear Settings");
                {
                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersDarius.ClearMenu.LaneClearSettings", "-- LaneClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuSliderBool("FlowersDarius.ClearMenu.LaneClearQ",
                        "Use Q| Min Hit Count >= x", true, 3, 1, 5));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersDarius.ClearMenu.LaneClearW", "Use W"));
                    MyLogic.ClearMenu.Add(new MenuSlider("FlowersDarius.ClearMenu.LaneClearManaPercent",
                       "When Player ManaPercent >= x%", 60, 1, 99));

                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersDarius.ClearMenu.JungleClearSettings", "-- JungleClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersDarius.ClearMenu.JungleClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersDarius.ClearMenu.JungleClearW", "Use W"));
                    MyLogic.ClearMenu.Add(new MenuSlider("FlowersDarius.ClearMenu.JungleClearManaPercent",
                       "When Player ManaPercent >= x%", 30, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.ClearMenu);

                MyLogic.KillStealMenu = new Menu("FlowersDarius.KillStealMenu", ":: KillSteal Settings");
                {
                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersDarius.KillStealMenu.QSettings", "-- Q Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersDarius.KillStealMenu.Q", "Use Q"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersDarius.KillStealMenu.ESettings", "-- E Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersDarius.KillStealMenu.E", "Use E"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersDarius.KillStealMenu.RSettings", "-- R Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersDarius.KillStealMenu.R", "Use R"));
                }
                MyLogic.Menu.Add(MyLogic.KillStealMenu);

                MyLogic.MiscMenu = new Menu("FlowersDarius.MiscMenu", ":: Misc Settings");
                {
                    MyManaManager.AddFarmToMenu(MyLogic.MiscMenu);

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersDarius.MiscMenu.QSettings", "-- Q Settings"));
                    MyLogic.MiscMenu.Add(new MenuList("FlowersDarius.MiscMenu.LockQ", "Enabled Lock Q",
                        new[] {"Combo & Harass", "Only Combo", "Only Harass", "Off"}, 1));
                }
                MyLogic.Menu.Add(MyLogic.MiscMenu);

                MyLogic.DrawMenu = new Menu("FlowersDarius.DrawMenu", ":: Draw Settings");
                {
                    MyManaManager.AddDrawToMenu(MyLogic.DrawMenu);
                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersDarius.DrawMenu.RangeSettings", "-- Spell Range"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersDarius.DrawMenu.Q", "Draw Q Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersDarius.DrawMenu.E", "Draw E Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersDarius.DrawMenu.R", "Draw R Range", false));

                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersDarius.DrawMenu.StatusSettings", "-- Logic Status"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersDarius.DrawMenu.ComboR", "Draw Combo R Status"));
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