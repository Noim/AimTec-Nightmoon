﻿namespace SharpShooter.MyBase
{
    #region

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;

    using SharpShooter.MyCommon;

    using System;
    using System.Linq;

    #endregion

    internal class MyChampions
    {
        private static readonly string[] all =
        {
            "Ashe", "Caitlyn", "Corki", "Draven", "Ezreal", "Graves", "Jhin", "Jinx", "Kalista", "KogMaw", "Lucian",
            "Quinn", "Sivir", "Tristana", "TwistedFate", "Twitch", "Varus", "Vayne", "Xayah", "MissFortune"
        };

        public MyChampions()
        {
            Initializer();
        }

        private static void Initializer()
        {
            MyMenuExtensions.myMenu = new Menu("SharpShooter: " + ObjectManager.GetLocalPlayer().ChampionName,
                "SharpShooter: " + ObjectManager.GetLocalPlayer().ChampionName, true);
            MyMenuExtensions.myMenu.Attach();

            var creditMenu = new Menu("SharpShooter.CreditMenu", "Credit");
            {
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.MainCoder", " Main/Coder: "));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.NightMoon", "NightMoon"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.Credit", " Credit: "));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.CreditBadao", "Badao"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.CreditBrian", "Brian"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.Creditdetuks", "detuks"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.CreditEsk0r", "Esk0r"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.CreditLizzaran", "Lizzaran"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.Creditmyo", "myo"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.Creditxcsoft", "xcsoft"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.CreditxSalice", "xSalice"));
                creditMenu.Add(new MenuSeperator("SharpShooter.CreditMenu.CreditLS", "LS"));
            }
            MyMenuExtensions.myMenu.Add(creditMenu);

            var supportMenu = new Menu("SharpShooter.SupportChampion", "Support Champion");
            {
                foreach (var name in all)
                {
                    supportMenu.Add(new MenuSeperator("SharpShooter.SupportChampion.SC_" + name, name));
                }
            }
            MyMenuExtensions.myMenu.Add(supportMenu);

            if (
                all.All(
                    x =>
                        !string.Equals(x, ObjectManager.GetLocalPlayer().ChampionName,
                            StringComparison.CurrentCultureIgnoreCase)))
            {
                MyMenuExtensions.myMenu.Add(
                    new MenuSeperator("NotSupport_" + ObjectManager.GetLocalPlayer().ChampionName,
                        "Not Support: " + ObjectManager.GetLocalPlayer().ChampionName));
                Console.WriteLine("SharpShooter: " + ObjectManager.GetLocalPlayer().ChampionName +
                       " Not Support!");
                return;
            }

            MyMenuExtensions.myMenu.Add(new MenuSeperator("ASDASDG"));

            MyMenuExtensions.UtilityMenu = new Menu("SharpShooter.UtilityMenu", "Utility Settings");
            MyMenuExtensions.myMenu.Add(MyMenuExtensions.UtilityMenu);

            MyLogic.Orbwalker = new Aimtec.SDK.Orbwalking.Orbwalker();
            MyLogic.Orbwalker.Attach(MyMenuExtensions.UtilityMenu);

            var myItemManager = new MyUtility.MyItemManager();
            //var myAutoLevelManager = new MyUtility.MyAutoLevelManager();

            switch (ObjectManager.GetLocalPlayer().ChampionName)
            {
                case "Ashe":
                    var ashePlugin = new MyPlugin.Ashe();
                    break;
                case "Caitlyn":
                    var caitlynPlugin = new MyPlugin.Caitlyn();
                    break;
                case "Corki":
                    var corkiPlugin = new MyPlugin.Corki();
                    break;
                case "Draven":
                    var dravenPlugin = new MyPlugin.Draven();
                    break;
                case "Ezreal":
                    var ezrealPlugin = new MyPlugin.Ezreal();
                    break;
                case "Graves":
                    var gravesPlugin = new MyPlugin.Graves();
                    break;
                case "Jhin":
                    var jhinPlugin = new MyPlugin.Jhin();
                    break;
                case "Jinx":
                    var jinxPlugin = new MyPlugin.Jinx();
                    break;
                case "Kalista":
                    var kalistaPlugin = new MyPlugin.Kalista();
                    break;
                case "KogMaw":
                    var kogMawPlugin = new MyPlugin.KogMaw();
                    break;
                case "Lucian":
                    var lucianPlugin = new MyPlugin.Lucian();
                    break;
                case "MissFortune":
                    var missFortunePlugin = new MyPlugin.MissFortune();
                    break;
                case "Quinn":
                    var quinnPlugin = new MyPlugin.Quinn();
                    break;
                case "Sivir":
                    var sivirPlugin = new MyPlugin.Sivir();
                    break;
                case "Tristana":
                    var tristanaPlugin = new MyPlugin.Tristana();
                    break;
                case "TwistedFate":
                    var twistedFatePlugin = new MyPlugin.TwistedFate();
                    break;
                case "Twitch":
                    var twitchPlugin = new MyPlugin.Twitch();
                    break;
                case "Varus":
                    var varusPlugin = new MyPlugin.Varus();
                    break;
                case "Vayne":
                    var vaynePlugin = new MyPlugin.Vayne();
                    break;
                case "Xayah":
                    var xayahPlugin = new MyPlugin.Xayah();
                    break;
            }

            //LoadChampionsPlugin();

            Console.WriteLine("SharpShooter: " + ObjectManager.GetLocalPlayer().ChampionName +
                              " Load Success, Made By NightMoon");
        }

        internal static object LoadChampionsPlugin()
        {
            var instance = Activator.CreateInstance("SharpShooter", "MyPlugin." + ObjectManager.GetLocalPlayer().ChampionName);
            return instance;
        }
    }
}