namespace Flowers_Vladimir.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Events;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.Extensions;

    using System;
    using System.Linq;

    #endregion

    internal class MyEvadeManager
    {
        public static Menu Menu;
        private static int RivenQTime;
        private static float RivenQRange;
        private static Vector2 RivenDashPos;

        internal static void Attach(Menu evadeMenu)
        {
            Menu = evadeMenu;

            Menu.Add(new MenuSeperator("MadeByNightMoon", "Made by NightMoon"));
            Menu.Add(new MenuSeperator("MoreSpellDodoge", "More Spell Dodge Pls Check Evade"));
            Menu.Add(new MenuSeperator("123123321123XD"));
            Menu.Add(new MenuBool("EnabledDodge", "Enabled Block Spell"));
            Menu.Add(new MenuSlider("EnabledHP", "When Player HealthPercent <= x%", 30));

            foreach (
                var hero in
                GameObjects.EnemyHeroes.Where(
                    i => MyBlockSpellDataBase.Spells.Any(a => a.ChampionName == i.ChampionName)))
            {
                var heroMenu = new Menu("Block" + hero.ChampionName.ToLower(), hero.ChampionName);
                Menu.Add(heroMenu);
            }

            foreach (
                var spell in
                MyBlockSpellDataBase.Spells.Where(
                    x =>
                        ObjectManager.Get<Obj_AI_Hero>().Any(
                            a => a.IsEnemy &&
                                 string.Equals(a.ChampionName, x.ChampionName,
                                     StringComparison.CurrentCultureIgnoreCase))))
            {
                var heroMenu = Menu["Block" + spell.ChampionName.ToLower()].As<Menu>();
                heroMenu.Add(new MenuBool("BlockSpell" + spell.SpellSlot, spell.ChampionName + " " + spell.SpellSlot));
            }

            Game.OnUpdate += OnUpdate;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnPlayAnimation += OnPlayAnimation;
            Dash.HeroDashed += OnDash;
        }

        private static void OnUpdate()
        {
            if (ObjectManager.GetLocalPlayer().IsDead || !Menu["EnabledDodge"].Enabled ||
                ObjectManager.GetLocalPlayer().HealthPercent() > Menu["EnabledHP"].Value)
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Sivir" &&
                !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.E))
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Xayah" &&
                !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.R))
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
                            CastSpell();
                        }
                        break;
                    case "nautilusgrandlinetarget":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                    case "nocturneparanoiadash":
                        if (GameObjects.EnemyHeroes.FirstOrDefault(
                                x =>
                                    !x.IsDead && x.ChampionName.ToLower() == "nocturne" &&
                                    x.Distance(ObjectManager.GetLocalPlayer()) < 500) != null)
                        {
                            CastSpell();
                        }
                        break;
                    case "soulshackles":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                    case "vladimirhemoplaguedebuff":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                    case "zedrdeathmark":
                        if ((time - Game.ClockTime) * 1000 <= 300)
                        {
                            CastSpell();
                        }
                        break;
                }
            }
            

            foreach (var target in GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.IsValidTarget()))
            {
                switch (target.ChampionName)
                {
                    case "Jax":
                        {
                            if (Menu["Blockjax"]["BlockSpellE"] != null && Menu["Blockjax"]["BlockSpellE"].Enabled)
                            {
                                if (target.HasBuff("jaxcounterstrike"))
                                {
                                    var buff = target.Buffs.FirstOrDefault(x => x.Name.ToLower() == "jaxcounterstrike");

                                    if (buff != null && (buff.EndTime - Game.ClockTime) * 1000 <= 650 &&
                                        ObjectManager.GetLocalPlayer().ServerPosition.Distance(target.ServerPosition) <= 350f)
                                    {
                                        CastSpell();
                                    }
                                }
                            }
                        }
                        break;
                    case "Riven":
                        {
                            if (Menu["Blockriven"]["BlockSpellQ"] != null && Menu["Blockriven"]["BlockSpellQ"].Enabled)
                            {
                                if ((int)(Game.ClockTime * 1000) - RivenQTime <= 100 && !RivenDashPos.IsZero &&
                                    ObjectManager.GetLocalPlayer().Distance(target) <= RivenQRange)
                                {
                                    CastSpell();
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs Args)
        {
            if (ObjectManager.GetLocalPlayer().IsDead || !Menu["EnabledDodge"].Enabled ||
                ObjectManager.GetLocalPlayer().HealthPercent() > Menu["EnabledHP"].Value)
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Sivir" &&
                !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.E))
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Xayah" &&
                !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.R))
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
                MyBlockSpellDataBase.Spells.Where(
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
                                    CastSpell("Alistar", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Alistar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Blitzcrank":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "PowerFistAttack")
                                {
                                    CastSpell("Blitzcrank", x.SpellSlot);
                                }
                            }
                            break;
                        case "Chogath":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Chogath", x.SpellSlot);
                                }
                            }
                            break;
                        case "Darius":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Darius", x.SpellSlot);
                                }
                            }
                            break;
                        case "Elise":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellData.Name == "EliseHumanQ" && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Elise", x.SpellSlot);
                                }
                            }
                            break;
                        case "Fiddlesticks":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Fiddlesticks", x.SpellSlot);
                                }
                            }
                            break;
                        case "Gangplank":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellData.Name == "Parley" && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Gangplank", x.SpellSlot);
                                }
                            }
                            break;
                        case "Garen":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Garen", x.SpellSlot);
                                }
                            }
                            break;
                        case "Hecarim":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "HecarimRampAttack")
                                {
                                    CastSpell("Hecarim", x.SpellSlot);
                                }
                            }
                            break;
                        case "Irelia":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Irelia", x.SpellSlot);
                                }
                            }
                            break;
                        case "Jarvan":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Jarvan", x.SpellSlot);
                                }
                            }
                            break;
                        case "Kalista":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (ObjectManager.GetLocalPlayer().HasBuff("kalistaexpungemarker") &&
                                    ObjectManager.GetLocalPlayer().Distance(target) <= 950f)
                                {
                                    CastSpell("Kalista", x.SpellSlot);
                                }
                            }
                            break;
                        case "Kayle":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Kayle", x.SpellSlot);
                                }
                            }
                            break;
                        case "Leesin":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Leesin", x.SpellSlot);
                                }
                            }
                            break;
                        case "Lissandra":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Lissandra", x.SpellSlot);
                                }
                            }
                            break;
                        case "Morgana":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Morgana", x.SpellSlot);
                                }
                            }
                            break;
                        case "Malzahar":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Mordekaiser", x.SpellSlot);
                                }
                            }
                            break;
                        case "Mordekaiser":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "MordekaiserQAttack2")
                                {
                                    CastSpell("Mordekaiser", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Mordekaiser", x.SpellSlot);
                                }
                            }
                            break;
                        case "Nasus":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Nasus", x.SpellSlot);
                                }
                            }
                            break;
                        case "Olaf":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Olaf", x.SpellSlot);
                                }
                            }
                            break;
                        case "Pantheon":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Pantheon", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Pantheon", x.SpellSlot);
                                }
                            }
                            break;
                        case "Renekton":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Renekton", x.SpellSlot);
                                }
                            }
                            break;
                        case "Rengar":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (ObjectManager.GetLocalPlayer().Distance(target) <= 300 && Args.Target.IsMe)
                                {
                                    CastSpell("Rengar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Riven":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (ObjectManager.GetLocalPlayer().Position.Distance(target.Position) <=
                                    125f + ObjectManager.GetLocalPlayer().BoundingRadius + target.BoundingRadius)
                                {
                                    CastSpell("Riven", x.SpellSlot);
                                }
                            }
                            break;
                        case "Ryze":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Ryze", x.SpellSlot);
                                }
                            }
                            break;
                        case "Singed":
                            if (x.SpellSlot == SpellSlot.E && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Singed", x.SpellSlot);
                                }
                            }
                            break;
                        case "Syndra":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "SyndraR")
                                {
                                    CastSpell("Syndra", x.SpellSlot);
                                }
                            }
                            break;
                        case "TahmKench":
                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("TahmKench", x.SpellSlot);
                                }
                            }
                            break;
                        case "Tristana":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "TristanaR")
                                {
                                    CastSpell("Tristana", x.SpellSlot);
                                }
                            }
                            break;
                        case "Trundle":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Trundle", x.SpellSlot);
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
                                CastSpell("TwistedFate", x.SpellSlot);
                            }
                            break;
                        case "Veigar":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "VeigarPrimordialBurst")
                                {
                                    CastSpell("Veigar", x.SpellSlot);
                                }
                            }
                            break;
                        case "Vi":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Vi", x.SpellSlot);
                                }
                            }
                            break;
                        case "Volibear":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Volibear", x.SpellSlot);
                                }
                            }

                            if (x.SpellSlot == SpellSlot.W && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Volibear", x.SpellSlot);
                                }
                            }
                            break;
                        case "Warwick":
                            if (x.SpellSlot == SpellSlot.R && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe)
                                {
                                    CastSpell("Warwick", x.SpellSlot);
                                }
                            }
                            break;
                        case "XinZhao":
                            if (x.SpellSlot == SpellSlot.Q && Args.SpellSlot == x.SpellSlot)
                            {
                                if (Args.Target.IsMe && Args.SpellData.Name == "XenZhaoThrust3")
                                {
                                    CastSpell("XinZhao", x.SpellSlot);
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
                    RivenQTime = (int) (Game.ClockTime * 1000);
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

        private static void CastSpell()
        {
            if (ObjectManager.GetLocalPlayer().IsDead)
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Sivir" &&
                !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.E))
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Xayah" &&
                !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.R))
            {
                return;
            }

            var target =
                GameObjects.EnemyHeroes.Where(x => !x.IsDead && x.Distance(ObjectManager.GetLocalPlayer()) <= 750f)
                    .OrderBy(x => x.Distance(ObjectManager.GetLocalPlayer()))
                    .FirstOrDefault();

            if (ObjectManager.GetLocalPlayer().ChampionName == "Sivir")
            {
                ObjectManager.GetLocalPlayer().SpellBook.CastSpell(SpellSlot.E);
            }
            else if (ObjectManager.GetLocalPlayer().ChampionName == "Xayah")
            {
                ObjectManager.GetLocalPlayer().SpellBook.CastSpell(SpellSlot.R, target?.Position ?? Game.CursorPos);
            }
        }

        private static void CastSpell(string name, SpellSlot spellslot)
        {
            if (ObjectManager.GetLocalPlayer().IsDead)
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Sivir" &&
                !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.E))
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().ChampionName == "Xayah" &&
                !ObjectManager.GetLocalPlayer().SpellBook.CanUseSpell(SpellSlot.R))
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

                if (ObjectManager.GetLocalPlayer().ChampionName == "Sivir")
                {
                    ObjectManager.GetLocalPlayer().SpellBook.CastSpell(SpellSlot.E);
                }
                else if (ObjectManager.GetLocalPlayer().ChampionName == "Xayah")
                {
                    ObjectManager.GetLocalPlayer().SpellBook.CastSpell(SpellSlot.R, target?.Position ?? Game.CursorPos);
                }
            }
        }
    }
}