namespace Flowers_Katarina.MyCommon
{
    #region 

    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Katarina.MyBase;

    using System;

    #endregion

    internal class MyMenuManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Menu = new Menu("FlowersKatarina", "Flowers Katarina", true);
                {
                    MyLogic.Menu.Add(new MenuSeperator("MadebyNightMoon", "Made by NightMoon"));
                    MyLogic.Menu.Add(new MenuSeperator("CreditName", "Credit: badao"));
                    MyLogic.Menu.Add(new MenuSeperator("willbeRemove", "This Assembly will be Remove"));
                    MyLogic.Menu.Add(new MenuSeperator("inthisassembly", "Use SharpAIO (new Version)"));
                    MyLogic.Menu.Add(new MenuSeperator("ASDASDF"));
                }

                MyLogic.Orbwalker = new Aimtec.SDK.Orbwalking.Orbwalker();
                MyLogic.Orbwalker.Attach(MyLogic.Menu);

                MyLogic.ComboMenu = new Menu("FlowersKatarina.ComboMenu", ":: Combo Settings");
                {
                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersKatarina.ComboMenu.QSettings", "-- Q Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.Q", "Use Q"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.QOnMinion", "Use Q| On Minion to Gapcloser", false));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersKatarina.ComboMenu.WSettings", "-- W Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.W", "Use W"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.WSmart", "Use W| Smart Gapcloser"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersKatarina.ComboMenu.ESettings", "-- E Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.E", "Use E"));
                    MyLogic.ComboMenu.Add(new MenuKeyBind("FlowersKatarina.ComboMenu.EKillAble", "Use E| Only KillAble",
                        Aimtec.SDK.Util.KeyCode.G, KeybindType.Toggle));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersKatarina.ComboMenu.RSettings", "-- R Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.R", "Use R"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.RAlways", "Use R| Always Cast", false));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.RKillAble", "Use R| KillAble"));
                    MyLogic.ComboMenu.Add(new MenuSliderBool("FlowersKatarina.ComboMenu.RCountHit", "Use R| Min Hit Count >= x", true, 3, 1, 5));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersKatarina.ComboMenu.ModeSettings", "-- Other Settings"));
                    MyLogic.ComboMenu.Add(new MenuList("FlowersKatarina.ComboMenu.Mode", "Combo Mode: ", new[] {"QE", "EQ"}, 0));
                    MyLogic.ComboMenu.Add(new MenuKeyBind("FlowersKatarina.ComboMenu.SwitchMode",
                        "Switch Combo Mode Key", Aimtec.SDK.Util.KeyCode.H, KeybindType.Press))
                        .OnValueChanged +=
                        delegate (MenuComponent iMenuComponent, ValueChangedArgs Args)
                        {
                            if (Args.GetNewValue<MenuKeyBind>().Enabled)
                            {
                                switch (MyLogic.ComboMenu["FlowersKatarina.ComboMenu.Mode"].As<MenuList>().Value)
                                {
                                    case 0:
                                        MyLogic.ComboMenu["FlowersKatarina.ComboMenu.Mode"].As<MenuList>().Value = 1;
                                        break;
                                    case 1:
                                        MyLogic.ComboMenu["FlowersKatarina.ComboMenu.Mode"].As<MenuList>().Value = 0;
                                        break;
                                }
                            }
                        };
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKatarina.ComboMenu.Ignite", "Use Ignite"));
                }
                MyLogic.Menu.Add(MyLogic.ComboMenu);

                MyLogic.HarassMenu = new Menu("FlowersKatarina.HarassMenu", ":: Harass Settings");
                {
                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersKatarina.HarassMenu.QSettings", "-- Q Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersKatarina.HarassMenu.Q", "Use Q"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersKatarina.HarassMenu.QOnMinion", "Use Q| On Minion to Gapcloser", false));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersKatarina.HarassMenu.WSettings", "-- W Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersKatarina.HarassMenu.W", "Use W", false));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersKatarina.HarassMenu.ESettings", "-- E Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersKatarina.HarassMenu.E", "Use E", false));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersKatarina.HarassMenu.ModeSettings", "-- Mode Settings"));
                    MyLogic.HarassMenu.Add(new MenuList("FlowersKatarina.HarassMenu.Mode", "Harass Mode: ", new[] { "QE", "EQ" }, 0));
                }
                MyLogic.Menu.Add(MyLogic.HarassMenu);

                MyLogic.ClearMenu = new Menu("FlowersKatarina.ClearMenu", ":: Clear Settings");
                {
                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersKatarina.ClearMenu.LaneClearSettings", "-- LaneClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersKatarina.ClearMenu.LaneClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersKatarina.ClearMenu.LaneClearQOnlyLH", "Use Q| Only LastHit"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersKatarina.ClearMenu.LaneClearW", "Use W"));

                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersKatarina.ClearMenu.JungleClearSettings", "-- JungleClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersKatarina.ClearMenu.JungleClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersKatarina.ClearMenu.JungleClearW", "Use W"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersKatarina.ClearMenu.JungleClearE", "Use E"));
                }
                MyLogic.Menu.Add(MyLogic.ClearMenu);

                MyLogic.LastHitMenu = new Menu("FlowersKatarina.LastHitMenu", ":: LastHit Settings");
                {
                    MyLogic.LastHitMenu.Add(new MenuSeperator("FlowersKatarina.LastHitMenu.QSettings", "-- Q Settings"));
                    MyLogic.LastHitMenu.Add(new MenuBool("FlowersKatarina.LastHitMenu.Q", "Use Q"));
                }
                MyLogic.Menu.Add(MyLogic.LastHitMenu);

                MyLogic.FleeMenu = new Menu("FlowersKatarina.FleeMenu", ":: Flee Settings");
                {
                    MyLogic.FleeMenu.Add(new MenuSeperator("FlowersKatarina.FleeMenu.KeySettings", "-- Key Settings"));
                    MyLogic.FleeMenu.Add(new MenuKeyBind("FlowersKatarina.FleeMenu.Key", "Flee Active Key",
                        Aimtec.SDK.Util.KeyCode.Z, KeybindType.Press));

                    MyLogic.FleeMenu.Add(new MenuSeperator("FlowersKatarina.FleeMenu.WSettings", "-- W Settings"));
                    MyLogic.FleeMenu.Add(new MenuBool("FlowersKatarina.FleeMenu.W", "Use W"));

                    MyLogic.FleeMenu.Add(new MenuSeperator("FlowersKatarina.FleeMenu.ESettings", "-- E Settings"));
                    MyLogic.FleeMenu.Add(new MenuBool("FlowersKatarina.FleeMenu.E", "Use E"));
                }
                MyLogic.Menu.Add(MyLogic.FleeMenu);

                MyLogic.KillStealMenu = new Menu("FlowersKatarina.KillStealMenu", ":: KillSteal Settings");
                {
                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersKatarina.KillStealMenu.QSettings", "-- Q Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersKatarina.KillStealMenu.Q", "Use Q"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersKatarina.KillStealMenu.ESettings", "-- E Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersKatarina.KillStealMenu.E", "Use E"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersKatarina.KillStealMenu.RSettings", "-- R Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersKatarina.KillStealMenu.R", "Use R"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersKatarina.KillStealMenu.OtherSettings", "-- Other Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersKatarina.KillStealMenu.CancelR", "Auto Cancel R to KS"));
                }
                MyLogic.Menu.Add(MyLogic.KillStealMenu);

                MyLogic.MiscMenu = new Menu("FlowersKatarina.MiscMenu", ":: Misc Settings");
                {
                    MyManaManager.AddFarmToMenu(MyLogic.MiscMenu);
                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersKatarina.MiscMenu.ESettings", "-- E Settings"));
                    MyLogic.MiscMenu.Add(new MenuSliderBool("FlowersKatarina.MiscMenu.EHumanizer",
                        "Enabled Humanizer| Delay <= x(ms)", false, 0, 0, 1500));
                    MyLogic.MiscMenu.Add(new MenuList("FlowersKatarina.MiscMenu.ETurret", "Disable E to Enemy Turret",
                        new[] {"Always", "Smart", "Off"}, 1));
                    MyLogic.MiscMenu.Add(new MenuSlider("FlowersKatarina.MiscMenu.ETurretHP",
                        "Smart Mode: When Player HealthPercent <= x%", 50, 1, 99));

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersKatarina.MiscMenu.RSettings", "-- R Settings"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersKatarina.MiscMenu.AutoCancelR", "Auto Cancel Ult"));

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersKatarina.MiscMenu.OtherSettings", "-- Other Settings"));
                    MyLogic.MiscMenu.Add(new MenuKeyBind("FlowersKatarina.MiscMenu.OneKeyEW", "Semi EW Key",
                        Aimtec.SDK.Util.KeyCode.A, KeybindType.Press));
                }
                MyLogic.Menu.Add(MyLogic.MiscMenu);

                MyLogic.DrawMenu = new Menu("FlowersKatarina.DrawMenu", ":: Draw Settings");
                {
                    MyManaManager.AddDrawToMenu(MyLogic.DrawMenu);
                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersKatarina.DrawMenu.RangeSettings", "-- Spell Range"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKatarina.DrawMenu.Q", "Draw Q Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKatarina.DrawMenu.E", "Draw E Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKatarina.DrawMenu.R", "Draw R Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKatarina.DrawMenu.Dagger", "Draw Dagger Range", false));

                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersKatarina.DrawMenu.StatusSettings", "-- Logic Status"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKatarina.DrawMenu.ComboE", "Draw Combo E Status"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKatarina.DrawMenu.ComboMode", "Draw Combo Mode"));
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