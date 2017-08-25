namespace Flowers_Vladimir.MyCommon
{
    #region 

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;

    using System;

    #endregion

    internal class MyManaManager
    {
        internal static bool SpellFarm { get; set; } = true;
        internal static bool SpellHarass { get; set; } = true;

        private static int tick { get; set; } = 0;

        internal static void AddFarmToMenu(Menu mainMenu)
        {
            try
            {
                if (mainMenu != null)
                {
                    mainMenu.Add(new MenuBool("MyManaManager.SpellFarm", "Use Spell To Farm(Mouse Scrool)"));
                    mainMenu.Add(new MenuKeyBind("MyManaManager.SpellHarass", "Use Spell To Harass(In Clear Mode)",
                        Aimtec.SDK.Util.KeyCode.H, KeybindType.Toggle, true));

                    Game.OnWndProc += delegate (WndProcEventArgs Args)
                    {
                        try
                        {
                            if (Args.Message == 0x20a)
                            {
                                mainMenu["MyManaManager.SpellFarm"].As<MenuBool>().Value = !mainMenu["MyManaManager.SpellFarm"].As<MenuBool>().Value;
                                SpellFarm = mainMenu["MyManaManager.SpellFarm"].Enabled;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error in MyManaManager.OnWndProcEvent." + ex);
                        }
                    };

                    Game.OnUpdate += delegate
                    {
                        if (Game.TickCount - tick > 20 * Game.Ping)
                        {
                            tick = Game.TickCount;
                            SpellFarm = mainMenu["MyManaManager.SpellFarm"].Enabled;
                            SpellHarass = mainMenu["MyManaManager.SpellHarass"].Enabled;
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyManaManager.AddFarmToMenu." + ex);
            }
        }

        internal static void AddDrawToMenu(Menu mainMenu)
        {
            try
            {
                if (mainMenu != null)
                {
                    mainMenu.Add(new MenuSeperator("MyManaManager.DrawFarm", "-- Logic Status"));
                    mainMenu.Add(new MenuBool("MyManaManager.DrawSpelFarm", "Draw Spell Farm Status"));
                    mainMenu.Add(new MenuBool("MyManaManager.DrawSpellHarass", "Draw Spell Harass Status"));

                    Render.OnRender += delegate
                    {
                        try
                        {
                            if (ObjectManager.GetLocalPlayer().IsDead)
                            {
                                return;
                            }

                            if (mainMenu["MyManaManager.DrawSpelFarm"].Enabled)
                            {
                                Vector2 MePos = Vector2.Zero;
                                Render.WorldToScreen(ObjectManager.GetLocalPlayer().Position, out MePos);

                                Render.Text(MePos.X - 57, MePos.Y + 48, System.Drawing.Color.FromArgb(242, 120, 34),
                                    "Spell Farms:" + (SpellFarm ? "On" : "Off"));
                            }

                            if (mainMenu["MyManaManager.DrawSpellHarass"].Enabled)
                            {
                                Vector2 MePos = Vector2.Zero;
                                Render.WorldToScreen(ObjectManager.GetLocalPlayer().Position, out MePos);

                                Render.Text(MePos.X - 57, MePos.Y + 68, System.Drawing.Color.FromArgb(242, 120, 34),
                                    "Spell Harass:" + (SpellHarass? "On" : "Off"));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error in MyManaManager.OnRender." + ex);
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in MyManaManager.AddDrawToMenu." + ex);
            }
        }
    }
}