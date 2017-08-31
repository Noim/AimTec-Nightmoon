namespace Flowers_Vladimir.MyCommon
{
    #region 

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;

    using Flowers_Library;

    using Flowers_Vladimir.MyBase;

    using System;
    using System.Linq;

    #endregion

    internal class MyMenuManager
    {
        internal static void Initializer()
        {
            try
            {
                MyLogic.Menu = new Menu("FlowersVladimir", "Flowers Vladimir", true);
                {
                    MyLogic.Menu.Add(new MenuSeperator("MadebyNightMoon", "Made by NightMoon"));
                    MyLogic.Menu.Add(new MenuSeperator("willbeRemove", "This Assembly will be Remove"));
                    MyLogic.Menu.Add(new MenuSeperator("inthisassembly", "Use SharpAIO (new Version)"));
                    MyLogic.Menu.Add(new MenuSeperator("ASDASDF"));
                }

                MyLogic.Orbwalker = new Aimtec.SDK.Orbwalking.Orbwalker();
                MyLogic.Orbwalker.Attach(MyLogic.Menu);

                MyLogic.ComboMenu = new Menu("FlowersVladimir.ComboMenu", ":: Combo Settings");
                {
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersVladimir.ComboMenu.Q", "Use Q"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersVladimir.ComboMenu.W", "Use W"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersVladimir.ComboMenu.WECharge", "Use W| Only E Charging"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersVladimir.ComboMenu.E", "Use E"));
                    MyLogic.ComboMenu.Add(new MenuKeyBind("FlowersVladimir.ComboMenu.R", "Use R",
                        Aimtec.SDK.Util.KeyCode.T, KeybindType.Toggle, true));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersVladimir.ComboMenu.RAlways", "Use R| Always Cast", false));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersVladimir.ComboMenu.RKillAble", "Use R| KillAble"));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersVladimir.ComboMenu.RBurstCombo", "Use R| Burst Combo"));
                    MyLogic.ComboMenu.Add(new MenuSliderBool("FlowersVladimir.ComboMenu.RCountHit",
                        "Use R| Min Hit Count >= x", true, 3, 1, 5));
                    MyLogic.ComboMenu.Add(new MenuBool("FlowersVladimir.ComboMenu.Ignite", "Use Ignite"));
                }
                MyLogic.Menu.Add(MyLogic.ComboMenu);

                MyLogic.HarassMenu = new Menu("FlowersVladimir.HarassMenu", ":: Harass Settings");
                {
                    MyLogic.HarassMenu.Add(new MenuBool("FlowersVladimir.HarassMenu.Q", "Use Q"));
                    MyLogic.HarassMenu.Add(new MenuSeperator("FlowersVladimir.HarassMenu.HarassList", "Harass Target List"));
                    foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy))
                    {
                        MyLogic.HarassMenu.Add(new MenuBool("FlowersVladimir.HarassMenu.Target_" + target.ChampionName,
                            target.ChampionName));
                    }
                }
                MyLogic.Menu.Add(MyLogic.HarassMenu);

                MyLogic.ClearMenu = new Menu("FlowersVladimir.ClearMenu", ":: Clear Settings");
                {
                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersVladimir.ClearMenu.LaneClearSettings", "-- LaneClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersVladimir.ClearMenu.LaneClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersVladimir.ClearMenu.LaneClearQFrenzy", "Use Q| Frenzy"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersVladimir.ClearMenu.LaneClearQLH", "Use Q| Only LastHit"));
                    MyLogic.ClearMenu.Add(new MenuSliderBool("FlowersVladimir.ClearMenu.LaneClearE",
                        "Use E| Min Hit Count >= x", false, 3, 1, 5));

                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersVladimir.ClearMenu.JungleClearSettings", "-- JungleClear Settings"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersVladimir.ClearMenu.JungleClearQ", "Use Q"));
                    MyLogic.ClearMenu.Add(new MenuBool("FlowersVladimir.ClearMenu.JungleClearE", "Use E"));

                    MyLogic.ClearMenu.Add(new MenuSeperator("FlowersVladimir.ClearMenu.FarmSettings", "-- Farm Settings"));
                    MyManaManager.AddFarmToMenu(MyLogic.ClearMenu);
                }
                MyLogic.Menu.Add(MyLogic.ClearMenu);

                MyLogic.LastHitMenu = new Menu("FlowersVladimir.LastHitMenu", ":: LastHit Settings");
                {
                    MyLogic.LastHitMenu.Add(new MenuBool("FlowersVladimir.LastHitMenu.Q", "Use Q"));
                    MyLogic.LastHitMenu.Add(new MenuBool("FlowersVladimir.LastHitMenu.QFrenzy", "Use Q| Frenzy"));
                }
                MyLogic.Menu.Add(MyLogic.LastHitMenu);

                MyLogic.KillStealMenu = new Menu("FlowersVladimir.KillStealMenu", ":: KillSteal Settings");
                {
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersVladimir.KillStealMenu.Q", "Use Q"));
                    MyLogic.KillStealMenu.Add(new MenuBool("FlowersVladimir.KillStealMenu.E", "Use E"));
                }
                MyLogic.Menu.Add(MyLogic.KillStealMenu);

                Gapcloser.MinSearchRange = 300;
                Gapcloser.MaxSearchRange = 500;
                Gapcloser.DefalutHPercent = 40;
                Gapcloser.Attach(MyLogic.Menu, ":: Gapcloser Settings");

                MyLogic.EvadeMenu = new Menu("FlowersVladimir.EvadeMenu", ":: Evade Settings");
                {
                    MyEvadeManager.Attach(MyLogic.EvadeMenu);
                }
                MyLogic.Menu.Add(MyLogic.EvadeMenu);

                MyLogic.DrawMenu = new Menu("FlowersVladimir.DrawMenu", ":: Draw Settings");
                {
                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersVladimir.DrawMenu.RangeSettings", "-- Spell Range"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersVladimir.DrawMenu.Q", "Draw Q Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersVladimir.DrawMenu.E", "Draw E Range", false));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersVladimir.DrawMenu.R", "Draw R Range", false));
                    MyManaManager.AddDrawToMenu(MyLogic.DrawMenu);
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersVladimir.DrawMenu.ComboR", "Draw Combo R Status"));
                    MyLogic.DrawMenu.Add(new MenuSeperator("FlowersVladimir.DrawMenu.DamageSettings", "-- Damage Indicator"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersVladimir.DrawMenu.ComboDamage", "Draw Combo Damage"));
                    MyLogic.DrawMenu.Add(new MenuBool("FlowersVladimir.DrawMenu.FillDamage", "Draw Fill Damage"));
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