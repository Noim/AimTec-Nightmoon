// Copyright 2014 - 2014 Esk0r
// EvadeManager.cs is part of Evade.
// 
// Evade is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Evade is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Evade. If not, see <http://www.gnu.org/licenses/>.

// GitHub: https://github.com/Esk0r/LeagueSharp/blob/master/Evade

namespace Flowers_Yasuo.MyEvade
{
    #region

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;
    using Aimtec.SDK.Extensions;

    using Flowers_Yasuo.MyBase;
    using Flowers_Yasuo.MyCommon;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    public static class EvadeManager
    {
        public const int SkillShotsExtraRadius = 9;
        public const int SkillShotsExtraRange = 20;
        public const int GridSize = 10;
        public const int EvadingFirstTimeOffset = 250;
        public const int EvadingSecondTimeOffset = 80;

        public static Menu Menu, EvadeSpellMenu, SkillShotMenu;

        public static readonly List<Skillshot> DetectedSkillshots = new List<Skillshot>();

        public static void Attach(Menu mainMenu)
        {
            Menu = new Menu("EvadeSpells", "Evade Spells")
            {
                new MenuSeperator("MadeByEsk0r", "Made by Esk0r"),
                new MenuSeperator("123123123123XD")
            };

            Collision.Init();

            EvadeSpellMenu = new Menu("evadeSpells", "Spell Settings");
            {
                foreach (var spell in EvadeSpellDatabase.Spells)
                {
                    var name = spell.Slot;
                    EvadeSpellMenu.Add(new MenuSeperator("YasuoEvade_" + name, "Yasuo " + name + " Settings"));
                    EvadeSpellMenu.Add(new MenuSlider("DangerLevel_" + name, "Danger level", spell.DangerLevel, 1, 5));

                    if (spell.Slot == SpellSlot.E)
                    {
                        EvadeSpellMenu.Add(new MenuBool("ETower", "Under Tower"));
                    }

                    EvadeSpellMenu.Add(new MenuBool("Enabled" + name, "Enabled"));
                }
            }
            Menu.Add(EvadeSpellMenu);

            SkillShotMenu = new Menu("Skillshots", "Skillshot Settings");
            {
                foreach (
                    var hero in
                    GameObjects.EnemyHeroes.Where(
                        i => SpellDatabase.Spells.Any(a => a.ChampionName == i.ChampionName)))
                {
                    var heroMenu = new Menu("Evade" + hero.ChampionName.ToLower(), hero.ChampionName);
                    SkillShotMenu.Add(heroMenu);
                }

                foreach (
                    var spell in
                    SpellDatabase.Spells.Where(
                        i => GameObjects.EnemyHeroes.Any(a => a.ChampionName == i.ChampionName)))
                {
                    var heroMenu = SkillShotMenu["Evade" + spell.ChampionName.ToLower()].As<Menu>();

                    heroMenu.Add(new MenuSeperator("EvadeSpell" + spell.MenuItemName, spell.SpellName + " " + spell.Slot));
                    heroMenu.Add(new MenuSlider("DangerLevel" + spell.MenuItemName, "Danger Level", spell.DangerValue, 1, 5));
                    heroMenu.Add(new MenuBool("Enabled" + spell.MenuItemName, "Enabled"));
                }
            }
            Menu.Add(SkillShotMenu);

            mainMenu.Add(Menu);

            Game.OnUpdate += OnUpdate;
            SkillshotDetector.OnDeleteMissile += OnDeleteMissile;
            SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
        }

        private static void OnUpdate()
        {
            DetectedSkillshots.RemoveAll(skillshot => !skillshot.IsActive());

            foreach (var skillshot in DetectedSkillshots)
            {
                skillshot.OnUpdate();
            }

            if (ObjectManager.GetLocalPlayer().IsDead || ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.SpellImmunity) ||
                ObjectManager.GetLocalPlayer().HasBuffOfType(BuffType.SpellShield))
            {
                return;
            }

            if (!IsSafePath(ObjectManager.GetLocalPlayer().GetWaypoints(), 100).IsSafe &&
                !IsSafe(ObjectManager.GetLocalPlayer().ServerPosition.To2D()).IsSafe)
            {
                TryToEvade(IsSafe(ObjectManager.GetLocalPlayer().ServerPosition.To2D()).SkillshotList, Game.CursorPos.To2D());
            }
        }

        private static void TryToEvade(List<Skillshot> HitBy, Vector2 Pos)
        {
            var dangerLevel = 0;

            foreach (var skillshot in HitBy)
            {
                dangerLevel = Math.Max(dangerLevel, skillshot.GetSlider("DangerLevel"));
            }

            foreach (var evadeSpell in EvadeSpellDatabase.Spells)
            {
                if (evadeSpell.Enabled && evadeSpell.DangerLevel <= dangerLevel && evadeSpell.IsReady())
                {
                    if (evadeSpell.Slot == SpellSlot.E)
                    {
                        var dodgeList =
                            GetEvadeTargets(evadeSpell)
                                .Where(
                                    x =>
                                        IsSafe(MyExtraManager.PosAfterE(x).To2D()).IsSafe &&
                                        (!MyExtraManager.UnderTower(MyExtraManager.PosAfterE(x)) ||
                                         EvadeSpellMenu["ETower"].Enabled)).ToArray();

                        if (dodgeList.Any())
                        {
                            var dodgeTarget =
                                dodgeList.Where(x => !x.HasBuff("YasuoDashWrapper"))
                                    .MinOrDefault(i => MyExtraManager.PosAfterE(i).Distance(Pos.To3D()));

                            if (dodgeTarget != null && dodgeTarget.DistanceToPlayer() <= MyLogic.E.Range &&
                                MyExtraManager.CanCastE(dodgeTarget))
                            {
                                MyLogic.E.CastOnUnit(dodgeTarget);
                            }
                        }
                    }

                    if (evadeSpell.Slot == SpellSlot.W)
                    {
                        var skillShotList =
                            HitBy.Where(
                                x => x.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall)).ToArray();

                        if (skillShotList.Any())
                        {
                            var willHitList =
                                skillShotList.Where(
                                    x =>
                                        x.IsAboutToHit(
                                            150 + evadeSpell.Delay,
                                            ObjectManager.GetLocalPlayer())).ToArray();

                            if (willHitList.Any())
                            {
                                if (
                                    willHitList.OrderByDescending(
                                            x => dangerLevel)
                                        .Any(
                                            x =>
                                                MyLogic.W.Cast(
                                                    ObjectManager.GetLocalPlayer().ServerPosition.Extend(x.Start.To3D(),
                                                        300))))
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }


        private static void OnDeleteMissile(Skillshot skillshot, MissileClient missile)
        {
            if (skillshot.SpellData.SpellName == "VelkozQ")
            {
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                var direction = skillshot.Direction.Perpendicular();

                if (DetectedSkillshots.Count(s => s.SpellData.SpellName == "VelkozQSplit") == 0)
                {
                    for (var i = -1; i <= 1; i = i + 2)
                    {
                        var skillshotToAdd = new Skillshot(
                            DetectionType.ProcessSpell, spellData, Utils.GameTimeTickCount, missile.Position.To2D(),
                            missile.Position.To2D() + i * direction * spellData.Range, skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                    }
                }
            }
        }

        private static void OnDetectSkillshot(Skillshot skillshot)
        {
            var alreadyAdded = false;

            foreach (var item in DetectedSkillshots)
            {
                if (item.SpellData.SpellName == skillshot.SpellData.SpellName &&
                    item.Unit.NetworkId == skillshot.Unit.NetworkId &&
                    skillshot.Direction.AngleBetween(item.Direction) < 5 &&
                    (skillshot.Start.Distance(item.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0))
                {
                    alreadyAdded = true;
                }
            }

            if (skillshot.Unit.Team == ObjectManager.GetLocalPlayer().Team)
            {
                return;
            }

            if (skillshot.Start.Distance(ObjectManager.GetLocalPlayer().ServerPosition.To2D()) >
                (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
            {
                return;
            }

            if (!alreadyAdded || skillshot.SpellData.DontCheckForDuplicates)
            {
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;

                        for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                            i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                            i++)
                        {
                            var end = skillshot.Start +
                                      skillshot.SpellData.Range *
                                      originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i);
                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                                skillshot.Unit);

                            DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "UFSlash")
                    {
                        skillshot.SpellData.MissileSpeed = 1600 + (int)skillshot.Unit.MoveSpeed;
                    }

                    if (skillshot.SpellData.SpellName == "SionR")
                    {
                        skillshot.SpellData.MissileSpeed = (int)skillshot.Unit.MoveSpeed;
                    }

                    if (skillshot.SpellData.Invert)
                    {
                        var newDirection = -(skillshot.End - skillshot.Start).Normalized();
                        var end = skillshot.Start + newDirection * skillshot.Start.Distance(skillshot.End);
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.Centered)
                    {
                        var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                        var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    var unit = skillshot.Unit as Obj_AI_Hero;

                    if (unit != null && skillshot.SpellData.SpellName == "TaricE" && unit.ChampionName == "Taric")
                    {
                        var target =
                            GameObjects.Heroes.FirstOrDefault(
                                h => h.Team == skillshot.Unit.Team && h.IsVisible && h.HasBuff("taricwleashactive"));

                        if (target != null)
                        {
                            var start = target.ServerPosition.To2D();
                            var direction = (skillshot.OriginalEnd - start).Normalized();
                            var end = start + direction * skillshot.SpellData.Range;
                            var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick,
                                    start, end, target)
                            {
                                OriginalEnd = skillshot.OriginalEnd
                            };
                            DetectedSkillshots.Add(skillshotToAdd);
                        }
                    }

                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        var angle = 60;
                        var edge1 =
                            (skillshot.End - skillshot.Unit.ServerPosition.To2D()).Rotated(
                                -angle / 2 * (float)Math.PI / 180);
                        var edge2 = edge1.Rotated(angle * (float)Math.PI / 180);

                        var positions = new List<Vector2>();

                        var explodingQ = DetectedSkillshots.FirstOrDefault(s => s.SpellData.SpellName == "SyndraQ");

                        if (explodingQ != null)
                        {
                            positions.Add(explodingQ.End);
                        }

                        foreach (var minion in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            if (minion.Name == "Seed" && !minion.IsDead && minion.Team != ObjectManager.GetLocalPlayer().Team)
                            {
                                positions.Add(minion.ServerPosition.To2D());
                            }
                        }

                        foreach (var position in positions)
                        {
                            var v = position - skillshot.Unit.ServerPosition.To2D();
                            if (edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                position.Distance(skillshot.Unit) < 800)
                            {
                                var start = position;
                                var end = skillshot.Unit.ServerPosition.To2D()
                                    .Extend(
                                        position,
                                        skillshot.Unit.Distance(position) > 200 ? 1300 : 1000);
                                var startTime = skillshot.StartTick;

                                startTime += (int)(150 + skillshot.Unit.Distance(position) / 2.5f);
                                var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, startTime, start, end,
                                    skillshot.Unit);
                                DetectedSkillshots.Add(skillshotToAdd);
                            }
                        }
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "MalzaharQ")
                    {
                        var start = skillshot.End - skillshot.Direction.Perpendicular() * 400;
                        var end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "ZyraQ")
                    {
                        var start = skillshot.End - skillshot.Direction.Perpendicular() * 450;
                        var end = skillshot.End + skillshot.Direction.Perpendicular() * 450;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Unit);
                        DetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }

                    if (skillshot.SpellData.SpellName == "DianaArc")
                    {
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, SpellDatabase.GetByName("DianaArcArc"), skillshot.StartTick,
                            skillshot.Start, skillshot.End,
                            skillshot.Unit);

                        DetectedSkillshots.Add(skillshotToAdd);
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        var d2 = d1 * 0.4f;
                        var d3 = d2 * 0.69f;
                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");
                        var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        var bounce2Pos = bounce1Pos + skillshot.Direction * d3;

                        bounce1SpellData.Delay =
                            (int)(skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int)(bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);

                        var bounce1 = new Skillshot(
                            skillshot.DetectionType, bounce1SpellData, skillshot.StartTick, skillshot.End, bounce1Pos,
                            skillshot.Unit);
                        var bounce2 = new Skillshot(
                            skillshot.DetectionType, bounce2SpellData, skillshot.StartTick, bounce1Pos, bounce2Pos,
                            skillshot.Unit);

                        DetectedSkillshots.Add(bounce1);
                        DetectedSkillshots.Add(bounce2);
                    }

                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int)(1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                    }

                    if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                    {
                        var endPos = new Vector2();

                        foreach (var s in DetectedSkillshots)
                        {
                            if (s.Unit.NetworkId == skillshot.Unit.NetworkId && s.SpellData.Slot == SpellSlot.E)
                            {
                                var extendedE = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start,
                                    skillshot.End + skillshot.Direction * 100, skillshot.Unit);

                                if (!extendedE.IsSafe(s.End))
                                {
                                    endPos = s.End;
                                }
                                break;
                            }
                        }

                        foreach (var m in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            if (m.UnitSkinName == "jarvanivstandard" && m.Team == skillshot.Unit.Team)
                            {
                                var extendedE = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start,
                                    skillshot.End + skillshot.Direction * 100, skillshot.Unit);

                                if (!extendedE.IsSafe(m.Position.To2D()))
                                {
                                    endPos = m.Position.To2D();
                                }
                                break;
                            }
                        }

                        if (endPos.IsValid())
                        {
                            skillshot = new Skillshot(DetectionType.ProcessSpell, SpellDatabase.GetByName("JarvanIVEQ"),
                                Utils.GameTimeTickCount, skillshot.Start, endPos, skillshot.Unit);
                            skillshot.End = endPos + 200 * (endPos - skillshot.Start).Normalized();
                            skillshot.Direction = (skillshot.End - skillshot.Start).Normalized();
                        }
                    }
                }

                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, SpellDatabase.GetByName("OriannaQend"), skillshot.StartTick,
                        skillshot.Start, skillshot.End,
                        skillshot.Unit);

                    DetectedSkillshots.Add(skillshotToAdd);
                }

                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }

                DetectedSkillshots.Add(skillshot);
            }
        }

        public static IsSafeResult IsSafe(Vector2 point)
        {
            var result = new IsSafeResult {SkillshotList = new List<Skillshot>()};

            foreach (var skillshot in DetectedSkillshots)
            {
                if (skillshot.Evade() && skillshot.IsDanger(point))
                {
                    result.SkillshotList.Add(skillshot);
                }
            }

            result.IsSafe = result.SkillshotList.Count == 0;

            return result;
        }

        public static SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0, Obj_AI_Base unit = null)
        {
            var IsSafe = true;
            var intersections = new List<FoundIntersection>();
            var intersection = new FoundIntersection();

            foreach (var skillshot in DetectedSkillshots.Where(x => x.Evade()))
            {
                var sResult = skillshot.IsSafePath(path, timeOffset, speed, delay, unit);
                IsSafe = IsSafe && sResult.IsSafe;

                if (sResult.Intersection.Valid)
                {
                    intersections.Add(sResult.Intersection);
                }
            }

            if (!IsSafe)
            {
                var intersetion = intersections.MinOrDefault(o => o.Distance);
                return new SafePathResult(false, intersetion.Valid ? intersetion : intersection);
            }

            return new SafePathResult(true, intersection);
        }

        public static List<Obj_AI_Base> GetEvadeTargets(EvadeSpellData spell, bool onlyGood = false, bool DontCheckForSafety = false)
        {
            var badTargets = new List<Obj_AI_Base>();
            var goodTargets = new List<Obj_AI_Base>();
            var allTargets = new List<Obj_AI_Base>();

            allTargets.AddRange(
                GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(spell.Range)));
            allTargets.AddRange(GameObjects.EnemyMinions.Where(x => x.IsValidTarget(spell.Range) && x.MaxHealth > 5));

            foreach (var target in allTargets.Where(x => DontCheckForSafety || IsSafe(x.ServerPosition.To2D()).IsSafe))
            {
                var pathToTarget = new List<Vector2> {ObjectManager.GetLocalPlayer().ServerPosition.To2D(), target.ServerPosition.To2D()};

                if (IsSafePath(pathToTarget, EvadingFirstTimeOffset, spell.Speed, spell.Delay).IsSafe)
                {
                    goodTargets.Add(target);
                }

                if (IsSafePath(pathToTarget, EvadingSecondTimeOffset, spell.Speed, spell.Delay).IsSafe)
                {
                    badTargets.Add(target);
                }
            }

            return goodTargets.Count > 0 ? goodTargets : (onlyGood ? new List<Obj_AI_Base>() : badTargets);
        }
    }
}