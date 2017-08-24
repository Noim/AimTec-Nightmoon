namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;
    using Aimtec.SDK.Util.Cache;

    using SharpShooter.MyBase;
    using SharpShooter.MyCommon;

    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Twitch : MyLogic
    {
        public Twitch()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.25f, 100f, 1400f, false, SkillshotType.Circle);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 1200f);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 975f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddSlider("ComboQCount", "Use Q| Enemies Count >= x", 3, 1, 5);
            ComboOption.AddSlider("ComboQRange", "Use Q| Search Enemies Range", 600, 0, 1800);
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboEKill", "Use E| When Target Can KillAble");
            ComboOption.AddBool("ComboEFull", "Use E| When Target have Full Stack", false);
            ComboOption.AddR();
            ComboOption.AddBool("ComboRKillSteal", "Use R| When Target Can KillAble");
            ComboOption.AddSlider("ComboRCount", "Use R| Enemies Count >= x", 3, 1, 5);

            HarassOption.AddMenu();
            HarassOption.AddW();
            HarassOption.AddE();
            HarassOption.AddBool("HarassEStack", "Use E| When Target will Leave E Range");
            HarassOption.AddSlider("HarassEStackCount", "Use E(Leave)| Min Stack Count >= x", 3, 1, 6);
            HarassOption.AddBool("HarassEFull", "Use E| When Target have Full Stack");
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddE();
            LaneClearOption.AddSlider("LaneClearECount", "Use E| Min KillAble Count >= x", 3, 1, 5);
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddE();

            MiscOption.AddMenu();
            MiscOption.AddBasic();

            DrawOption.AddMenu();
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(false, false, true, false, false);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PostAttack += PostAttack;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
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

        private static void KillSteal()
        {
            if (KillStealOption.UseE && E.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(E.Range) && !x.IsUnKillable()))
                {
                    if (target.IsValidTarget(E.Range) && target.Health < GetRealEDamage(target) - target.HPRegenRate)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = MyTargetSelector.GetTarget(E.Range);

            if (target.IsValidTarget(E.Range))
            {
                if (ComboOption.UseR && R.Ready)
                {
                    if (ComboOption.GetBool("ComboRKillSteal").Enabled &&
                        GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= R.Range) <= 2 &&
                        target.Health <= Me.GetAutoAttackDamage(target) * 4 + GetRealEDamage(target) * 2)
                    {
                        R.Cast();
                    }

                    if (GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= R.Range) >= ComboOption.GetSlider("ComboRCount").Value)
                    {
                        R.Cast();
                    }
                }

                if (ComboOption.UseQ && Q.Ready &&
                    GameObjects.EnemyHeroes.Count(x => x.DistanceToPlayer() <= ComboOption.GetSlider("ComboQRange").Value) >=
                    ComboOption.GetSlider("ComboQCount").Value)
                {
                    Q.Cast();
                }

                if (ComboOption.UseW && W.Ready && target.IsValidTarget(W.Range) &&
                    target.Health > GetRealEDamage(target) && GetEStackCount(target) < 6 &&
                    Me.Mana > Q.GetBasicSpell().Cost + W.GetBasicSpell().Cost + E.GetBasicSpell().Cost + R.GetBasicSpell().Cost)
                {
                    var wPred = W.GetPrediction(target);

                    if (wPred.HitChance >= HitChance.High)
                    {
                        W.Cast(wPred.CastPosition);
                    }
                }

                if (ComboOption.UseE && E.Ready && target.IsValidTarget(E.Range) &&
                    target.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom"))
                {
                    if (ComboOption.GetBool("ComboEFull").Enabled && GetEStackCount(target) >= 6)
                    {
                        E.Cast();
                    }

                    if (ComboOption.GetBool("ComboEKill").Enabled && target.Health <= GetRealEDamage(target) &&
                        target.IsValidTarget(E.Range))
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseW && W.Ready)
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

                if (HarassOption.UseE && E.Ready)
                {
                    var target = HarassOption.GetTarget(E.Range);

                    if (target.IsValidTarget(E.Range))
                    {
                        if (HarassOption.GetBool("HarassEStack").Enabled)
                        {
                            if (target.DistanceToPlayer() > E.Range * 0.8 && target.IsValidTarget(E.Range) &&
                                GetEStackCount(target) >= HarassOption.GetSlider("HarassEStackCount").Value)
                            {
                                E.Cast();
                            }
                        }

                        if (HarassOption.GetBool("HarassEFull").Enabled && GetEStackCount(target) >= 6)
                        {
                            E.Cast();
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
                if (LaneClearOption.UseE && E.Ready)
                {
                    var eKillMinionsCount =
                        GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMinion())
                            .Count(
                                x =>
                                    x.DistanceToPlayer() <= E.Range && x.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom") &&
                                    x.Health < GetRealEDamage(x));

                    if (eKillMinionsCount >= LaneClearOption.GetSlider("LaneClearECount").Value)
                    {
                        E.Cast();
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseE && E.Ready)
                {
                    var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range) && x.IsMob()).ToArray();

                    foreach (
                        var mob in
                        mobs.Where(
                            x =>
                                !x.Name.ToLower().Contains("mini") && x.DistanceToPlayer() <= E.Range &&
                               x.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom")))
                    {
                        if (mob.Health < GetRealEDamage(mob) && mob.IsValidTarget(E.Range))
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void PostAttack(object sender, PostAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() ||
                Args.Target.Health <= 0 || Orbwalker.Mode == OrbwalkingMode.None)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.obj_AI_Hero:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            if (ComboOption.UseW && W.Ready)
                            {
                                var target = (Obj_AI_Hero)Args.Target;

                                if (!target.IsDead && target.IsValidAutoRange())
                                {
                                    var wPred = W.GetPrediction(target);

                                    if (wPred.HitChance >= HitChance.High)
                                    {
                                        W.Cast(wPred.UnitPosition);
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static double GetRealEDamage(Obj_AI_Base target)
        {
            if (target != null && !target.IsDead && target.Buffs.Any(b => b.Name.ToLower() == "twitchdeadlyvenom"))
            {
                if (target.HasBuff("KindredRNoDeathBuff"))
                {
                    return 0;
                }

                if (target.HasBuff("UndyingRage") && target.GetBuff("UndyingRage").EndTime - Game.ClockTime > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("JudicatorIntervention"))
                {
                    return 0;
                }

                if (target.HasBuff("ChronoShift") && target.GetBuff("ChronoShift").EndTime - Game.ClockTime > 0.3)
                {
                    return 0;
                }

                if (target.HasBuff("FioraW"))
                {
                    return 0;
                }

                if (target.HasBuff("ShroudofDarkness"))
                {
                    return 0;
                }

                if (target.HasBuff("SivirShield"))
                {
                    return 0;
                }

                var damage = 0d;

                damage += E.Ready ? GetEDMGTwitch(target) : 0d;

                if (target.UnitSkinName == "Morderkaiser")
                {
                    damage -= target.Mana;
                }

                if (Me.HasBuff("SummonerExhaust"))
                {
                    damage = damage * 0.6f;
                }

                if (target.HasBuff("BlitzcrankManaBarrierCD") && target.HasBuff("ManaBarrier"))
                {
                    damage -= target.Mana / 2f;
                }

                if (target.HasBuff("GarenW"))
                {
                    damage = damage * 0.7f;
                }

                if (target.HasBuff("ferocioushowl"))
                {
                    damage = damage * 0.7f;
                }

                return damage;
            }

            return 0d;
        }

        internal static double GetEDMGTwitch(Obj_AI_Base target)
        {
            if (target.Buffs.All(b => b.Name.ToLower() != "twitchdeadlyvenom"))
            {
                return 0;
            }

            double eDamage = 0;

            var basicDMG = new double[] { 20, 35, 50, 65, 80 }[Me.GetSpell(SpellSlot.E).Level - 1];
            var countDMG = new double[] { 15, 20, 25, 30, 35 }[Me.GetSpell(SpellSlot.E).Level - 1] +
                           0.25f * Me.FlatPhysicalDamageMod + 0.20f * Me.FlatMagicDamageMod;

            eDamage = basicDMG + countDMG * GetEStackCount(target);

            return Me.CalculateDamage(target, DamageType.Physical, eDamage);
        }

        internal static int GetEStackCount(Obj_AI_Base target)
        {
            if (target == null || target.IsDead ||
                !target.IsValidTarget() ||
                target.Type != GameObjectType.obj_AI_Minion && target.Type != GameObjectType.obj_AI_Hero)
            {
                return 0;
            }

            return target.GetRealBuffCount("twitchdeadlyvenom");
        }
    }
}