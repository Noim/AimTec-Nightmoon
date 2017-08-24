namespace SharpShooter.MyUtility
{
    #region

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;

    using SharpShooter.MyCommon;

    using System;

    #endregion

    internal class MyAutoLevelManager
    {
        private static Menu Menu;
        private static Menu AutoLevelMenu;
        private static Random random;
        private static int playerLevel;
        private static bool canbeLevel;
        private static int lastLevelTime;

        public MyAutoLevelManager()
        {
            Initializer();
        }

        private static void Initializer()
        {
            playerLevel = ObjectManager.GetLocalPlayer().Level;
            canbeLevel = ObjectManager.GetLocalPlayer().Level == 1;
            random = new Random(Game.TickCount);
            Menu = MyMenuExtensions.UtilityMenu;

            AutoLevelMenu = new Menu("SharpShooter.MyUtility.AutoLevelMenu", "Auto Level")
            {
                new MenuBool("SharpShooter.MyUtility.AutoLevelMenu.Enabled", "Enabled", false),
                new MenuBool("SharpShooter.MyUtility.AutoLevelMenu.R", "Auto Level R"),
                new MenuSlider("SharpShooter.MyUtility.AutoLevelMenu.MinDelay", "Min Level Delay", 500, 0, 2000),
                new MenuSlider("SharpShooter.MyUtility.AutoLevelMenu.MaxDelay", "Max Level Delay", 2000, 0, 5000),
                new MenuBool("SharpShooter.MyUtility.AutoLevelMenu.QWE", "Auto Level QWE"),
                new MenuList("SharpShooter.MyUtility.AutoLevelMenu.Mode", "Auto Level Mode: ", new []{"QWE", "QEW", "WQE", "WEQ", "EQW", "EWQ"}, 0)
            };
            Menu.Add(AutoLevelMenu);

            Obj_AI_Base.OnLevelUp += OnLevelUp;
            Game.OnUpdate += OnUpdate;
        }

        private static void OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs Args)
        {
            if (sender.IsMe)
            {
                if (Args.NewLevel > playerLevel)
                {
                    playerLevel = Args.NewLevel;
                    canbeLevel = true;
                    lastLevelTime = Game.TickCount;
                }
            }
        }

        private static void OnUpdate()
        {
            if (!AutoLevelMenu["SharpShooter.MyUtility.AutoLevelMenu.Enabled"].Enabled)
            {
                return;
            }

            if (Game.TickCount - lastLevelTime > 6000 + Game.Ping)
            {
                canbeLevel = false;
            }

            if (canbeLevel == false)
            {
                return;
            }

            if (AutoLevelMenu["SharpShooter.MyUtility.AutoLevelMenu.R"].Enabled && canbeLevel)
            {
                switch (ObjectManager.GetLocalPlayer().Level)
                {
                    case 6:
                        if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.R).Level == 0)
                        {
                            ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.R);
                            canbeLevel = false;
                            lastLevelTime = Game.TickCount;
                        }
                        break;
                    case 11:
                        if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.R).Level == 1)
                        {
                            ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.R);
                            canbeLevel = false;
                            lastLevelTime = Game.TickCount;
                        }
                        break;
                    case 16:
                        if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.R).Level == 2)
                        {
                            ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.R);
                            canbeLevel = false;
                            lastLevelTime = Game.TickCount;
                        }
                        break;
                }
            }

            if (AutoLevelMenu["SharpShooter.MyUtility.AutoLevelMenu.QWE"].Enabled && canbeLevel)
            {
                if (ObjectManager.GetLocalPlayer().Level != 6 && ObjectManager.GetLocalPlayer().Level != 11 &&
                    ObjectManager.GetLocalPlayer().Level != 16)
                {
                    Aimtec.SDK.Util.DelayAction.Queue(
                        random.Next(AutoLevelMenu["SharpShooter.MyUtility.AutoLevelMenu.MinDelay"].Value,
                            AutoLevelMenu["SharpShooter.MyUtility.AutoLevelMenu.MaxDelay"].Value), AutoLevelEvent);
                }
            }
        }

        private static void AutoLevelEvent()
        {
            switch (AutoLevelMenu["SharpShooter.MyUtility.AutoLevelMenu.Mode"].As<MenuList>().Value)
            {
                case 0:
                    {
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.Q);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.W);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.E);
                        canbeLevel = false;
                        lastLevelTime = Game.TickCount;
                    }
                    break;
                case 1:
                    {
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.Q);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.E);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.W);
                        canbeLevel = false;
                        lastLevelTime = Game.TickCount;
                    }
                    break;
                case 2:
                    {
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.W);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.Q);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.E);
                        canbeLevel = false;
                        lastLevelTime = Game.TickCount;
                    }
                    break;
                case 3:
                    {
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.W);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.E);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.Q);
                        canbeLevel = false;
                        lastLevelTime = Game.TickCount;
                    }
                    break;
                case 4:
                    {
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.E);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.Q);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.W);
                        canbeLevel = false;
                        lastLevelTime = Game.TickCount;
                    }
                    break;
                case 5:
                    {
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.E);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.W);
                        ObjectManager.GetLocalPlayer().SpellBook.LevelSpell(SpellSlot.Q);
                        canbeLevel = false;
                        lastLevelTime = Game.TickCount;
                    }
                    break;
            }
        }
    }
}
