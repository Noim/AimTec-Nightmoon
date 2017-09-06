namespace Flowers_Kalista.MyCommon
{
    #region 

    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Kalista.MyBase;

    using System;

    #endregion

    internal class MyMenuManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Menu = new Menu("FlowersKalista", "Flowers Kalista", true);
                {
                    MyLogic.Menu.Add(new MenuSeperator("CreditName", "Made by NightMoon"));
                    MyLogic.Menu.Add(new MenuSeperator("willbeRemove", "This Assembly will be Remove"));
                    MyLogic.Menu.Add(new MenuSeperator("inthisassembly", "Use SharpShooter (new Version)"));
                    MyLogic.Menu.Add(new MenuSeperator("ASDASDF"));
                }

                MyLogic.Orbwalker = new Aimtec.SDK.Orbwalking.Orbwalker();
                MyLogic.Orbwalker.Attach(MyLogic.Menu);

                MyLogic.ComboMenu = new Menu("FlowersKalista.ComboMenu", ":: Combo Settings");
                {
                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersKalista.ComboMenu.QSettings", "-- Q Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKalista.ComboMenu.Q", "Use Q"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersKalista.ComboMenu.ESettings", "-- E Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKalista.ComboMenu.E", "Use E"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKalista.ComboMenu.ESlow", "Use E| When Enemy Have Buff and Minion Can KillAble"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersKalista.ComboMenu.GapcloserSettings", "-- Gapcloser Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersKalista.ComboMenu.Gapcloser", "Auto Attack Minion To Gapcloser Target"));
                }
                MyLogic.Menu.Add(MyLogic.ComboMenu);

                MyLogic.HarassMenu = new Menu("FlowersKalista.HarassMenu", ":: Harass Settings");
                {
                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersKalista.HarassMenu.QSettings", "-- Q Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersKalista.HarassMenu.Q", "Use Q", false));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersKalista.HarassMenu.ESettings", "-- E Settings"));

                    MyLogic.HarassMenu.Add(new MenuBool("FlowersKalista.HarassMenu.ESlow",
                        "Use E| When Enemy Have Buff and Minion Can KillAble"));
                    MyLogic.HarassMenu.Add(new MenuSliderBool("FlowersKalista.HarassMenu.ELeave",
                        "Use E| When Enemy Will Leave E Range And Buff Count >= x", false, 3, 1, 10));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersKalista.HarassMenu.ManaSettings", "-- Mana Settings"));
                    MyLogic.HarassMenu.Add(new MenuSlider("FlowersKalista.HarassMenu.Mana",
                        "When Player ManaPercent >= x%", 60, 1, 99));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersKalista.HarassMenu.TargetSettings", "-- Target Settings"));
                    AddTargetList(MyLogic.HarassMenu, "FlowersKalista.HarassMenu.HarassTarget_");
                }
                MyLogic.Menu.Add(MyLogic.HarassMenu);

                MyLogic.ClearMenu = new Menu("FlowersKalista.ClearMenu", ":: Clear Settings");
                {
                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersKalista.ClearMenu.LaneClearSettings", "-- LaneClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuSliderBool("FlowersKalista.ClearMenu.LaneClearE",
                        "Use E| Min KillAble Count >= x", true, 3, 1, 5));
                    MyLogic.ClearMenu.Add(new MenuSlider("FlowersKalista.ClearMenu.LaneClearMana",
                        "When Player ManaPercent >= x%", 30, 1, 99));

                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersKalista.ClearMenu.JungleClearSettings", "-- JungleClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersKalista.ClearMenu.JungleClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersKalista.ClearMenu.JungleClearE", "Use E"));
                    MyLogic.ClearMenu.Add(new MenuSlider("FlowersKalista.ClearMenu.JungleClearMana",
                        "When Player ManaPercent >= x%", 30, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.ClearMenu);

                MyLogic.LastHitMenu = new Menu("FlowersKalista.LastHitMenu", ":: LastHit Settings");
                {
                    MyLogic.LastHitMenu.Add(new MenuSeperator("FlowersKalista.LastHitMenu.ESettings", "-- E Settings"));
                    MyLogic.LastHitMenu.Add(new MenuBool("FlowersKalista.LastHitMenu.E", "Use E"));
                    MyLogic.LastHitMenu.Add(new MenuBool("FlowersKalista.LastHitMenu.Auto", "Auto E To LastHit"));
                    MyLogic.LastHitMenu.Add(new MenuSlider("FlowersKalista.LastHitMenu.Mana",
                        "When Player ManaPercent >= x%", 30, 1, 99));
                }
                MyLogic.Menu.Add(MyLogic.LastHitMenu);

                MyLogic.KillStealMenu = new Menu("FlowersKalista.KillStealMenu", ":: KillSteal Settings");
                {
                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersKalista.KillStealMenu.QSettings", "-- Q Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersKalista.KillStealMenu.Q", "Use Q"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersKalista.KillStealMenu.ESettings", "-- E Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersKalista.KillStealMenu.E", "Use E"));
                }
                MyLogic.Menu.Add(MyLogic.KillStealMenu);

                MyLogic.MiscMenu = new Menu("FlowersKalista.MiscMenu", ":: Misc Settings");
                {
                    MyManaManager.AddFarmToMenu(MyLogic.MiscMenu);

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersKalista.MiscMenu.ESettings", "-- E Settings"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersKalista.MiscMenu.AutoESteal", "Auto E Steal Mob (Only Buff&Dragon&Baron)"));
                    MyLogic.MiscMenu.Add(new MenuSliderBool("FlowersKalista.MiscMenu.EToler", "Enabled E Toler DMG", true, 0, -100, 110));

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersKalista.MiscMenu.RSettings", "-- R Settings"));
                    MyLogic.MiscMenu.Add(new MenuSliderBool("FlowersKalista.MiscMenu.AutoRAlly", "Auto R| My Allies HealthPercent <= x%", true, 30, 1, 99));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersKalista.MiscMenu.Balista", "Auto Balista"));

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersKalista.MiscMenu.ForcusSettings", "-- Forcus Settings"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersKalista.MiscMenu.ForcusAttack", "Forcus Attack Passive Target"));
                }
                MyLogic.Menu.Add(MyLogic.MiscMenu);

                MyLogic.DrawMenu = new Menu("FlowersKalista.DrawMenu", ":: Draw Settings");
                {
                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersKalista.DrawMenu.RangeSettings", "-- Spell Range"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKalista.DrawMenu.Q", "Draw Q Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKalista.DrawMenu.W", "Draw W Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKalista.DrawMenu.E", "Draw E Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersKalista.DrawMenu.R", "Draw R Range", false));

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

        internal static void AddTargetList(Menu mainMenu, string extraName)
        {
            foreach (var target in GameObjects.EnemyHeroes)
            {
                mainMenu.Add(new MenuBool(extraName + target.ChampionName, "Use On: " + target.ChampionName));
            }
        }
    }
}