namespace Flowers_Vladimir.MyCommon
{
    #region

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Util.Cache;

    using Flowers_Vladimir.MyBase;

    using System;
    using System.Linq;

    using Color = System.Drawing.Color;

    #endregion

    internal class MyDamageIndicator // made by detuks
    {
        private const int XOffset = 10;
        private static int YOffset = 18;
        private const int Width = 103;
        private const int Height = 9;

        private static readonly Color Color = Color.Lime;
        private static readonly Color FillColor = Color.Goldenrod;

        public static void OnDamageIndicator()
        {
            Render.OnPresent += delegate
            {
                if (ObjectManager.GetLocalPlayer().IsDead || !MyLogic.DrawMenu["FlowersVladimir.DrawMenu.ComboDamage"].Enabled)
                {
                    return;
                }

                foreach (var target in GameObjects.EnemyHeroes.Where(h => h.IsValid && h.IsFloatingHealthBarActive))
                {
                    Vector2 pos;
                    Render.WorldToScreen(target.ServerPosition, out pos);

                    if (!Render.IsPointInScreen(pos))
                    {
                        return;
                    }

                    if (target.IsMelee)
                    {
                        YOffset = 12;
                    }
                    else if (target.ChampionName == "Annie" || target.ChampionName == "Jhin")
                    {
                        YOffset = 5;
                    }
                    else
                    {
                        YOffset = 18;
                    }

                    var damage = MyExtraManager.GetComboDamage(target);

                    if (damage > 2)
                    {
                        var barPos = target.FloatingHealthBarPosition;
                        var percentHealthAfterDamage = Math.Max(0, target.Health - damage) / target.MaxHealth;
                        var yPos = barPos.Y + YOffset;
                        var xPosDamage = barPos.X + XOffset + Width * percentHealthAfterDamage;
                        var xPosCurrentHp = barPos.X + XOffset + Width * target.Health / target.MaxHealth;

                        if (damage > target.Health)
                        {
                            var X = (int)barPos.X + XOffset;
                            var Y = (int)barPos.Y + YOffset - 15;
                            var text = "KILLABLE: " + (target.Health - damage);
                            Render.Text(X, Y, Color.Red, text);
                        }

                        Render.Line(xPosDamage, yPos, xPosDamage, yPos + Height, 5, true, Color);

                        if (MyLogic.DrawMenu["FlowersVladimir.DrawMenu.FillDamage"].Enabled)
                        {
                            var differenceInHp = xPosCurrentHp - xPosDamage;
                            var pos1 = barPos.X + 9 + 107 * percentHealthAfterDamage;

                            for (var i = 0; i < differenceInHp; i++)
                            {
                                Render.Line(pos1 + i, yPos, pos1 + i, yPos + Height, 5, true, FillColor);
                            }
                        }
                    }
                }
            };
        }
    }
}
