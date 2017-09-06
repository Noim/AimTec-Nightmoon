namespace Flowers_Fiora.MyEvade
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Fiora.MyBase;
    using Flowers_Fiora.MyCommon;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    internal class EvadeTargetManager
    {
        public static Menu Menu, AttackMenu, SpellMenu;
        public static readonly List<SpellData> Spells = new List<SpellData>();
        private static readonly List<Targets> DetectedTargets = new List<Targets>();

        public static void Attach(Menu mainMenu)
        {
            Menu = new Menu("EvadeTargetMenu", "Evade Targets")
            {
                new MenuSeperator("Brian.EvadeTargetMenu.Credit", "Made by Brian"),
                new MenuSeperator("Brian.EvadeTargetMenu.Seperator"),
                new MenuBool("Brian.EvadeTargetMenu.EvadeTargetW", "Use W"),
                new MenuBool("Brian.EvadeTargetMenu.EvadeTargetETower", "-> Under Tower", false)
            };

            InitSpells();

            AttackMenu = new Menu("Brian.EvadeTargetMenu.DodgeAttackMenu", "Dodge Attack");
            {
                AttackMenu.Add(new MenuBool("Brian.EvadeTargetMenu.BAttack", "Basic Attack"));
                AttackMenu.Add(new MenuSlider("Brian.EvadeTargetMenu.BAttackHpU", "-> If Hp <", 35, 1, 99));
                AttackMenu.Add(new MenuBool("Brian.EvadeTargetMenu.CAttack", "Crit Attack"));
                AttackMenu.Add(new MenuSlider("Brian.EvadeTargetMenu.CAttackHpU", "-> If Hp <", 40, 1, 99));
            }
            Menu.Add(AttackMenu);

            SpellMenu =new Menu("Brian.EvadeTargetMenu.DodgeSpellMenu", "Dodge Spell");
            {
                foreach (var spell in Spells.Where(i => GameObjects.EnemyHeroes.Any(a => a.ChampionName == i.ChampionName)))
                {
                    SpellMenu.Add(new MenuBool("Brian.EvadeTargetMenu." + spell.MissileName,
                        spell.ChampionName + "(" + spell.Slot + ")"));
                }
            }
            Menu.Add(SpellMenu);

            mainMenu.Add(Menu);


            Game.OnUpdate += OnUpdate;
            GameObject.OnCreate += OnCreate;
            GameObject.OnDestroy += OnDestroy;
        }

        private static void InitSpells()
        {
            Spells.Add(
                new SpellData
                { ChampionName = "Ahri", SpellNames = new[] { "ahrifoxfiremissiletwo" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData
                { ChampionName = "Ahri", SpellNames = new[] { "ahritumblemissile" }, Slot = SpellSlot.R });
            Spells.Add(
                new SpellData { ChampionName = "Akali", SpellNames = new[] { "akalimota" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { ChampionName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { ChampionName = "Annie", SpellNames = new[] { "disintegrate" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Brand",
                    SpellNames = new[] { "brandconflagrationmissile" },
                    Slot = SpellSlot.E
                });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Brand",
                    SpellNames = new[] { "brandwildfire", "brandwildfiremissile" },
                    Slot = SpellSlot.R
                });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Caitlyn",
                    SpellNames = new[] { "caitlynaceintheholemissile" },
                    Slot = SpellSlot.R
                });
            Spells.Add(
                new SpellData
                { ChampionName = "Cassiopeia", SpellNames = new[] { "cassiopeiatwinfang" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { ChampionName = "Elise", SpellNames = new[] { "elisehumanq" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Ezreal",
                    SpellNames = new[] { "ezrealarcaneshiftmissile" },
                    Slot = SpellSlot.E
                });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "FiddleSticks",
                    SpellNames = new[] { "fiddlesticksdarkwind", "fiddlesticksdarkwindmissile" },
                    Slot = SpellSlot.E
                });
            Spells.Add(
                new SpellData { ChampionName = "Gangplank", SpellNames = new[] { "parley" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { ChampionName = "Janna", SpellNames = new[] { "sowthewind" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData { ChampionName = "Kassadin", SpellNames = new[] { "nulllance" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Katarina",
                    SpellNames = new[] { "katarinaq", "katarinaqmis" },
                    Slot = SpellSlot.Q
                });
            Spells.Add(
                new SpellData
                { ChampionName = "Kayle", SpellNames = new[] { "judicatorreckoning" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Leblanc",
                    SpellNames = new[] { "leblancchaosorb", "leblancchaosorbm" },
                    Slot = SpellSlot.Q
                });
            Spells.Add(new SpellData { ChampionName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData
                { ChampionName = "Malphite", SpellNames = new[] { "seismicshard" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "MissFortune",
                    SpellNames = new[] { "missfortunericochetshot", "missFortunershotextra" },
                    Slot = SpellSlot.Q
                });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Nami",
                    SpellNames = new[] { "namiwenemy", "namiwmissileenemy" },
                    Slot = SpellSlot.W
                });
            Spells.Add(
                new SpellData { ChampionName = "Nunu", SpellNames = new[] { "iceblast" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { ChampionName = "Pantheon", SpellNames = new[] { "pantheonq" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Ryze",
                    SpellNames = new[] { "spellflux", "spellfluxmissile" },
                    Slot = SpellSlot.E
                });
            Spells.Add(
                new SpellData { ChampionName = "Shaco", SpellNames = new[] { "twoshivpoison" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { ChampionName = "Shen", SpellNames = new[] { "shenvorpalstar" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { ChampionName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData { ChampionName = "Swain", SpellNames = new[] { "swaintorment" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { ChampionName = "Syndra", SpellNames = new[] { "syndrar" }, Slot = SpellSlot.R });
            Spells.Add(
                new SpellData { ChampionName = "Taric", SpellNames = new[] { "dazzle" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData { ChampionName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                { ChampionName = "Tristana", SpellNames = new[] { "detonatingshot" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData
                { ChampionName = "TwistedFate", SpellNames = new[] { "bluecardattack" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData
                { ChampionName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData
                { ChampionName = "TwistedFate", SpellNames = new[] { "redcardattack" }, Slot = SpellSlot.W });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Urgot",
                    SpellNames = new[] { "urgotheatseekinghomemissile" },
                    Slot = SpellSlot.Q
                });
            Spells.Add(
                new SpellData { ChampionName = "Vayne", SpellNames = new[] { "vaynecondemn" }, Slot = SpellSlot.E });
            Spells.Add(
                new SpellData
                { ChampionName = "Veigar", SpellNames = new[] { "veigarprimordialburst" }, Slot = SpellSlot.R });
            Spells.Add(
                new SpellData
                { ChampionName = "Viktor", SpellNames = new[] { "viktorpowertransfer" }, Slot = SpellSlot.Q });
            Spells.Add(
                new SpellData
                {
                    ChampionName = "Vladimir",
                    SpellNames = new[] { "vladimirtidesofbloodnuke" },
                    Slot = SpellSlot.E
                });
        }

        private static void OnCreate(GameObject sender)
        {
            if (!sender.IsValid<MissileClient>())
            {
                return;
            }

            var missile = (MissileClient)sender;
            if (!missile.SpellCaster.IsValid<Obj_AI_Hero>() ||
                missile.SpellCaster.Team == ObjectManager.GetLocalPlayer().Team)
            {
                return;
            }

            var unit = (Obj_AI_Hero)missile.SpellCaster;
            var spellData =
                Spells.FirstOrDefault(
                    i =>
                    i.SpellNames.Contains(missile.SpellData.Name.ToLower())
                    && SpellMenu["Brian.EvadeTargetMenu." + i.MissileName].Enabled);

            if (spellData == null && missile.SpellData.IsAutoAttack()
                && (!missile.SpellData.Name.ToLower().Contains("crit")
                        ? AttackMenu["Brian.EvadeTargetMenu.BAttack"].Enabled
                          && ObjectManager.GetLocalPlayer().HealthPercent() < AttackMenu["Brian.EvadeTargetMenu.BAttackHpU"].Value
                        : AttackMenu["Brian.EvadeTargetMenu.CAttack"].Enabled
                          && ObjectManager.GetLocalPlayer().HealthPercent() < AttackMenu["Brian.EvadeTargetMenu.CAttackHpU"].Value))
            {
                spellData = new SpellData
                { ChampionName = unit.ChampionName, SpellNames = new[] { missile.SpellData.Name } };
            }

            if (spellData == null || !missile.Target.IsMe)
            {
                return;
            }

            DetectedTargets.Add(new Targets { Start = unit.ServerPosition, Obj = missile, Time = Game.TickCount });
        }

        private static void OnDestroy(GameObject sender)
        {
            if (!sender.IsValid<MissileClient>())
            {
                return;
            }

            var missile = (MissileClient)sender;

            if (missile.SpellCaster.IsValid<Obj_AI_Hero>() && missile.SpellCaster.Team != ObjectManager.GetLocalPlayer().Team)
            {
                DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);
            }
        }

        private static void OnUpdate()
        {
            if (DetectedTargets.Any())
            {
                DetectedTargets.RemoveAll(i => i.Time - Game.TickCount > 5000);
            }

            if (ObjectManager.GetLocalPlayer().IsDead)
            {
                return;
            }

            if (ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.SpellImmunity) ||
                ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.SpellShield))
            {
                return;
            }

            if (!MyLogic.W.IsReady(300))
            {
                return;
            }

            foreach (var target in DetectedTargets.Where(i => ObjectManager.GetLocalPlayer().Distance(i.Obj.Position) < 700))
            {
                if (MyLogic.W.Ready && Menu["Brian.EvadeTargetMenu.EvadeTargetW"].Enabled && MyLogic.W.IsInRange(target.Obj, 500)
                    && MyLogic.W.Cast(ObjectManager.GetLocalPlayer().ServerPosition.Extend(target.Start, 100)))
                {
                    return;
                }
            }
        }

        public class SpellData
        {
            public string ChampionName;
            public SpellSlot Slot;
            public string[] SpellNames = { };

            public string MissileName => SpellNames.First();
        }

        private class Targets
        {
            public MissileClient Obj;
            public Vector3 Start;
            public int Time;
        }
    }
}
