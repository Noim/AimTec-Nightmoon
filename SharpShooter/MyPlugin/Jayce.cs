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

    using System;
    using System.Linq;

    using static SharpShooter.MyCommon.MyMenuExtensions;

    #endregion

    internal class Jayce : MyLogic
    {
        private static float qCd, qCdEnd;
        private static float q1Cd, q1CdEnd;
        private static float wCd, wCdEnd;
        private static float w1Cd, w1CdEnd;
        private static float eCd, eCdEnd;
        private static float e1Cd, e1CdEnd;

        private static bool isMelee => !Me.HasBuff("jaycestancegun");
        private static bool isWActive => Me.Buffs.Any(buffs => buffs.Name.ToLower() == "jaycehypercharge");

        public Jayce()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1050f);
            Q.SetSkillshot(0.25f, 79f, 1200f, true, SkillshotType.Line);

            Q2 = new Aimtec.SDK.Spell(SpellSlot.Q, 600f) { Speed = float.MaxValue, Delay = 0.25f };

            QE = new Aimtec.SDK.Spell(SpellSlot.Q, 1650f);
            QE.SetSkillshot(0.35f, 98f, 1900f, true, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W);

            W2 = new Aimtec.SDK.Spell(SpellSlot.W, 350f);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 650f);
            E.SetSkillshot(0.1f, 120f, float.MaxValue, false, SkillshotType.Circle);

            E2 = new Aimtec.SDK.Spell(SpellSlot.E, 240f) { Speed = float.MaxValue, Delay = 0.25f };

            R = new Aimtec.SDK.Spell(SpellSlot.R);

            ComboOption.AddMenu();
            ComboOption.AddBool("UsQECombo", "Use Cannon Q");
            ComboOption.AddBool("UseWCombo", "Use Cannon W");
            ComboOption.AddBool("UseECombo", "Use Cannon E");
            ComboOption.AddBool("UsQEComboHam", "Use Hammer Q");
            ComboOption.AddBool("UseWComboHam", "Use Hammer W");
            ComboOption.AddBool("UseEComboHam", "Use Hammer E");
            ComboOption.AddBool("UseRCombo", "Use R Switch");

            HarassOption.AddMenu();
            HarassOption.AddBool("UsQEHarass", "Use Cannon Q");
            HarassOption.AddBool("UseWHarass", "Use Cannon W");
            HarassOption.AddBool("UseEHarass", "Use Cannon E");
            HarassOption.AddBool("UsQEHarassHam", "Use Hammer Q", false);
            HarassOption.AddBool("UseWHarassHam", "Use Hammer W", false);
            HarassOption.AddBool("UseEHarassHam", "Use Hammer E", false);
            HarassOption.AddBool("UseRHarass", "Use R Switch");
            HarassOption.AddMana(60);
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddBool("UsQEFarm", "Use Cannon Q");
            LaneClearOption.AddBool("UseRFarm", "Use R Switch");
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddBool("UsQEJungle", "Use Cannon Q");
            JungleClearOption.AddBool("UseWJungle", "Use Cannon W");
            JungleClearOption.AddBool("UseEJungle", "Use Cannon E");
            JungleClearOption.AddBool("UsQEJungleHam", "Use Hammer Q");
            JungleClearOption.AddBool("UseWJungleHam", "Use Hammer W");
            JungleClearOption.AddBool("UseEJungleHam", "Use Hammer E");
            JungleClearOption.AddBool("UseRJungle", "Use R Switch");
            JungleClearOption.AddMana();

            FleeOption.AddMenu();
            FleeOption.AddQ();
            FleeOption.AddE();
            FleeOption.AddR();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddE();
            KillStealOption.AddBool("UsQEEKS", "Use QE");
            KillStealOption.AddR();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddE();
            MiscOption.AddBool("E", "forceGate", "Auto E| After Q", false);
            MiscOption.AddSlider("E", "gatePlace", "Gate Place Distance", 50, 50, 110);
            MiscOption.AddSlider("E", "autoE", "Auto E Save|When Player HealthPercent < x%", 20, 0, 101);
            MiscOption.AddSetting("QE");
            MiscOption.AddKey("QE", "SemiQE", "Semi-manual QE Key", KeyCode.T, KeybindType.Press);
            MiscOption.AddList("QE", "SemiQEMode", "Semi-manual QE Mode", new[] { "To Target", "To Mouse" });

            DrawOption.AddMenu();
            DrawOption.AddRange(Q, "Cannon Q");
            DrawOption.AddRange(QE, "Cannon Q Extend");
            DrawOption.AddRange(W, "Cannon W");
            DrawOption.AddRange(E, "Cannon E");
            DrawOption.AddRange(Q, "Hammer Q");
            DrawOption.AddRange(W, "Hammer W");
            DrawOption.AddRange(E, "Hammer E");
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, true, true, false, false);
            DrawOption.AddBool("DrawCoolDown", "Draw Spell CoolDown");

            Game.OnUpdate += OnUpdate;
            Orbwalker.PostAttack += PostAttack;
            GameObject.OnCreate += OnCreate;
            Gapcloser.OnGapcloser += OnGapcloser;
            Render.OnRender += OnRender;
        }

        private static void PostAttack(object sender, PostAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0 ||
                wCd != 0 || W.GetBasicSpell().Level == 0 || !W.Ready || isWActive)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.obj_AI_Hero:
                    {
                        var target = Args.Target as Obj_AI_Hero;

                        if (target != null && target.IsValidTarget())
                        {
                            if (Orbwalker.Mode == OrbwalkingMode.Combo)
                            {
                                if (ComboOption.GetBool("UseWCombo").Enabled)
                                {
                                    if (target.IsValidAutoRange())
                                    {
                                        Orbwalker.ResetAutoAttackTimer();
                                        W.Cast();
                                    }
                                }
                            }
                            else if (Orbwalker.Mode == OrbwalkingMode.Mixed ||
                                     Orbwalker.Mode == OrbwalkingMode.Laneclear && MyManaManager.SpellHarass)
                            {
                                if (HarassOption.HasEnouguMana() &&
                                    HarassOption.GetHarassTargetEnabled(target.ChampionName) &&
                                    HarassOption.GetBool("UseWHarass").Enabled)
                                {
                                    if (target.IsValidAutoRange())
                                    {
                                        Orbwalker.ResetAutoAttackTimer();
                                        W.Cast();
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private static void OnUpdate()
        {
            CalculateCooldown();

            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (MiscOption.GetBool("QE", "SemiQE").Enabled)
            {
                SemiQELogic();
            }

            if (FleeOption.isFleeKeyActive)
            {
                Flee();
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

        private static void CalculateCooldown()
        {
            if (!isMelee)
            {
                qCdEnd = Me.GetSpell(SpellSlot.Q).CooldownEnd;
                wCdEnd = Me.GetSpell(SpellSlot.W).CooldownEnd;
                eCdEnd = Me.GetSpell(SpellSlot.E).CooldownEnd;
            }
            else
            {
                q1CdEnd = Me.GetSpell(SpellSlot.Q).CooldownEnd;
                w1CdEnd = Me.GetSpell(SpellSlot.W).CooldownEnd;
                e1CdEnd = Me.GetSpell(SpellSlot.E).CooldownEnd;
            }

            qCd = Me.GetSpell(SpellSlot.Q).Level > 0 ? CheckCD(qCdEnd) : -1;
            wCd = Me.GetSpell(SpellSlot.W).Level > 0 ? CheckCD(wCdEnd) : -1;
            eCd = Me.GetSpell(SpellSlot.E).Level > 0 ? CheckCD(eCdEnd) : -1;
            q1Cd = Me.GetSpell(SpellSlot.Q).Level > 0 ? CheckCD(q1CdEnd) : -1;
            w1Cd = Me.GetSpell(SpellSlot.W).Level > 0 ? CheckCD(w1CdEnd) : -1;
            e1Cd = Me.GetSpell(SpellSlot.E).Level > 0 ? CheckCD(e1CdEnd) : -1;
        }

        private static float CheckCD(float Expires)
        {
            var time = Expires - Game.ClockTime;

            if (time < 0)
            {
                time = 0;

                return time;
            }

            return time;
        }

        private static void SemiQELogic()
        {

        }

        private static void Flee()
        {

        }

        private static void KillSteal()
        {

        }

        private static void Combo()
        {

        }

        private static void Harass()
        {

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

        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                
            }
        }

        private static void OnCreate(GameObject sender)
        {
            if (sender == null || sender.Type != GameObjectType.MissileClient ||
                !MiscOption.GetBool("E", "forceGate").Enabled || eCd != 0 || !E.Ready)
            {
                return;
            }

            var missile = sender as MissileClient;

            if (missile != null && missile.SpellCaster.IsMe &&
                string.Equals(missile.SpellData.Name, "jayceshockblastmis", StringComparison.CurrentCultureIgnoreCase))
            {
                var vec = missile.ServerPosition - Vector3.Normalize(Me.ServerPosition - missile.ServerPosition) * 100;

                E.Cast(vec);
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (E.Ready && target != null && target.IsValidTarget(E2.Range))
            {
                switch (Args.Type)
                {
                    case SpellType.Dash:
                    case SpellType.SkillShot:
                    case SpellType.Targeted:
                        {
                            if (Args.EndPosition.DistanceToPlayer() <= target.BoundingRadius + Me.BoundingRadius)
                            {
                                if (!isMelee)
                                {
                                    R.Cast();
                                }

                                if (isMelee)
                                {
                                    E.CastOnUnit(target);
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static void OnRender()
        {
            if (DrawOption.GetBool("DrawCoolDown").Enabled)
            {
                string msg;
                var QCoolDown = (int)qCd == -1 ? 0 : (int)qCd;
                var WCoolDown = (int)wCd == -1 ? 0 : (int)wCd;
                var ECoolDown = (int)eCd == -1 ? 0 : (int)eCd;
                var Q1CoolDown = (int)q1Cd == -1 ? 0 : (int)q1Cd;
                var W1CoolDown = (int)w1Cd == -1 ? 0 : (int)w1Cd;
                var E1CoolDown = (int)e1Cd == -1 ? 0 : (int)e1Cd;

                if (isMelee)
                {
                    msg = "Q: " + QCoolDown + "   W: " + WCoolDown + "   E: " + ECoolDown;
                    Render.Text(Me.FloatingHealthBarPosition.X + 30, Me.FloatingHealthBarPosition.Y - 30,
                        System.Drawing.Color.Orange, msg);
                }
                else
                {
                    msg = "Q: " + Q1CoolDown + "   W: " + W1CoolDown + "   E: " + E1CoolDown;
                    Render.Text(Me.FloatingHealthBarPosition.X + 30, Me.FloatingHealthBarPosition.Y - 30,
                        System.Drawing.Color.SkyBlue, msg);
                }
            }
        }

        private static void CastQCannon(Obj_AI_Hero target, bool useE)
        {
            var qePred = QE.GetPrediction(target);

            if (qePred.HitChance >= HitChance.High && qCd == 0 && eCd == 0 && useE)
            {
                var gateVector = Me.Position + 
                    Vector3.Normalize(target.ServerPosition - Me.Position) * MiscOption.GetSlider("E", "gatePlace").Value;

                if (Me.Distance(qePred.CastPosition) < QE.Range + 100)
                {
                    if (E.Ready && QE.Ready)
                    {
                        E.Cast(gateVector);
                        QE.Cast(qePred.CastPosition);
                        return;
                    }
                }
            }

            var qPred = Q.GetPrediction(target);

            if ((!useE || !E.Ready) && qCd == 0 && qPred.HitChance >= HitChance.High &&
                Me.Distance(target.ServerPosition) <= Q.Range && Q.Ready && eCd != 0)
            {
                Q.Cast(target);
            }
        }

        private static void CastQCannonMouse()
        {
            Me.IssueOrder(OrderType.MoveTo, Game.CursorPos);

            if (isMelee && !R.Ready)
            {
                return;
            }

            if (isMelee && R.Ready)
            {
                R.Cast();
                return;
            }

            if (eCd == 0 && qCd == 0 && !isMelee)
            {
                if (MiscOption.GetList("QE", "SemiQEMode").Value == 1)
                {
                    var gateDis = MiscOption.GetSlider("E", "gatePlace").Value;
                    var gateVector = Me.ServerPosition + Vector3.Normalize(Game.CursorPos - Me.ServerPosition) * gateDis;

                    if (E.Ready && QE.Ready)
                    {
                        E.Cast(gateVector);
                        QE.Cast(Game.CursorPos);
                    }
                }
                else
                {
                    var qTarget = TargetSelector.GetTarget(QE.Range);

                    if (qTarget != null && qTarget.IsValidTarget(QE.Range) && qCd == 0)
                    {
                        CastQCannon(qTarget, true);
                    }
                }
            }
        }

        private static bool ECheck(Obj_AI_Hero target, bool usQE, bool useW)
        {
            if (GetEDamage(target) >= target.Health)
            {
                return true;
            }

            if ((qCd == 0 && usQE || wCd == 0 && useW) && q1Cd != 0 && w1Cd != 0)
            {
                return true;
            }

            if (WallStun(target))
            {
                return true;
            }

            if (Me.HealthPercent() <= MiscOption.GetSlider("E", "autoE").Value)
            {
                return true;
            }

            return false;
        }

        private static void SwitchFormCheck(Obj_AI_Hero target, bool usQE, bool useW, bool usQE2, bool useW2, bool useE2)
        {
            if (target == null)
                return;

            if (target.Health > 80)
            {
                if (target.Distance(Me) > 650 && R.Ready && qCd == 0 && wCd == 0 && eCd == 0 && isMelee)
                {
                    R.Cast();
                    return;
                }

                if ((qCd != 0 || !usQE) && (wCd != 0 && (!isWActive || !useW)) && R.Ready && HammerAllReady() &&
                    !isMelee && Me.Distance(target.ServerPosition) < 650 && (usQE2 || useW2 || useE2))
                {
                    R.Cast();
                    return;
                }
            }

            if (!isMelee && target.Distance(Me) <= Q2.Range + 150 &&
                target.Health <= GetEDamage(target, true) + GetQDamage(target, true) + Me.GetAutoAttackDamage(target) &&
                q1Cd == 0 && e1Cd == 0)
            {
                R.Cast();
                return;
            }

            if ((qCd == 0 && usQE || wCd == 0 && useW && R.Ready) && isMelee)
            {
                R.Cast();
                return;
            }

            if (q1Cd != 0 && w1Cd != 0 && e1Cd != 0 && isMelee && R.Ready)
            {
                R.Cast();
            }
        }

        private static bool HammerAllReady()
        {
            return q1Cd == 0 && w1Cd == 0 && e1Cd == 0;
        }

        private static bool WallStun(Obj_AI_Hero target)
        {
            if (target == null)
                return false;

            var pred = E.GetPrediction(target);
            var pushedPos = pred.UnitPosition + Vector3.Normalize(pred.UnitPosition - Me.ServerPosition) * 350;

            return IsPassWall(target.ServerPosition, pushedPos);
        }

        private static bool IsPassWall(Vector3 start, Vector3 end)
        {
            double count = Vector3.Distance(start, end);

            for (uint i = 0; i <= count; i += 25)
            {
                var pos = start.To2D().Extend(Me.ServerPosition.To2D(), -i);

                if (pos.IsWall())
                {
                    return true;
                }
            }

            return false;
        }

        internal static double GetQDamage(Obj_AI_Base target, bool getmeleeDMG = false, bool getcannonDMG = false)
        {
            var level = Q.GetBasicSpell().Level - 1;

            var meleeDMG = new double[] { 35, 70, 105, 140, 175, 210 }[level] + 1 * Me.FlatPhysicalDamageMod;
            var cannonDMG = new double[] { 70, 120, 170, 220, 270, 320 }[level] + 1.2 * Me.FlatPhysicalDamageMod;

            if (getmeleeDMG)
            {
                return Me.CalculateDamage(target, DamageType.Physical, meleeDMG);
            }

            if (getcannonDMG)
            {
                return Me.CalculateDamage(target, DamageType.Physical, cannonDMG);
            }

            return Me.CalculateDamage(target, DamageType.Physical, isMelee ? meleeDMG : cannonDMG);
        }

        internal static double GetWDamage(Obj_AI_Base target, bool ignoreCheck = false)
        {
            if (!isMelee || !ignoreCheck)
            {
                return 0;
            }

            var level = W.GetBasicSpell().Level - 1;

            var meleeDMG = new double[] { 100, 160, 220, 280, 340, 400 }[level] + 1 * Me.FlatPhysicalDamageMod;

            return Me.CalculateDamage(target, DamageType.Magical, meleeDMG);
        }

        internal static double GetEDamage(Obj_AI_Base target, bool ignoreCheck = false)
        {
            if (!isMelee || !ignoreCheck)
            {
                return 0;
            }

            var level = E.GetBasicSpell().Level - 1;

            var meleeDMG = new[] { 0.08, 0.104, 0.128, 0.152, 0.176, 0.20 }[level] * target.MaxHealth + 1 * Me.FlatPhysicalDamageMod;
            var mobDMG = new double[] { 200, 300, 400, 500, 600 }[level];

            if (target.Type == GameObjectType.obj_AI_Hero)
            {
                if (meleeDMG > mobDMG)
                {
                    return mobDMG;
                }
            }

            return Me.CalculateDamage(target, DamageType.Magical, meleeDMG);
        }
    }
}
