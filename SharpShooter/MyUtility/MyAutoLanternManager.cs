using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aimtec;
using Aimtec.SDK.Extensions;
using Aimtec.SDK.Menu;
using Aimtec.SDK.Menu.Components;
using Aimtec.SDK.Util.Cache;
using SharpShooter.MyCommon;

namespace SharpShooter.MyUtility
{
    internal class MyAutoLanternManager
    {
        private static Menu Menu;
        private static Menu AutoLanternMenu;

        private static Obj_AI_Minion Lantern;

        public MyAutoLanternManager()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Menu = MyMenuExtensions.UtilityMenu;

            AutoLanternMenu = new Menu("SharpShooter.MyUtility.AutoLanternMenu", "Auto Lantern")
            {
                new MenuBool("SharpShooter.MyUtility.AutoLanternMenu.Enabled", "Enabled"),
                new MenuSlider("SharpShooter.MyUtility.AutoLanternMenu.HP", "When Player HealthPercent <= x%", 30, 1, 101),
                new MenuKeyBind("SharpShooter.MyUtility.AutoLanternMenu.Key", "Catch Key", Aimtec.SDK.Util.KeyCode.X, KeybindType.Press)
            };
            Menu.Add(AutoLanternMenu);

            Game.OnUpdate += OnUpdate;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDestroy += OnDestroy;
        }

        private static void OnCreate(GameObject sender)
        {
            if (ObjectManager.GetLocalPlayer().ChampionName == "Thresh" || GameObjects.AllyHeroes.All(x => x.ChampionName != "Thresh"))
            {
                return;
            }

            if (sender != null && sender.IsAlly && sender.Type == GameObjectType.obj_AI_Minion &&
                sender.Name == "ThreshLantern")
            {
                Lantern = sender as Obj_AI_Minion;
            }
        }

        private static void OnDestroy(GameObject sender)
        {
            if (ObjectManager.GetLocalPlayer().ChampionName == "Thresh" || GameObjects.AllyHeroes.All(x => x.ChampionName != "Thresh"))
            {
                return;
            }

            if (sender != null && sender.IsAlly && sender.Type == GameObjectType.obj_AI_Minion &&
                sender.Name == "ThreshLantern")
            {
                Lantern = null;
            }
        }

        private static void OnUpdate()
        {
            if (ObjectManager.GetLocalPlayer().IsDead || ObjectManager.GetLocalPlayer().IsRecalling() || 
                ObjectManager.GetLocalPlayer().ChampionName == "Thresh" ||
                GameObjects.AllyHeroes.All(x => x.ChampionName != "Thresh") ||
                Lantern == null || !Lantern.IsValid)
            {
                return;
            }

            if (Lantern != null)
            {
                if (!Lantern.IsValid)
                {
                    Lantern = null;
                }
            }

            if (!AutoLanternMenu["SharpShooter.MyUtility.AutoLanternMenu.Enabled"].Enabled ||
                ObjectManager.GetLocalPlayer().HealthPercent() >
                AutoLanternMenu["SharpShooter.MyUtility.AutoLanternMenu.HP"].Value)
            {
                return;
            }

            if (Lantern.DistanceToPlayer() <= 450)
            {
                ObjectManager.GetLocalPlayer().SpellBook.CastSpell((SpellSlot)62, Lantern);
            }
        }
    }
}
