namespace SharpShooter.MyPlugin
{
    #region

    using Aimtec;
    using Aimtec.SDK.Damage;
    using Aimtec.SDK.Damage.JSON;
    using Aimtec.SDK.Extensions;
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

    internal class Graves : MyLogic
    {
        public Graves()
        {
            Initializer();
        }

        private static void Initializer()
        {
            Q = new Aimtec.SDK.Spell(SpellSlot.Q, 800f);
            Q.SetSkillshot(0.25f, 40f, 3000f, false, SkillshotType.Line);

            W = new Aimtec.SDK.Spell(SpellSlot.W, 900f);
            W.SetSkillshot(0.25f, 250f, 1000f, false, SkillshotType.Circle);

            E = new Aimtec.SDK.Spell(SpellSlot.E, 425f);

            R = new Aimtec.SDK.Spell(SpellSlot.R, 1050f);
            R.SetSkillshot(0.25f, 100f, 2100f, false, SkillshotType.Line);

            ComboOption.AddMenu();
            ComboOption.AddQ();
            ComboOption.AddW();
            ComboOption.AddE();
            ComboOption.AddBool("ComboEReset", "Use E|Reset Attack");
            ComboOption.AddBool("ComboECheck", "Use E|Check Safe");
            ComboOption.AddSliderBool("ComboRCount", "Use R| When Min Hit Count >= x", 4, 1, 5);

            HarassOption.AddMenu();
            HarassOption.AddQ();
            HarassOption.AddMana();
            HarassOption.AddTargetList();

            LaneClearOption.AddMenu();
            LaneClearOption.AddSliderBool("LaneClearQCount", "Use Q| Min Hit Count >= x", 3, 1, 5, true);
            LaneClearOption.AddE();
            LaneClearOption.AddMana();

            JungleClearOption.AddMenu();
            JungleClearOption.AddQ();
            JungleClearOption.AddW();
            JungleClearOption.AddE();
            JungleClearOption.AddMana();

            KillStealOption.AddMenu();
            KillStealOption.AddQ();
            KillStealOption.AddW();
            KillStealOption.AddR();
            KillStealOption.AddTargetList();

            GapcloserOption.AddMenu();

            MiscOption.AddMenu();
            MiscOption.AddBasic();

            DrawOption.AddMenu();
            DrawOption.AddQ(Q);
            DrawOption.AddW(W);
            DrawOption.AddE(E);
            DrawOption.AddR(R);
            DrawOption.AddFarm();
            DrawOption.AddDamageIndicatorToHero(true, true, false, true, true);

            Game.OnUpdate += OnUpdate;
            Orbwalker.PostAttack += PostAttack;
            Gapcloser.OnGapcloser += OnGapcloser;
            Obj_AI_Base.OnIssueOrder += OnIssueOrder;
            SpellBook.OnCastSpell += OnCastSpell;
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
            if (KillStealOption.UseQ && Q.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(Q.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.Q)))
                {
                    if (target.IsValidTarget(Q.Range) && !target.IsUnKillable())
                    {
                        CastQ(target);
                    }
                }
            }

            if (KillStealOption.UseW && W.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(W.Range) && x.Health < Me.GetSpellDamage(x, SpellSlot.W)))
                {
                    if (target.IsValidTarget(W.Range) && !target.IsUnKillable())
                    {
                        var wPred = W.GetPrediction(target);

                        if (wPred.HitChance >= HitChance.High)
                        {
                            W.Cast(wPred.UnitPosition);
                        }
                    }
                }
            }


            if (KillStealOption.UseR && R.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x => x.IsValidTarget(R.Range) &&
                             KillStealOption.GetKillStealTarget(x.ChampionName.ToLower()) &&
                             x.Health < Me.GetSpellDamage(x, SpellSlot.R) + Me.GetSpellDamage(x, SpellSlot.R, DamageStage.AreaOfEffect) &&
                             x.DistanceToPlayer() >
                             Me.AttackRange + Me.BoundingRadius + Me.BoundingRadius + 30))
                {
                    if (target.IsValidTarget(R.Range) && !target.IsUnKillable())
                    {
                        var rPred = R.GetPrediction(target);

                        if (rPred.HitChance >= HitChance.High)
                        {
                            R.Cast(rPred.UnitPosition);
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(R.Range);

            if (target.IsValidTarget(R.Range))
            {
                if (ComboOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                {
                    CastQ(target);
                }

                if (ComboOption.UseE && E.Ready && target.IsValidTarget(800f) &&
                    !target.IsValidTarget(Me.AttackRange + Me.BoundingRadius + target.BoundingRadius + 50))
                {
                    ELogic(target);
                }

                if (ComboOption.UseW && W.Ready && target.IsValidTarget(W.Range) &&
                    (target.DistanceToPlayer() <= target.AttackRange + 70 ||
                     target.DistanceToPlayer() >= Me.AttackRange + Me.BoundingRadius - target.BoundingRadius + 15 + 80))
                {
                    var wPred = W.GetPrediction(target);

                    if (wPred.HitChance >= HitChance.High)
                    {
                        W.Cast(wPred.UnitPosition);
                    }
                }

                if (ComboOption.GetSliderBool("ComboRCount").Enabled && R.Ready && target.IsValidTarget(R.Range))
                {
                    var rPred = R.GetPrediction(target);

                    if (rPred.HitChance >= HitChance.Medium)
                    {
                        if (rPred.AoeTargetsHitCount >= ComboOption.GetSliderBool("ComboRCount").Value)
                        {
                            R.Cast(rPred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            if (HarassOption.HasEnouguMana())
            {
                var target = HarassOption.GetTarget(Q.Range);

                if (HarassOption.UseQ && Q.Ready && target.IsValidTarget(Q.Range))
                {
                    CastQ(target);
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
                if (LaneClearOption.GetSliderBool("LaneClearQCount").Enabled && Q.Ready)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion()).ToArray();

                    if (minions.Any())
                    {
                        var qFarm = Q.GetSpellFarmPosition(minions);

                        if (qFarm.HitCount >= LaneClearOption.GetSliderBool("LaneClearQCount").Value)
                        {
                            Q.Cast(qFarm.CastPosition);
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

                        if (qFarm.HitCount >= 1)
                        {
                            Q.Cast(qFarm.CastPosition);
                        }
                    }
                }
            }
        }

        private static void PostAttack(object sender, PostAttackEventArgs Args)
        {
            if (Args.Target == null || Args.Target.IsDead || !Args.Target.IsValidTarget() || Args.Target.Health <= 0 ||
                !E.Ready)
            {
                return;
            }

            switch (Args.Target.Type)
            {
                case GameObjectType.obj_AI_Hero:
                    {
                        if (Orbwalker.Mode == OrbwalkingMode.Combo)
                        {
                            if (ComboOption.UseE && ComboOption.GetBool("ComboEReset").Enabled)
                            {
                                var target = Args.Target as Obj_AI_Hero;

                                if (target != null && !target.IsDead && target.IsValidTarget())
                                {
                                    ELogic(target);
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
                                if (JungleClearOption.HasEnouguMana() && JungleClearOption.UseE)
                                {
                                    var mobs =
                                        GameObjects.EnemyMinions.Where(x => x.IsValidSpellTarget(800) && x.IsMob())
                                            .Where(x => !x.Name.ToLower().Contains("mini"))
                                            .ToArray();

                                    if (mobs.Any() && mobs.FirstOrDefault() != null)
                                    {
                                        ELogic(mobs.FirstOrDefault());
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
            if (W.Ready && target != null && target.IsValidTarget(W.Range))
            {
                switch (Args.Type)
                {
                    case SpellType.Melee:
                        if (target.IsValidTarget(target.AttackRange + target.BoundingRadius + 100))
                        {
                            var wPred = W.GetPrediction(target);
                            W.Cast(wPred.UnitPosition);
                        }
                        break;
                    case SpellType.Dash:
                    case SpellType.SkillShot:
                    case SpellType.Targeted:
                        {
                            var wPred = W.GetPrediction(target);
                            W.Cast(wPred.UnitPosition);
                        }
                        break;
                }
            }
        }

        private static void OnIssueOrder(Obj_AI_Base sender, Obj_AI_BaseIssueOrderEventArgs/*Obj_AI_BaseMissileClientDataEventArgs*/ Args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (Args.OrderType != OrderType.AttackUnit || !E.Ready)
            {
                return;
            }

            if (Orbwalker.Mode != OrbwalkingMode.Combo && Orbwalker.Mode != OrbwalkingMode.Laneclear)
            {
                return;
            }

            var target = (AttackableUnit)Args.Target;

            if (target == null || !target.IsValidTarget())
            {
                return;
            }

            if (!Orbwalker.CanAttack() || !target.IsValidTarget(Me.AttackRange + Me.BoundingRadius + target.BoundingRadius - 20))
            {
                Args.ProcessEvent = false;
                return;
            }

            if (Orbwalker.Mode == OrbwalkingMode.Combo && ComboOption.UseE && 
                ComboOption.GetBool("ComboEReset").Enabled && target.Type == GameObjectType.obj_AI_Hero)
            {
                if(ELogic((Obj_AI_Hero)target))
                {
                    DelayAction.Queue(1, () =>
                    {
                        //E.Cast(Me.ServerPosition.Extend(Args.Target.ServerPosition, E.Range - Args.Target.BoundingRadius));
                        Orbwalker.ResetAutoAttackTimer();
                        //Me.IssueOrder(OrderType.AttackUnit, target);
                    });
                }
            }
            else if (Orbwalker.Mode == OrbwalkingMode.Laneclear && JungleClearOption.HasEnouguMana() && 
                JungleClearOption.UseE && target.IsMob())
            {
                if (ELogic((Obj_AI_Minion)target))
                {
                    DelayAction.Queue(1, () =>
                    {
                        // E.Cast(Me.ServerPosition.Extend(Args.Target.ServerPosition, E.Range - Args.Target.BoundingRadius));
                        Orbwalker.ResetAutoAttackTimer();
                        //Me.IssueOrder(OrderType.AttackUnit, target);
                    });
                }
            }
        }

        private static void OnCastSpell(Obj_AI_Base sender, SpellBookCastSpellEventArgs Args)
        {
            if (sender.IsMe && Args.Slot == SpellSlot.E && Orbwalker.Mode != OrbwalkingMode.None)
            {
                Orbwalker.ResetAutoAttackTimer();
            }
        }

        private static bool ELogic(Obj_AI_Base target)
        {
            if (!E.Ready)
            {
                return false;
            }

            var ePosition = Me.ServerPosition.Extend(Game.CursorPos, E.Range);

            if (ePosition.PointUnderEnemyTurret() && Me.HealthPercent() <= 50)
            {
                return false;
            }

            if (ComboOption.GetBool("ComboECheck").Enabled && Orbwalker.Mode == OrbwalkingMode.Combo)
            {
                if (GameObjects.EnemyHeroes.Count(x => !x.IsDead && x.Distance(ePosition) <= 550) >= 3)
                {
                    return false;
                }

                //Catilyn W
                if (ObjectManager
                        .Get<GameObject>()
                        .FirstOrDefault(
                            x =>
                                x != null && x.IsValid &&
                                x.Name.ToLower().Contains("yordletrap_idle_red.troy") &&
                                x.Position.Distance(ePosition) <= 100) != null)
                {
                    return false;
                }

                //Jinx E
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid && x.IsEnemy && x.Name == "k" &&
                                             x.Position.Distance(ePosition) <= 100) != null)
                {
                    return false;
                }

                //Teemo R
                if (ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(x => x.IsValid && x.IsEnemy && x.Name == "Noxious Trap" &&
                                             x.Position.Distance(ePosition) <= 100) != null)
                {
                    return false;
                }
            }

            if (target.Distance(ePosition) > Me.AttackRange + Me.BoundingRadius + target.BoundingRadius + 15)
            {
                return false;
            }

            if (target.Health < Me.GetAutoAttackDamage(target) * 2 &&
                target.Distance(Me.ServerPosition.Extend(Game.CursorPos, E.Range)) <= Me.AttackRange + Me.BoundingRadius + target.BoundingRadius - 20)
            {
                return E.Cast(Me.ServerPosition.Extend(Game.CursorPos, E.Range));
            }
            else if (!Me.HasBuff("gravesbasicattackammo2") && Me.HasBuff("gravesbasicattackammo1") &&
                     target.Distance(Me.ServerPosition.Extend(Game.CursorPos, E.Range)) <= Me.AttackRange + Me.BoundingRadius + target.BoundingRadius - 20)
            {
                return E.Cast(Me.ServerPosition.Extend(Game.CursorPos, E.Range));
            }
            else if (!Me.HasBuff("gravesbasicattackammo2") && !Me.HasBuff("gravesbasicattackammo1") &&
                     target.IsValidTarget(Me.AttackRange + Me.BoundingRadius + target.BoundingRadius - 20))
            {
                return E.Cast(Me.ServerPosition.Extend(Game.CursorPos, E.Range));
            }
            return false;
        }

        private static void CastQ(Obj_AI_Base target)
        {
            if (target == null || !target.IsValidTarget(Q.Range))
            {
                return;
            }

            var from = Me.ServerPosition.To2D();
            var to = target.ServerPosition.To2D();
            var direction = (from - to).Normalized();
            var distance = from.Distance(to);

            for (var d = 0; d < distance; d = d + 20)
            {
                var point = from + d * direction;
                var flags = NavMesh.WorldToCell(point.To3D()).Flags;

                if (flags.HasFlag(NavCellFlags.Building) || flags.HasFlag(NavCellFlags.Wall))
                {
                    return;
                }
            }

            var qPred = Q.GetPrediction(target);
            if (qPred.HitChance >= HitChance.High)
            {
                Q.Cast(qPred.UnitPosition);
            }
        }
    }
}