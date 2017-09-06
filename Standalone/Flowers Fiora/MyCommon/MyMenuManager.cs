namespace Flowers_Fiora.MyCommon
{
    #region 

    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Fiora.MyBase;

    using System;

    #endregion

    internal class MyMenuManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Menu = new Menu("FlowersFiora", "Flowers Fiora", true);
                {
                    MyLogic.Menu.Add(new MenuSeperator("MadebyNightMoon", "Made by NightMoon"));
                    MyLogic.Menu.Add(new MenuSeperator("CreditName", "Cirdit: Brian & Esk0r"));
                    MyLogic.Menu.Add(new MenuSeperator("willbeRemove", "This Assembly will be Remove"));
                    MyLogic.Menu.Add(new MenuSeperator("inthisassembly", "Use SharpAIO (new Version)"));
                    MyLogic.Menu.Add(new MenuSeperator("ASDASDF"));
                }

                MyLogic.Orbwalker = new Aimtec.SDK.Orbwalking.Orbwalker();
                MyLogic.Orbwalker.Attach(MyLogic.Menu);

                MyLogic.ComboMenu =new Menu("FlowersFiora.ComboMenu", ":: Combo Settings");
                {
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersFiora.ComboMenu.Q", "Use Q"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersFiora.ComboMenu.E", "Use E"));
                    MyLogic.ComboMenu.Add(new MenuKeyBind("FlowersFiora.ComboMenu.R", "Use R", Aimtec.SDK.Util.KeyCode.R,
                        KeybindType.Toggle, true));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersFiora.ComboMenu.RSolo", "Use R| Solo KillAble"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersFiora.ComboMenu.RTeam", "Use R| Team Fight"));
                    foreach (var hero in GameObjects.EnemyHeroes)
                    {
                        MyLogic.ComboMenu.Add(new MenuBool("FlowersFiora.ComboMenu.RTargetFor" + hero.ChampionName,
                            "Use On " + hero.ChampionName));
                    }
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersFiora.ComboMenu.Item", "Use Item"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersFiora.ComboMenu.Ignite", "Use Ignite"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersFiora.ComboMenu.Force", "Forcus Attack Passive Position"));
                }
                MyLogic.Menu.Add(MyLogic.ComboMenu);

                MyLogic.HarassMenu = new Menu("FlowersFiora.HarassMenu", ":: Harass Settings");
                {
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersFiora.HarassMenu.Q", "Use Q"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersFiora.HarassMenu.E", "Use E", false));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersFiora.HarassMenu.Item", "Use Item"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersFiora.HarassMenu.Turret", "Allow Under Turret Harass", false));
                    MyLogic.HarassMenu.Add(new MenuSlider("FlowersFiora.HarassMenu.HarassManaPercent", 
                        "When Player ManaPercent >= x%", 60, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.HarassMenu);

                MyLogic.ClearMenu = new Menu("FlowersFiora.ClearMenu", ":: Clear Settings");
                {
                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersFiora.ClearMenu.LaneClearSettings", "-- LaneClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersFiora.ClearMenu.LaneClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersFiora.ClearMenu.LaneClearQLH", "Use Q| Only LastHit"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersFiora.ClearMenu.LaneClearE", "Use E"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersFiora.ClearMenu.LaneClearItem", "Use Item"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersFiora.ClearMenu.LaneClearTurret", "Allow Under Turret Farm", false));
                    MyLogic.ClearMenu.Add(new MenuSlider("FlowersFiora.ClearMenu.LaneClearManaPercent",
                        "When Player ManaPercent >= x%", 60, 1, 99));

                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersFiora.ClearMenu.JungleClearSettings", "-- JungleClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersFiora.ClearMenu.JungleClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersFiora.ClearMenu.JungleClearE", "Use E"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersFiora.ClearMenu.JungleClearItem", "Use Item"));
                    MyLogic.ClearMenu.Add(new MenuSlider("FlowersFiora.ClearMenu.JungleClearManaPercent",
                        "When Player ManaPercent >= x%", 20, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.ClearMenu);

                MyLogic.FleeMenu = new Menu("FlowersFiora.FleeMenu", ":: Flee Settings");
                {
                    MyLogic.FleeMenu.Add(new MenuKeyBind("FlowersFiora.FleeMenu.FleeKey", "Flee Key",
                        Aimtec.SDK.Util.KeyCode.Z, KeybindType.Press));
                    MyLogic.FleeMenu.Add(new MenuBool("FlowersFiora.FleeMenu.Q", "Use Q"));
                }
                MyLogic.Menu.Add(MyLogic.FleeMenu);

                MyLogic.KillStealMenu = new Menu("FlowersFiora.KillStealMenu", ":: KillSteal Settings");
                {
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersFiora.KillStealMenu.Q", "Use Q"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersFiora.KillStealMenu.W", "Use W"));
                }
                MyLogic.Menu.Add(MyLogic.KillStealMenu);

                MyLogic.MiscMenu = new Menu("FlowersFiora.MiscMenu", ":: Misc Settings");
                {
                    MyManaManager.AddFarmToMenu(MyLogic.MiscMenu);

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersFiora.MiscMenu.QSettings", "-- Q Settings"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersFiora.MiscMenu.CheckSafe", "Check Safe"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersFiora.MiscMenu.UnderTurret", "Allow Under Turret Cast", false));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersFiora.MiscMenu.ComboUnderTurret", "Combo Mode Ignore Turret Check"));

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersFiora.MiscMenu.ForceSettings", "-- Force Settings"));
                    MyLogic.MiscMenu.Add(new MenuSlider("FlowersFiora.MiscMenu.ForceResetTime", "Reset Time", 450, 100, 1500));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersFiora.MiscMenu.ForceResetTimePing", "Include Game Ping"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersFiora.MiscMenu.ForceResetTimeMoveSpeed", "Include MoveMent Speed", false));
                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersFiora.MiscMenu.Seperator1",
                        "This is Reset Your Orbwalker Point"));
                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersFiora.MiscMenu.Seperator2",
                        "When you Force Attack Passive"));
                }
                MyLogic.Menu.Add(MyLogic.MiscMenu);

                MyLogic.EvadeMenu = new Menu("FlowersFiora.EvadeMenu", ":: Evade Settings");
                {
                    MyEvade.EvadeManager.Attach(MyLogic.EvadeMenu);
                    MyEvade.EvadeOthers.Attach(MyLogic.EvadeMenu);
                    MyEvade.EvadeTargetManager.Attach(MyLogic.EvadeMenu);
                }
                MyLogic.Menu.Add(MyLogic.EvadeMenu);

                MyLogic.DrawMenu = new Menu("FlowersFiora.DrawMenu", ":: Draw Settings");
                {
                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersFiora.DrawMenu.RangeSettings", "-- Spell Range"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersFiora.DrawMenu.Q", "Draw Q Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersFiora.DrawMenu.W", "Draw W Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersFiora.DrawMenu.R", "Draw R Range", false));

                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersFiora.DrawMenu.StatusSettings", "-- Logic Status"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersFiora.DrawMenu.ComboR", "Draw Combo R Status"));

                    MyManaManager.AddDrawToMenu(MyLogic.DrawMenu);
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