namespace SharpShooter.MyCommon
{
    #region 

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;

    #endregion

    internal class MyManaManager
    {
        internal static bool SpellFarm { get; set; } = true;
        internal static bool SpellHarass { get; set; } = true;

        private static bool FarmScrool { get; set; } = true;
        private static bool HarassScrool { get; set; } = true;

        internal static void AddFarmToMenu(Menu mainMenu)
        {
            if (mainMenu != null)
            {
                var farmMenu = new Menu("MyManaManager.SpellFarmSettings", "Spell Settings")
                {
                    new MenuList("MyManaManager.SpellFarmMode", "Enabled Spell Farm Control: ",
                        new[] {"Mouse scrool", "Key Toggle", "Off"}, 0),
                    new MenuKeyBind("MyManaManager.SpellFarmKey", "Spell Farm Key", Aimtec.SDK.Util.KeyCode.J,
                        KeybindType.Toggle, true),
                    new MenuList("MyManaManager.SpellHarassMode", "Enabled Spell Harass Control: ",
                        new[] {"Mouse scrool", "Key Toggle", "Off"}, 1),
                    new MenuKeyBind("MyManaManager.SpellHarassKey", "Spell Harass Key", Aimtec.SDK.Util.KeyCode.H,
                        KeybindType.Toggle, true)
                };
                mainMenu.Add(farmMenu);

                Game.OnWndProc += delegate (WndProcEventArgs Args)
                {
                    if (Args.Message == 0x20a)
                    {
                        if (farmMenu["MyManaManager.SpellFarmMode"].As<MenuList>().Value == 0)
                        {
                            FarmScrool = !FarmScrool;
                        }

                        if (farmMenu["MyManaManager.SpellHarassMode"].As<MenuList>().Value == 0)
                        {
                            HarassScrool = !HarassScrool;
                        }
                    }
                };

                Game.OnUpdate += delegate
                {
                    SpellFarm = farmMenu["MyManaManager.SpellFarmMode"].As<MenuList>().Value == 0 && FarmScrool ||
                                farmMenu["MyManaManager.SpellFarmMode"].As<MenuList>().Value == 1 &&
                                farmMenu["MyManaManager.SpellFarmKey"].As<MenuKeyBind>().Enabled ||
                                farmMenu["MyManaManager.SpellFarmMode"].As<MenuList>().Value == 2;
                    SpellHarass = farmMenu["MyManaManager.SpellHarassMode"].As<MenuList>().Value == 0 && FarmScrool ||
                                farmMenu["MyManaManager.SpellHarassMode"].As<MenuList>().Value == 1 &&
                                farmMenu["MyManaManager.SpellHarassKey"].As<MenuKeyBind>().Enabled;
                };
            }
        }

        internal static void AddDrawToMenu(Menu mainMenu)
        {
            if (mainMenu != null)
            {
                var newMenu = new Menu("MyManaManager.SpellFarmDraw", "Spell Farm")
                    {
                        new MenuBool("MyManaManager.DrawSpelFarm", "Draw Spell Farm Status"),
                        new MenuBool("MyManaManager.DrawSpellHarass", "Draw Spell Harass Status")
                    };
                mainMenu.Add(newMenu);

                Render.OnRender += delegate
                {
                    if (ObjectManager.GetLocalPlayer().IsDead || MenuGUI.IsChatOpen() || MenuGUI.IsShopOpen())
                    {
                        return;
                    }

                    if (newMenu["MyManaManager.DrawSpelFarm"].Enabled)
                    {
                        Vector2 MePos = Vector2.Zero;
                        Render.WorldToScreen(ObjectManager.GetLocalPlayer().Position, out MePos);

                        Render.Text(MePos.X - 57, MePos.Y + 48, System.Drawing.Color.FromArgb(242, 120, 34),
                            "Spell Farms:" + (SpellFarm ? "On" : "Off"));
                    }

                    if (newMenu["MyManaManager.DrawSpellHarass"].Enabled)
                    {
                        Vector2 MePos = Vector2.Zero;
                        Render.WorldToScreen(ObjectManager.GetLocalPlayer().Position, out MePos);

                        Render.Text(MePos.X - 57, MePos.Y + 68, System.Drawing.Color.FromArgb(242, 120, 34),
                            "Spell Harass:" + (SpellHarass ? "On" : "Off"));
                    }
                };
            }
        }
    }
}
