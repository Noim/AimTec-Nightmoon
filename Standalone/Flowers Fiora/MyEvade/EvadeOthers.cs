namespace Flowers_Fiora.MyEvade
{
    #region

    using Aimtec;
    using Aimtec.SDK.Events;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.Extensions;

    using Flowers_Fiora.MyCommon;

    using System;
    using System.Linq;

    #endregion

    internal class EvadeOthers
    {
        public static Menu Menu;
        private static int RivenQTime;
        private static float RivenQRange;
        private static Vector2 RivenDashPos;

        internal static void Attach(Menu evadeMenu)
        {
            Menu = new Menu("EvadeOthers", "Evade Others")
            {
                new MenuSeperator("MadeByNightMoon", "Made by NightMoon"),
                new MenuSeperator("123123321123XD"),
                new MenuBool("EnabledWDodge", "Enabled W Block Spell"),
                new MenuSlider("EnabledWHP", "When Player HealthPercent <= x%", 100)
            };

            foreach (
                var hero in
                GameObjects.EnemyHeroes.Where(
                    i => BlockSpellDataBase.Spells.Any(a => a.ChampionName == i.ChampionName)))
            {
                var heroMenu = new Menu("Block" + hero.ChampionName.ToLower(), hero.ChampionName);
                Menu.Add(heroMenu);
            }

            foreach (
                var spell in
                BlockSpellDataBase.Spells.Where(
                    x =>
                        ObjectManager.Get<Obj_AI_Hero>().Any(
                            a => a.IsEnemy &&
                                 string.Equals(a.ChampionName, x.ChampionName,
                                     StringComparison.CurrentCultureIgnoreCase))))
            {
                var heroMenu = Menu["Block" + spell.ChampionName.ToLower()].As<Menu>();
                heroMenu.Add(new MenuBool("BlockSpell" + spell.SpellSlot, spell.ChampionName + " " + spell.SpellSlot));
            }
            evadeMenu.Add(Menu);

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Dash.HeroDashed += OnDash;
        }

        private static void OnUpdate()
        {
            if (ObjectManager.GetLocalPlayer().IsDead || !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.W) ||
                !Menu["EnabledWDodge"].Enabled)
            {
                return;
            }

            var buffs = ObjectManager.GetLocalPlayer().Buffs;

            foreach (var buff in buffs)
            {
                var time = buff.EndTime;

                switch (buff.Name.ToLower())
                {
                    case "karthusfallenonetarget":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastW();
                        }
                        break;
                    case "nautilusgrandlinetarget":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastW();
                        }
                        break;
                    case "nocturneparanoiadash":
                        if (GameObjects.EnemyHeroes.FirstOrDefault(
                                x =>
                                    !x.IsDead && x.ChampionName.ToLower() == "nocturne" &&
                                    x.Distance(ObjectManager.GetLocalPlayer()) < 500) != null)
                        {
                            CastW();
                        }
                        break;
                    case "soulshackles":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastW();
                        }
                        break;
                    case "vladimirhemoplaguedebuff":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastW();
                        }
                        break;
                    case "zedrdeathmark":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastW();
                        }
                        break;
                }
            }

            foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget()))
            {
                switch (target.ChampionName)
                {
                    case "Jax":
                        if (Menu["Blockjax"]["BlockSpellE"] != null && Menu["Blockjax"]["BlockSpellE"].Enabled)
                        {
                            if (target.HasBuff("jaxcounterstrike"))
                            {
                                var buff = target.Buffs.FirstOrDefault(x => x.Name.ToLower() == "jaxcounterstrike");

                                if (buff != null && (buff.EndTime - Game.ClockTime) * 1000 <= 650 &&
                                    ObjectManager.GetLocalPlayer().Distance(target) <= 350f)
                                {
                                    CastW();
                                }
                            }
                        }
                        break;
                    case "Riven":
                        if (Menu["Blockriven"]["BlockSpellQ"] != null && Menu["Blockriven"]["BlockSpellQ"].Enabled)
                        {
                            if (Utils.GameTimeTickCount - RivenQTime <= 100 && RivenDashPos.IsValid() &&
                                ObjectManager.GetLocalPlayer().Distance(target) <= RivenQRange)
                            {
                                CastW();
                            }
                        }
                        break;
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (ObjectManager.GetLocalPlayer().IsDead || !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.W) ||
                !Menu["EnabledWDodge"].Enabled)
            {
                return;
            }

            var target = sender as Obj_AI_Hero;

            if (target == null || target.Team == ObjectManager.GetLocalPlayer().Team || !target.IsValid ||
                Args.Target == null || string.IsNullOrEmpty(Args.SpellData.Name))
            {
                return;
            }

            var spells =
                BlockSpellDataBase.Spells.Where(
                    x =>
                        string.Equals(x.ChampionName, target.ChampionName, StringComparison.CurrentCultureIgnoreCase) &&
                         Menu["Block" + target.ChampionName.ToLower()]["BlockSpell" + x.SpellSlot.ToString()] != null &&
                        Menu["Block" + target.ChampionName.ToLower()]["BlockSpell" + x.SpellSlot.ToString()].Enabled).ToArray();

            if (spells.Any())
            {
                foreach (var x in spells)
                {
                    switch (x.ChampionName)
                    {
                        case "Alistar":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (target.Distance(ObjectManager.GetLocalPlayer()) <= 350)
                                {
                                    CastW("Alistar", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Alistar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Blitzcrank":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "PowerFistAttack")
                                {
                                    CastW("Blitzcrank", x.SpellSlot);
                                }
                            }
                            break;
                        case "Chogath":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Chogath", x.SpellSlot);
                                }
                            }
                            break;
                        case "Darius":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Darius", x.SpellSlot);
                                }
                            }
                            break;
                        case "Elise":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellData.Name == "EliseHumanQ" && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Elise", x.SpellSlot);
                                }
                            }
                            break;
                        case "Fiddlesticks":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Fiddlesticks", x.SpellSlot);
                                }
                            }
                            break;
                        case "Gangplank":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellData.Name == "Parley" && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Gangplank", x.SpellSlot);
                                }
                            }
                            break;
                        case "Garen":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Garen", x.SpellSlot);
                                }
                            }
                            break;
                        case "Hecarim":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "HecarimRampAttack")
                                {
                                    CastW("Hecarim", x.SpellSlot);
                                }
                            }
                            break;
                        case "Irelia":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Irelia", x.SpellSlot);
                                }
                            }
                            break;
                        case "Jarvan":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Jarvan", x.SpellSlot);
                                }
                            }
                            break;
                        case "Kalista":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (ObjectManager.GetLocalPlayer().HasBuff("kalistaexpungemarker") &&
                                    ObjectManager.GetLocalPlayer().Distance(target) <= 950f)
                                {
                                    CastW("Kalista", x.SpellSlot);
                                }
                            }
                            break;
                        case "Kayle":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Kayle", x.SpellSlot);
                                }
                            }
                            break;
                        case "Leesin":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Leesin", x.SpellSlot);
                                }
                            }
                            break;
                        case "Lissandra":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Lissandra", x.SpellSlot);
                                }
                            }
                            break;
                        case "Malzahar":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Mordekaiser", x.SpellSlot);
                                }
                            }
                            break;
                        case "Mordekaiser":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "MordekaiserQAttack2")
                                {
                                    CastW("Mordekaiser", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Mordekaiser", x.SpellSlot);
                                }
                            }
                            break;
                        case "Nasus":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Nasus", x.SpellSlot);
                                }
                            }
                            break;
                        case "Olaf":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Olaf", x.SpellSlot);
                                }
                            }
                            break;
                        case "Pantheon":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Pantheon", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Pantheon", x.SpellSlot);
                                }
                            }
                            break;
                        case "Renekton":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Renekton", x.SpellSlot);
                                }
                            }
                            break;
                        case "Rengar":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (ObjectManager.GetLocalPlayer().Distance(target) <= 300 && Args.Target.IsMe)
                                {
                                    CastW("Rengar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Riven":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (ObjectManager.GetLocalPlayer().Position.Distance(target.Position) <=
                                    125f + ObjectManager.GetLocalPlayer().BoundingRadius + target.BoundingRadius)
                                {
                                    CastW("Riven", x.SpellSlot);
                                }
                            }
                            break;
                        case "Ryze":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Ryze", x.SpellSlot);
                                }
                            }
                            break;
                        case "Singed":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Singed", x.SpellSlot);
                                }
                            }
                            break;
                        case "Syndra":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "SyndraR")
                                {
                                    CastW("Syndra", x.SpellSlot);
                                }
                            }
                            break;
                        case "TahmKench":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("TahmKench", x.SpellSlot);
                                }
                            }
                            break;
                        case "Tristana":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "TristanaR")
                                {
                                    CastW("Tristana", x.SpellSlot);
                                }
                            }
                            break;
                        case "Trundle":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Trundle", x.SpellSlot);
                                }
                            }
                            break;
                        case "TwistedFate":
                            if (Args.SpellData.Name.Contains("attack") && Args.Target.IsMe &&
                                target.Buffs.Any(
                                    buff =>
                                        buff.Name == "BlueCardAttack" || buff.Name == "GoldCardAttack" ||
                                        buff.Name == "RedCardAttack"))
                            {
                                CastW("TwistedFate", x.SpellSlot);
                            }
                            break;
                        case "Veigar":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "VeigarPrimordialBurst")
                                {
                                    CastW("Veigar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Vi":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Vi", x.SpellSlot);
                                }
                            }
                            break;
                        case "Volibear":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Volibear", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Volibear", x.SpellSlot);
                                }
                            }
                            break;
                        case "Warwick":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastW("Warwick", x.SpellSlot);
                                }
                            }
                            break;
                        case "XinZhao":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "XenZhaoThrust3")
                                {
                                    CastW("XinZhao", x.SpellSlot);
                                }
                            }
                            break;
                    }
                }
            }
        }

        private static void OnPlayAnimation(Obj_AI_Base sender, Obj_AI_BasePlayAnimationEventArgs Args)
        {
            var riven = sender as Obj_AI_Hero;

            if (riven == null || riven.Team == ObjectManager.GetLocalPlayer().Team || riven.ChampionName != "Riven" || !riven.IsValid)
            {
                return;
            }


            if (Menu["Block" + riven.ChampionName.ToLower()]["BlockSpell" + SpellSlot.Q.ToString()] != null &&
                Menu["Block" + riven.ChampionName.ToLower()]["BlockSpell" + SpellSlot.Q.ToString()].Enabled)
            {
                if (Args.Animation.ToLower() == "spell1c")
                {
                    RivenQTime = Utils.GameTimeTickCount;
                    RivenQRange = riven.HasBuff("RivenFengShuiEngine") ? 225f : 150f;
                }
            }
        }

        private static void OnDash(object obj, Dash.DashArgs Args)
        {
            var riven = Args.Unit as Obj_AI_Hero;

            if (riven == null || riven.Team == ObjectManager.GetLocalPlayer().Team || riven.ChampionName != "Riven" || !riven.IsValid)
            {
                return;
            }

            if (Menu["Block" + riven.ChampionName.ToLower()]["BlockSpell" + SpellSlot.Q.ToString()] != null &&
               Menu["Block" + riven.ChampionName.ToLower()]["BlockSpell" + SpellSlot.Q.ToString()].Enabled)
            {
                RivenDashPos = Args.EndPos;
            }
        }

        private static void CastW()
        {
            if (ObjectManager.GetLocalPlayer().IsDead || !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.W))
            {
                return;
            }

            var target =
                GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.Distance(ObjectManager.GetLocalPlayer()) <= 750f)
                    .OrderBy(x => x.Distance(ObjectManager.GetLocalPlayer()))
                    .FirstOrDefault();

            ObjectManager.GetLocalPlayer().SpellBook.CastSpell(SpellSlot.W, target?.Position ?? Game.CursorPos);
        }

        private static void CastW(string name, SpellSlot spellslot)
        {
            if (ObjectManager.GetLocalPlayer().IsDead || !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.W))
            {
                return;
            }

            if (Menu["Block" + name.ToLower()]["BlockSpell" + spellslot.ToString()] != null &&
                Menu["Block" + name.ToLower()]["BlockSpell" + spellslot.ToString()].Enabled)
            {
                var target =
                   GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.Distance(ObjectManager.GetLocalPlayer()) <= 750f)
                        .OrderBy(x => x.Distance(ObjectManager.GetLocalPlayer()))
                        .FirstOrDefault();

                ObjectManager.GetLocalPlayer().SpellBook.CastSpell(SpellSlot.W, target?.Position ?? Game.CursorPos);
            }
        }
    }
}