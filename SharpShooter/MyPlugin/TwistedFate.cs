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

    internal class TwistedFate : MyLogic
    {
        public TwistedFate()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 1450f);
            Q.SetSkillshot(0.25f, 40f, 1000f, false, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 850f);

            E = new Aimtec.SDK.Spell(SpellSlot.E);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 5500f);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddBool("ComboSaveMana", "Use Q| Save Mana To Cast W");
            ComboOption.AddW();
            ComboOption.AddList("ComboWSmartKS", "Use W| Smart Card KillAble", new[] { "First Card", "Blue Card", "Off" });
            ComboOption.AddBool("ComboDisableAA", "Auto Disable Attack| When Selecting");

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddQ();
            LaneClearOption.AddSlider("LaneClearQCount", "Use Q|Min Hit Count >= x", 4, 1, 10);
            LaneClearOption.AddW();
            LaneClearOption.AddBool("LaneClearWBlue", "Use W| Blue Card");
            LaneClearOption.AddBool("LaneClearWRed", "Use W| Red Card");
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();
            MiscOption.AddSubMenu("CardSelect", "Card Select Settings");
            MiscOption.AddKey("CardSelect", "CardSelectYellow", "Gold Card", KeyCode.W, KeybindType.Press);
            MiscOption.AddKey("CardSelect", "CardSelectBlue", "Blue Card", KeyCode.E, KeybindType.Press);
            MiscOption.AddKey("CardSelect", "CardSelectRed", "Red Card", KeyCode.T, KeybindType.Press);
            MiscOption.AddBool("CardSelect", "HumanizerSelect", "Humanizer Select Card", false);
            MiscOption.AddSlider("CardSelect", "HumanizerSelectMin", "Humanizer Select Card Min Delay", 0, 0, 2000);
            MiscOption.AddSlider("CardSelect", "HumanizerSelectMax", "Humanizer Select Card Max Delay", 2000, 0, 3500);
            MiscOption.AddQ();
            MiscOption.AddBool("Q", "AutoQImmobile", "Auto Q|Enemy Cant Movement");
            MiscOption.AddKey("Q", "SemiQ", "Semi-manual Q Key", KeyCode.Q, KeybindType.Press);
            MiscOption.AddR();
            MiscOption.AddBool("R", "UltYellow", "Auto Gold Card| In Ult");

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, true, true, false, true);

            Game.OnUpdate += OnUpdate;
            Gapcloser.OnGapcloser += OnGapcloser;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Orbwalker.PreAttack += PreAttack;
        }

        private static void OnUpdate()
        {
            if (Me.IsDead || Me.IsRecalling())
            {
                return;
            }

            if (Game.TickCount - HumanizerCardSelect.LastWSent > 3000)
            {
                HumanizerCardSelect.Select = HumanizerCards.None;
            }

            if (Game.TickCount - LastForcusTime > Orbwalker.WindUpTime)
            {
                if (Orbwalker.Mode != OrbwalkingMode.None)
                {
                    Orbwalker.ForceTarget(null);
                }
            }

            Auto();
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
                Farm();
            }
        }

        private static void Auto()
        {
            if (Q.Ready)
            {
                if (MiscOption.GetKey("Q", "SemiQ").Enabled)
                {
                    var target = MyTargetSelector.GetTarget(Q.Range);

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }

                if (MiscOption.GetBool("Q", "AutoQImmobile").Enabled)
                {
                    var target = GameObjects.EnemyHeroes.FirstOrDefault(x => x.IsValidTarget(Q.Range) && !x.CanMoveMent());

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        Q.Cast(target.ServerPosition);
                    }
                }
            }

            if (W.Ready)
            {
                if (MiscOption.GetKey("CardSelect", "CardSelectYellow").Enabled)
                {
                    HumanizerCardSelect.Select = HumanizerCards.Yellow;
                    HumanizerCardSelect.StartSelecting(HumanizerCards.Yellow);
                }

                if (MiscOption.GetKey("CardSelect", "CardSelectBlue").Enabled)
                {
                    HumanizerCardSelect.Select = HumanizerCards.Blue;
                    HumanizerCardSelect.StartSelecting(HumanizerCards.Blue);
                }

                if (MiscOption.GetKey("CardSelect", "CardSelectRed").Enabled)
                {
                    HumanizerCardSelect.Select = HumanizerCards.Red;
                    HumanizerCardSelect.StartSelecting(HumanizerCards.Red);
                }
            }
        }

        private static void KillSteal()
        {
            if (KillStealOption.UseQ)
            {
                foreach (var target in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(Q.Range)))
                {
                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        if (Me.GetSpellDamage(target, SpellSlot.Q) >= target.Health + 40 && !target.IsUnKillable())
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            if (ComboOption.UseQ && Q.Ready)
            {
                var target = MyTargetSelector.GetTarget(Q.Range);

                if (target != null && target.IsValidTarget(Q.Range))
                {
                    if (ComboOption.GetBool("ComboSaveMana").Enabled)
                    {
                        if (Me.Mana >= W.GetBasicSpell().Cost + Q.GetBasicSpell().Cost)
                        {
                            var qPred = Q.GetPrediction(target);

                            if (qPred.HitChance >= HitChance.High)
                            {
                                Q.Cast(qPred.CastPosition);
                            }
                        }
                    }
                    else
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }

            if (ComboOption.UseW && W.Ready)
            {
                var target = MyTargetSelector.GetTarget(W.Range);

                if (target != null && target.IsValidTarget())
                {
                    if (ComboOption.GetList("ComboWSmartKS").Value != 2 &&
                        target.Health <= Me.GetSpellDamage(target, SpellSlot.W) + Me.GetAutoAttackDamage(target) &&
                        target.IsValidTarget(Me.GetFullAttackRange(target) + 80))
                    {
                        if (ComboOption.GetList("ComboWSmartKS").Value == 0)
                        {
                            W.Cast();
                            W.Cast();
                            W.Cast();

                            if (HumanizerCardSelect.IsSelect && target.IsValidAutoRange() && Orbwalker.CanAttack())
                            {
                                Me.IssueOrder(OrderType.AttackUnit, target);
                            }
                        }
                        else
                        {
                            HumanizerCardSelect.StartSelecting(HumanizerCards.Blue);

                            if (HumanizerCardSelect.IsSelect && target.IsValidAutoRange() && Orbwalker.CanAttack())
                            {
                                Me.IssueOrder(OrderType.AttackUnit, target);
                            }
                        }
                    }
                    else
                    {
                        HumanizerCardSelect.StartSelecting(Me.Mana + W.GetBasicSpell().Cost >=
                                                           Q.GetBasicSpell().Cost + W.GetBasicSpell().Cost
                            ? HumanizerCards.Yellow
                            : HumanizerCards.Blue);
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                if (HarassOption.UseQ && Q.Ready)
                {
                    var target = HarassOption.GetTarget(Q.Range);

                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        var qPred = Q.GetPrediction(target);

                        if (qPred.HitChance >= HitChance.High)
                        {
                            Q.Cast(qPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Farm()
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
                if (LaneClearOption.UseQ && Q.Ready)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        var qFarm = Q.GetSpellFarmPosition(minions);

                        if (qFarm.HitCount >= LaneClearOption.GetSlider("LaneClearQCount").Value)
                        {
                            Q.Cast(qFarm.CastPosition);
                        }
                    }
                }

                if (LaneClearOption.UseW && W.Ready)
                {
                    var minions =
                        GameObjects.EnemyMinions.Where(
                            x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + 80) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        var wFarm = FarmPrediction.GetCircleFarmPosition(minions,
                            Me.AttackRange + Me.BoundingRadius + 80, 280);

                        if (LaneClearOption.GetBool("LaneClearWRed").Enabled && wFarm.HitCount >= 3)
                        {
                            var min = minions.FirstOrDefault(x => x.Distance(wFarm.CastPosition) <= 80);

                            if (min != null)
                            {
                                HumanizerCardSelect.StartSelecting(HumanizerCards.Red);

                                Orbwalker.ForceTarget(min);
                                LastForcusTime = Game.TickCount;
                            }
                        }
                        else if (LaneClearOption.GetBool("LaneClearWBlue").Enabled)
                        {
                            var min = minions.FirstOrDefault(x => x.Health < Me.GetSpellDamage(x, SpellSlot.W) + Me.GetAutoAttackDamage(x));

                            if (min != null && min.IsValidAutoRange())
                            {
                                HumanizerCardSelect.StartSelecting(HumanizerCards.Blue);

                                Orbwalker.ForceTarget(min);
                                LastForcusTime = Game.TickCount;
                            }
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (JungleClearOption.HasEnouguMana())
            {
                if (JungleClearOption.UseQ && Q.Ready)
                {
                    var mobs = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMob()).ToArray();

                    if (mobs.Any())
                    {
                        var qFarm = Q.GetSpellFarmPosition(mobs);

                        if (qFarm.HitCount >= 2 || mobs.Any(x => MobsName.Contains(x.UnitSkinName.ToLower())) && qFarm.HitCount >= 1)
                        {
                            Q.Cast(qFarm.CastPosition);
                        }
                    }
                }

                if (JungleClearOption.UseW && W.Ready)
                {
                    var Mob = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Me.AttackRange + Me.BoundingRadius + 80) && x.IsMob()).ToArray();

                    if (Mob.Length > 0)
                    {
                        if (Me.Mana >= W.GetBasicSpell().Cost * 2 + Q.GetBasicSpell().Cost * 2)
                        {
                            HumanizerCardSelect.StartSelecting(Mob.Length >= 2
                                ? HumanizerCards.Red
                                : HumanizerCards.Blue);
                        }
                    }
                }
            }
        }

        private static void PreAttack(object sender, PreAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0)
            {
                return;
            }

            if (Orbwalker.Mode == OrbwalkingMode.Combo &&
                ComboOption.GetBool("ComboDisableAA").Enabled &&
                Args.Target.Type == GameObjectType.obj_AI_Hero)
            {
                if (HumanizerCardSelect.Status == HumanizerSelectStatus.Selecting &&
                    Game.TickCount - HumanizerCardSelect.LastWSent > 300)
                {
                    Args.Cancel = true;
                }
            }
        }

        private static void OnGapcloser(Obj_AI_Hero target, GapcloserArgs Args)
        {
            if (target != null && target.IsValidTarget() && W.Ready)
            {
                if (W.Ready && target.IsValidTarget(W.Range))
                {
                    switch (Args.Type)
                    {
                        case SpellType.Melee:
                            if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100) && !Args.HaveShield)
                            {
                                HumanizerCardSelect.StartSelecting(HumanizerCards.Yellow);

                                if (Me.HasBuff("goldcardpreattack") && target.IsValidAutoRange())
                                {
                                    Me.IssueOrder(OrderType.AttackUnit, target);
                                }
                            }
                            break;
                        case SpellType.Dash:
                        case SpellType.SkillShot:
                        case SpellType.Targeted:
                            {
                                if (target.IsValidAutoRange() && !Args.HaveShield)
                                {
                                    HumanizerCardSelect.StartSelecting(HumanizerCards.Yellow);

                                    if (Me.HasBuff("goldcardpreattack") && target.IsValidAutoRange())
                                    {
                                        Me.IssueOrder(OrderType.AttackUnit, target);
                                    }
                                }
                            }
                            break;
                    }
                }
            }
        }

        private static void OnProcessSpellCast(Obj_AI_Base sender, Obj_AI_BaseMissileClientDataEventArgs args)
        {
            if (sender.IsMe)
            {
                if (args.SpellSlot == SpellSlot.R && args.SpellData.Name.Equals("Gate", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (MiscOption.GetBool("R", "UltYellow").Enabled)
                    {
                        if (W.Ready)
                        {
                            HumanizerCardSelect.StartSelecting(HumanizerCards.Yellow);
                        }
                    }
                }
            }
        }
    }

    internal enum HumanizerSelectStatus
    {
        Ready = 0,
        Selecting = 1,
        Selected = 2,
        Cooldown = 3
    }

    internal enum HumanizerCards
    {
        Red = 0,
        Yellow = 1,
        Blue = 2,
        None = 3
    }

    internal static class HumanizerCardSelect
    {
        internal static HumanizerCards Select;
        internal static int LastWSent;
        internal static Random random = new Random(Game.TickCount);

        internal static HumanizerSelectStatus Status { get; set; }

        static HumanizerCardSelect()
        {
            Game.OnUpdate += OnUpdate;
        }

        internal static void StartSelecting(HumanizerCards card)
        {
            if (ObjectManager.GetLocalPlayer().SpellBook.GetSpell(SpellSlot.W).Name == "PickACard" && Status == HumanizerSelectStatus.Ready)
            {
                Select = card;

                if (Game.TickCount - LastWSent > 170 + Game.Ping / 2)
                {
                    MyLogic.W.Cast();
                    LastWSent = Game.TickCount;
                }
            }
        }

        internal static bool IsSelect => ObjectManager.GetLocalPlayer().HasBuff("GoldCardPreAttack") ||
            ObjectManager.GetLocalPlayer().HasBuff("BlueCardPreAttack") ||
            ObjectManager.GetLocalPlayer().HasBuff("RedCardPreAttack");

        private static void OnUpdate()
        {
            if (ObjectManager.GetLocalPlayer().IsDead || ObjectManager.GetLocalPlayer().IsRecalling())
            {
                return;
            }

            var wName = MyLogic.W.GetBasicSpell().Name.ToLower();
            var wState = MyLogic.W.GetBasicSpell().State;

            if (wName != "pickacard" && ObjectManager.GetLocalPlayer().HasBuff("pickacard_tracker") && Game.TickCount - LastWSent > 0)
            {
                if (MiscOption.GetBool("CardSelect", "HumanizerSelect").Enabled &&
                    Game.TickCount - LastWSent <=
                    random.Next(MiscOption.GetSlider("CardSelect", "HumanizerSelectMin").Value,
                        MiscOption.GetSlider("CardSelect", "HumanizerSelectMax").Value))
                {
                    return;
                }

                if (Select == HumanizerCards.Blue &&
                    wName.Equals("BlueCardLock", StringComparison.InvariantCultureIgnoreCase))
                {
                    MyLogic.W.Cast();
                }
                else if (Select == HumanizerCards.Yellow &&
                    wName.Equals("GoldCardLock", StringComparison.InvariantCultureIgnoreCase))
                {
                    MyLogic.W.Cast();
                }
                else if (Select == HumanizerCards.Red && wName.
                    Equals("RedCardLock", StringComparison.InvariantCultureIgnoreCase))
                {
                    MyLogic.W.Cast();
                }
            }
            else
            {
                if (wState == SpellState.Ready)
                {
                    Status = HumanizerSelectStatus.Ready;
                }
                else if ((wState == SpellState.Cooldown || wState == SpellState.Disabled ||
                          wState == SpellState.NoMana || wState == SpellState.NotLearned ||
                          wState == SpellState.Surpressed || wState == SpellState.Unknown)
                         && !IsSelect)
                {
                    Status = HumanizerSelectStatus.Cooldown;
                }
                else if (IsSelect)
                {
                    Status = HumanizerSelectStatus.Selected;
                }
                else
                {
                    Status = HumanizerSelectStatus.Selecting;
                }
            }
        }
    }
}