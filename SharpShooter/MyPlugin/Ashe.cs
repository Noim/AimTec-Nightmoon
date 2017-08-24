namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Library;
    using Flowers_Library.Prediction;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Ashe : MyLogic
    {
        public Ashe()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 1225f);
            W.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.Cone, false, HitChance.Medium);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 5000f);
            E.SetSkillshot(0.25f, 300f, 1400f, false, SkillshotType.Line);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 2000f);
            R.SetSkillshot(0.25f, 130f, 1550f, true, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboSaveMana", "Use Q |Save Mana");
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddR();
            ComboOption.AddBool("ComboRSolo", "Use R |Solo Mode");
            ComboOption.AddBool("ComboRTeam", "Use R |Team Fight");

            HarassOption.AddMenu();
            HarassOption.AddW();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddW();
            LaneClearOption.AddSlider("LaneClearWCount", "Use W |Min Hit Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddW();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddR();
            MiscOption.AddKey("R", "SemiR", "Semi-manual R Key", KeyCode.T, KeybindType.Press);
            MiscOption.AddBool("R", "AutoR", "Auto R| Anti Gapcloser");

            DrawOption.AddMenu();
            DrawOption.AddW(W);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(false, true, false, true, true);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PreAttack += PreAttack;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (MiscOption.GetKey("R", "SemiR").Enabled)
            {
                OneKeyR();
            }

            KillSteal();

            if (Orbwalker.Mode == OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Mixed)
            {
                Harass();
            }

            if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
            {
                FarmHarass();
            }
        }

        private static void OneKeyR()
        {
            Orbwalker.Move(Game.CursorPos);

            if (R.Ready)
            {
                var target = MyTargetSelector.GetTarget(R.Range);

                if (target != null && !target.HasBuffOfType(BuffType.SpellShield) && target.IsValidTarget(R.Range))
                {
                    var rPred = R.GetPrediction(target);

                    if (rPred.HitChance >= HitChance.High)
                    {
                        R.Cast(rPred.CastPosition);
                    }
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseW && W.Ready)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(W.Range)))
                {
                    if (!target.IsValidTarget(W.Range) || !(target.Health < Me.GetSpellDamage(target, SpellSlot.W)))
                    {
                        continue;
                    }

                    if (target.IsValidAutoRange() && (Me.HasBuff("AsheQAttack") || Me.HasBuff("asheqcastready")))
                    {
                        continue;
                    }

                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.CastPosition);
                        }
                    }
                }
            }

            if (KillStealOption.UseR && R.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(2000) && KillStealOption.GetKillStealTarget(x.ChampionName)))
                {
                    if (!(target.DistanceToPlayer() > 800) || !(target.Health < Me.GetSpellDamage(target, SpellSlot.R)) ||
                        target.HasBuffOfType(BuffType.SpellShield))
                    {
                        continue;
                    }

                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.HitChance >= HitChance.High)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseQ && Q.Ready && Orbwalker.GetOrbwalkingTarget() != null)
            {
                var target = Orbwalker.GetOrbwalkingTarget() as Obj_AI_Hero;

                if (target != null && !target.IsDead && target.IsValidAutoRange())
                {
                    if (Me.HasBuff("asheqcastready"))
                    {
                        Q.Cast();
                    }
                }
            }

            if (ComboOption.UseR && R.Ready)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(1200)))
                {
                    if (ComboOption.GetBool("ComboRTeam").Enabled)
                    {
                        if (target.IsValidTarget(600) && Me.CountEnemyHeroesInRange(600) >= 3 &&
                            target.CountAllyHeroesInRange(200) <= 2)
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.HitChance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }
                    }

                    if (ComboOption.GetBool("ComboRSolo").Enabled)
                    {
                        if (Me.CountEnemyHeroesInRange(800) == 1 &&
                            !target.IsValidAutoRange() &&
                            target.DistanceToPlayer() <= 700 &&
                            target.Health > Me.GetAutoAttackDamage(target) &&
                            target.Health < Me.GetSpellDamage(target, SpellSlot.R) + Me.GetAutoAttackDamage(target) * 3 &&
                            !target.HasBuffOfType(BuffType.SpellShield))
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.HitChance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }

                        if (target.DistanceToPlayer() <= 1000 &&
                            (!target.CanMoveMent() || target.HasBuffOfType(BuffType.Stun) ||
                             R.GetPrediction(target).HitChance == HitChance.Immobile))
                        {
                            var rPred = R.GetPrediction(target);

                            if (rPred.HitChance >= HitChance.High)
                            {
                                R.Cast(rPred.CastPosition);
                            }
                        }
                    }
                }
            }

            if (ComboOption.UseW && W.Ready && !Me.HasBuff("AsheQAttack"))
            {
                if (ComboOption.GetBool("ComboSaveMana").Enabled &&
                    Me.Mana > (R.Ready ? R.GetBasicSpell().Cost : 0) + W.GetBasicSpell().Cost + Q.GetBasicSpell().Cost ||
                    !ComboOption.GetBool("ComboSaveMana").Enabled)
                {
                    var target = MyTargetSelector.GetTarget(W.Range);

                    if (target.IsValidTarget(W.Range))
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.CastPosition);
                        }
                    }
                }
            }

            if (ComboOption.UseE && E.Ready)
            {
                var target = MyTargetSelector.GetTarget(1000);

                if (target != null)
                {
                    var ePred = E.GetPrediction(target);

                    if (ePred.UnitPosition.IsGrass() || target.ServerPosition.IsGrass())
                    {
                        E.Cast(ePred.UnitPosition);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseW && W.Ready && !Me.HasBuff("AsheQAttack"))
                {
                    var target = HarassOption.GetTarget(W.Range);

                    if (target.IsValidTarget(W.Range))
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void FarmHarass()
        {
            if (MyManaManager.SpellHarass)
            {
                Harass();
            }

            if (MyManaManager.SpellFarm)
            {
                LaneClear();
                JungleClear();
            }
        }

        private static void LaneClear()
        {
            if (LaneClearOption.HasEnouguMana())
            {
                if (LaneClearOption.UseW && W.Ready)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        var wFarm = W.GetSpellFarmPosition(minions);

                        if (wFarm.HitCount >= LaneClearOption.GetSlider("LaneClearWCount").Value)
                        {
                            W.Cast(wFarm.CastPosition);
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseW && !Me.HasBuff("AsheQAttack"))
                {
                    var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMob()).ToArray();

                    if (mobs.Any())
                    {
                        var wFarm = W.GetSpellFarmPosition(mobs);

                        if (wFarm.HitCount >= 2 || mobs.Any(x => MobsName.Contains(x.UnitSkinName.ToLower())) && wFarm.HitCount >= 1)
                        {
                            W.Cast(wFarm.CastPosition);
                        }
                    }
                }
            }
        }

        private static void PreAttack(object sender, PreAttackEventArgs Args)
        {
            if (Args.Target == null || Me.IsDead || Args.Target.IsDead || !Args.Target.IsValidTarget())
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.obj_AI_Hero:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            if (ComboOption.UseQ && Q.Ready)
                            {
                                var target = (Obj_AI_Hero)Args.Target;

                                if (!target.IsDead && target.IsValidAutoRange())
                                {
                                    if (Me.HasBuff("asheqcastready"))
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Minion:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                        {
                            if (Args.Target.IsMob())
                            {
                                if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseQ && Q.Ready)
                                {
                                    var mob = (Obj_AI_Minion)Args.Target;

                                    if (!mob.IsValidAutoRange() ||
                                        !(mob.Health > Me.GetAutoAttackDamage(mob) * 2) ||
                                        !MobsName.Contains(mob.UnitSkinName.ToLower()))
                                    {
                                        return;
                                    }

                                    if (Me.HasBuff("asheqcastready"))
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
                case GameObjectType.obj_AI_Turret:
                case GameObjectType.obj_HQ:
                case GameObjectType.obj_Barracks:
                case GameObjectType.obj_BarracksDampener:
                case GameObjectType.obj_Building:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Laneclear)
                        {
                            if (LaneClearOption.HasEnouguMana(true) && LaneClearOption.UseQ)
                            {
                                if (Me.CountEnemyHeroesInRange(850) == 0)
                                {
                                    if (Me.HasBuff("asheqcastready"))
                                    {
                                        Q.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (MiscOption.GetBool("R", "AutoR").Enabled && R.Ready && target != null && target.IsValidTarget(R.Range) && !Args.HaveShield)
            {
                switch (Args.Type)
                {
                    case SpellType.SkillShot:
                        {
                            if (target.IsValidTarget(300))
                            {
                                var rPred = R.GetPrediction(target);

                                R.Cast(rPred.UnitPosition);
                            }
                        }
                        break;
                    case SpellType.Melee:
                    case SpellType.Dash:
                    case SpellType.Targeted:
                        {
                            if (target.IsValidTarget(400))
                            {
                                var rPred = R.GetPrediction(target);

                                R.Cast(rPred.UnitPosition);
                            }
                        }
                        break;
                }
            }
        }
    }
}