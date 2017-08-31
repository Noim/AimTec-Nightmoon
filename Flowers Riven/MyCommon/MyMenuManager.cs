namespace Flowers_Riven.MyCommon
{
    #region 

    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Riven.MyBase;

    using System;

    #endregion

    internal class MyMenuManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Menu = new Menu("FlowersRiven", "Flowers Riven", true);
                {
                    MyLogic.Menu.Add(new MenuSeperator("CreditName", "Made by NightMoon"));
                    MyLogic.Menu.Add(new MenuSeperator("willbeRemove", "This Assembly will be Remove"));
                    MyLogic.Menu.Add(new MenuSeperator("inthisassembly", "Use SharpAIO (new Version)"));
                    MyLogic.Menu.Add(new MenuSeperator("ASDASDF"));
                }

                MyLogic.Orbwalker = new Aimtec.SDK.Orbwalking.Orbwalker();
                MyLogic.Orbwalker.Attach(MyLogic.Menu);

                MyLogic.ComboMenu =new Menu("FlowersRiven.ComboMenu", ":: Combo Settings");
                {
                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRiven.ComboMenu.QSettings", "-- Q Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRiven.ComboMenu.QGapcloser", "Use Q Gapcloser"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRiven.ComboMenu.WSettings", "-- W Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRiven.ComboMenu.WCancel", "Use W Cancel Animation"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRiven.ComboMenu.ESettings", "-- E Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRiven.ComboMenu.EGapcloser", "Use E Gapcloser"));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRiven.ComboMenu.RSettings", "-- R Settings"));
                    MyLogic.ComboMenu.Add(new MenuKeyBind("FlowersRiven.ComboMenu.R", "Use R", Aimtec.SDK.Util.KeyCode.G,
                        KeybindType.Toggle, true));
                    MyLogic.ComboMenu.Add(new MenuList("FlowersRiven.ComboMenu.RMode", "Use R2 Mode: ",
                        new[] {"My Logic", "Only KillSteal", "First Cast", "Off"}, 0));

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRiven.ComboMenu.RTargetSettings", "-- RTarget Settings"));
                    foreach (var hero in GameObjects.EnemyHeroes)
                    {
                        MyLogic.ComboMenu.Add(new MenuBool("FlowersRiven.ComboMenu.RTargetFor" + hero.ChampionName,
                            "Use On " + hero.ChampionName));
                    }

                    MyLogic.ComboMenu.Add(new MenuSeperator("FlowersRiven.ComboMenu.OtherSettings", "-- Other Settings"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRiven.ComboMenu.Ignite", "Use Ignite"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRiven.ComboMenu.Item", "Use Item"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersRiven.ComboMenu.Youmuu", "Use Youmuu"));
                }
                MyLogic.Menu.Add(MyLogic.ComboMenu);

                MyLogic.BurstMenu = new Menu("FlowersRiven.BurstMenu", ":: Burst Settings");
                {
                    MyLogic.BurstMenu.Add(new MenuSeperator("FlowersRiven.BurstMenu.Text1", "-- How to burst"));
                    MyLogic.BurstMenu.Add(new MenuSeperator("FlowersRiven.BurstMenu.Text2", "1.you need to enbaled the Key"));
                    MyLogic.BurstMenu.Add(new MenuSeperator("FlowersRiven.BurstMenu.Text3", "2.Select the Target (or not, but this will be force target to burst)"));
                    MyLogic.BurstMenu.Add(new MenuSeperator("FlowersRiven.BurstMenu.Text4", "3.and then press the Combo Key"));
                    MyLogic.BurstMenu.Add(new MenuSeperator("BUSRAWDQWD"));
                    MyLogic.BurstMenu.Add(new MenuBool("FlowersRiven.BurstMenu.Flash", "Use Flash"));
                    MyLogic.BurstMenu.Add(new MenuBool("FlowersRiven.BurstMenu.Ignite", "Use Ignite"));
                    MyLogic.BurstMenu.Add(new MenuList("FlowersRiven.BurstMenu.Mode", "Burst Mode: ",
                        new[] {"Shy", "EQ3"}, 0));
                    MyLogic.BurstMenu.Add(new MenuKeyBind("FlowersRiven.BurstMenu.Key", "Burst Active Key(Toggle)",
                        Aimtec.SDK.Util.KeyCode.T, KeybindType.Toggle));
                }
                MyLogic.Menu.Add(MyLogic.BurstMenu);

                MyLogic.HarassMenu = new Menu("FlowersRiven.HarassMenu", ":: Harass Settings");
                {
                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersRiven.HarassMenu.QSettings", "-- Q Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersRiven.HarassMenu.Q", "Use Q"));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersRiven.HarassMenu.WSettings", "-- W Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersRiven.HarassMenu.W", "Use W"));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersRiven.HarassMenu.ESettings", "-- E Settings"));
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersRiven.HarassMenu.E", "Use E"));

                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersRiven.HarassMenu.ModeSettings", "-- Mode Settings"));
                    MyLogic.HarassMenu.Add(new MenuList("FlowersRiven.HarassMenu.Mode", "Harass Mode: ",
                        new[] {"Smart(will use Q/E return)", "normal"}, 0));
                }
                MyLogic.Menu.Add(MyLogic.HarassMenu);

                MyLogic.ClearMenu = new Menu("FlowersRiven.ClearMenu", ":: Clear Settings");
                {
                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersRiven.ClearMenu.LaneClearSettings", "-- LaneClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRiven.ClearMenu.LaneClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRiven.ClearMenu.LaneClearQSmart", "Use Q| Smart Farm"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRiven.ClearMenu.LaneClearQTurret", "Use Q| When Attach Turret reset"));
                    MyLogic.ClearMenu.Add(new MenuSliderBool("FlowersRiven.ClearMenu.LaneClearW",
                        "Use W| Min Hit Count >= x", true, 3, 1, 5));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRiven.ClearMenu.LaneClearItem", "Use Item"));

                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersRiven.ClearMenu.JungleClearSettings", "-- JungleClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRiven.ClearMenu.JungleClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRiven.ClearMenu.JungleClearW", "Use W"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRiven.ClearMenu.JungleClearE", "Use E"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersRiven.ClearMenu.JungleClearItem", "Use Item"));
                }
                MyLogic.Menu.Add(MyLogic.ClearMenu);

                MyLogic.FleeMenu = new Menu("FlowersRiven.FleeMenu", ":: Flee Settings");
                {
                    MyLogic.FleeMenu.Add(new MenuSeperator("FlowersRiven.FleeMenu.KeySettings", "-- Key Settings"));
                    MyLogic.FleeMenu.Add(new MenuKeyBind("FlowersRiven.FleeMenu.FleeKey", "Flee Key",
                        Aimtec.SDK.Util.KeyCode.Z, KeybindType.Press));
                    MyLogic.FleeMenu.Add(new MenuKeyBind("FlowersRiven.FleeMenu.WallJumpKey", "Wall Jump Key",
                        Aimtec.SDK.Util.KeyCode.A, KeybindType.Press));

                    MyLogic.FleeMenu.Add(new MenuSeperator("FlowersRiven.FleeMenu.QSettings", "-- Q Settings"));
                    MyLogic.FleeMenu.Add(new MenuBool("FlowersRiven.FleeMenu.Q", "Use Q"));

                    MyLogic.FleeMenu.Add(new MenuSeperator("FlowersRiven.FleeMenu.WSettings", "-- W Settings"));
                    MyLogic.FleeMenu.Add(new MenuBool("FlowersRiven.FleeMenu.W", "Use W"));

                    MyLogic.FleeMenu.Add(new MenuSeperator("FlowersRiven.FleeMenu.ESettings", "-- E Settings"));
                    MyLogic.FleeMenu.Add(new MenuBool("FlowersRiven.FleeMenu.E", "Use E"));
                }
                MyLogic.Menu.Add(MyLogic.FleeMenu);

                MyLogic.KillStealMenu = new Menu("FlowersRiven.KillStealMenu", ":: KillSteal Settings");
                {
                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersRiven.KillStealMenu.RSettings", "-- R Settings"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersRiven.KillStealMenu.R", "Use R"));

                    MyLogic.KillStealMenu.Add(new MenuSeperator("FlowersRiven.KillStealMenu.RTargetSettings", "-- RTarget Settings"));
                    foreach (var hero in GameObjects.EnemyHeroes)
                    {
                        MyLogic.KillStealMenu.Add(new MenuBool("FlowersRiven.KillStealMenu.RTargetFor" + hero.ChampionName,
                            "Use On " + hero.ChampionName));
                    }
                }
                MyLogic.Menu.Add(MyLogic.KillStealMenu);

                MyLogic.MiscMenu = new Menu("FlowersRiven.MiscMenu", ":: Misc Settings");
                {
                    MyManaManager.AddFarmToMenu(MyLogic.MiscMenu);

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersRiven.MiscMenu.QSettings", "-- Q Settings"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersRiven.MiscMenu.KeepQ", "Keep Q Alive"));
                    MyLogic.MiscMenu.Add(new MenuList("FlowersRiven.MiscMenu.QMode", "Use Q Mode: ",
                        new[] {"To target", "To Mouse"}, 0));

                    MyLogic.MiscMenu.Add(new MenuSeperator("FlowersRiven.MiscMenu.AnimationSettings", "-- Animation Settings"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersRiven.MiscMenu.SemiCancel", "Semi Cancel Animation"));
                    MyLogic.MiscMenu.Add(new MenuBool("FlowersRiven.MiscMenu.CalculatePing", "Calulate your Ping"));
                    //MyLogic.MiscMenu.Add(new MenuSlider("FlowersRiven.MiscMenu.Q1Delay", "Q1 Cancel Delay", 251, 0, 500));
                    //MyLogic.MiscMenu.Add(new MenuSlider("FlowersRiven.MiscMenu.Q2Delay", "Q2 Cancel Delay", 251, 0, 500));
                    //MyLogic.MiscMenu.Add(new MenuSlider("FlowersRiven.MiscMenu.Q3Delay", "Q3 Cancel Delay", 351, 0, 600));
                    //MyLogic.MiscMenu.Add(new MenuSeperator("QDelay111", "Default: Q1Q2 = 351, Q3 = 451"));
                    //MyLogic.MiscMenu.Add(new MenuSeperator("QDelay222", "Normal: Q1Q2Q3 = 0"));
                    //MyLogic.MiscMenu.Add(new MenuSeperator("QDelay333", "Speed: Q1Q2 = 251, Q3 = 351(feedback)"));
                    //MyLogic.MiscMenu.Add(new MenuSeperator("QDelay444", "Im Not Sure this is perfect Delay"));
                }
                MyLogic.Menu.Add(MyLogic.MiscMenu);

                MyLogic.EvadeMenu = new Menu("FlowersRiven.EvadeMenu", ":: Evade Settings");
                {
                    MyLogic.EvadeMenu.Add(new MenuBool("FlowersRiven.EvadeMenu.Use E", "Use E Shield"));
                }
                MyLogic.Menu.Add(MyLogic.EvadeMenu);

                MyLogic.DrawMenu = new Menu("FlowersRiven.DrawMenu", ":: Draw Settings");
                {
                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersRiven.DrawMenu.RangeSettings", "-- Spell Range"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRiven.DrawMenu.W", "Draw W Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRiven.DrawMenu.E", "Draw E Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRiven.DrawMenu.R", "Draw R Range", false));

                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersRiven.DrawMenu.StatusSettings", "-- Logic Status"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRiven.DrawMenu.ComboR", "Draw Combo R Status"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersRiven.DrawMenu.Burst", "Draw Burst Status"));

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