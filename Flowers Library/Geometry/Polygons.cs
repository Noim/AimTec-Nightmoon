// <copyright file="MyPolygon.cs" company="LeagueSharp">
//    Copyright (c) 2015 LeagueSharp.
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see http://www.gnu.org/licenses/
// </copyright>

//https://github.com/LeagueSharp-Devs/LeagueSharp.SDKEx/blob/master/Core/Math/Polygons

namespace Flowers_Library
{
    #region

    using Aimtec;
    using Aimtec.SDK.Util.ThirdParty;
    using Aimtec.SDK.Extensions;

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    #endregion

    public class MyPolygon
    {
        public List<Vector2> Points { get; set; } = new List<Vector2>();

        public void Add(Vector2 point)
        {
            this.Points.Add(point);
        }

        public void Add(Vector3 point)
        {
            this.Points.Add(point.To2D());
        }

        public void Add(MyPolygon polygon)
        {
            foreach (var point in polygon.Points)
            {
                this.Points.Add(point);
            }
        }

        public virtual void Draw(Color color, int width = 1)
        {
            for (var i = 0; i <= this.Points.Count - 1; i++)
            {
                var nextIndex = (this.Points.Count - 1 == i) ? 0 : (i + 1);
                var playerPositionZ = ObjectManager.GetLocalPlayer().ServerPosition.Z;
                var from = Vector2.Zero;
                var fromGet = Render.WorldToScreen(To3DY(this.Points[i], playerPositionZ), out from);
                var to = Vector2.Zero;
                var toGet = Render.WorldToScreen(To3DY(this.Points[nextIndex], playerPositionZ), out to);

                Render.Line(from.X, from.Y, to.X, to.Y, width, true, color);
            }
        }

        public Vector3 To3DY(Vector2 v, float z)
        {
            return new Vector3(v.X, v.Y, z);
        }

        public bool IsInside(Vector2 point)
        {
            return !this.IsOutside(point);
        }

        public bool IsInside(Vector3 point)
        {
            return !this.IsOutside(point.To2D());
        }

        public bool IsInside(GameObject gameObject)
        {
            return !this.IsOutside(gameObject.Position.To2D());
        }

        public bool IsOutside(Vector2 point)
        {
            var p = new IntPoint(point.X, point.Y);
            return Clipper.PointInPolygon(p, this.ToClipperPath()) != 1;
        }

        public List<IntPoint> ToClipperPath()
        {
            var result = new List<IntPoint>(this.Points.Count);
            result.AddRange(this.Points.Select(point => new IntPoint(point.X, point.Y)));
            return result;
        }

        public class Rectangle : MyPolygon
        {
            public Rectangle(Vector3 start, Vector3 end, float width)
                : this(start.To2D(), end.To2D(), width)
            { }

            public Rectangle(Vector2 start, Vector2 end, float width)
            {
                this.Start = start;
                this.End = end;
                this.Width = width;

                this.UpdatePolygon();
            }

            public Vector2 Direction => (this.End - this.Start).Normalized();

            public Vector2 End { get; set; }

            public Vector2 Perpendicular => this.Direction.Perpendicular();

            public Vector2 Start { get; set; }

            public float Width { get; set; }

            public void UpdatePolygon(int offset = 0, float overrideWidth = -1)
            {
                this.Points.Clear();
                this.Points.Add(
                    this.Start + ((overrideWidth > 0 ? overrideWidth : this.Width + offset) * this.Perpendicular)
                    - (offset * this.Direction));
                this.Points.Add(
                    this.Start - ((overrideWidth > 0 ? overrideWidth : this.Width + offset) * this.Perpendicular)
                    - (offset * this.Direction));
                this.Points.Add(
                    this.End - ((overrideWidth > 0 ? overrideWidth : this.Width + offset) * this.Perpendicular)
                    + (offset * this.Direction));
                this.Points.Add(
                    this.End + ((overrideWidth > 0 ? overrideWidth : this.Width + offset) * this.Perpendicular)
                    + (offset * this.Direction));
            }
        }

        public class Line : MyPolygon
        {
            public Line(Vector3 start, Vector3 end, float length = -1)
                : this(start.To2D(), end.To2D(), length)
            { }

            public Line(Vector2 start, Vector2 end, float length = -1)
            {
                this.LineStart = start;
                this.LineEnd = end;

                if (length > 0)
                {
                    this.Length = length;
                }

                this.UpdatePolygon();
            }

            public float Length
            {
                get
                {
                    return this.LineStart.Distance(this.LineEnd);
                }

                set
                {
                    this.LineEnd = (this.LineEnd - this.LineStart).Normalized() * value + this.LineStart;
                }
            }

            public Vector2 LineEnd { get; set; }

            public Vector2 LineStart { get; set; }

            public void UpdatePolygon()
            {
                this.Points.Clear();
                this.Points.Add(this.LineStart);
                this.Points.Add(this.LineEnd);
            }
        }

        public class Circle : MyPolygon
        {
            private readonly int quality;

            public Circle(Vector3 center, float radius, int quality = 20)
                : this(center.To2D(), radius, quality)
            { }

            public Circle(Vector2 center, float radius, int quality = 20)
            {
                this.Center = center;
                this.Radius = radius;
                this.quality = quality;

                this.UpdatePolygon();
            }

            public Vector2 Center { get; set; }

            public float Radius { get; set; }

            public void UpdatePolygon(int offset = 0, float overrideWidth = -1)
            {
                this.Points.Clear();

                var outRadius = overrideWidth > 0
                                    ? overrideWidth
                                    : (offset + this.Radius) / (float)Math.Cos(2 * Math.PI / this.quality);

                for (var i = 1; i <= this.quality; i++)
                {
                    var angle = i * 2 * Math.PI / this.quality;
                    var point = new Vector2(
                        this.Center.X + (outRadius * (float)Math.Cos(angle)),
                        this.Center.Y + (outRadius * (float)Math.Sin(angle)));

                    this.Points.Add(point);
                }
            }
        }

        public class Sector : MyPolygon
        {
            private readonly int quality;

            public Sector(Vector3 center, Vector3 direction, float angle, float radius, int quality = 20)
                : this(center.To2D(), direction.To2D(), angle, radius, quality)
            { }

            public Sector(Vector2 center, Vector2 endPosition, float angle, float radius, int quality = 20)
            {
                this.Center = center;
                this.Direction = (endPosition - center).Normalized();
                this.Angle = angle;
                this.Radius = radius;
                this.quality = quality;

                this.UpdatePolygon();
            }

            public float Angle { get; set; }

            public Vector2 Center { get; set; }

            public Vector2 Direction { get; set; }

            public float Radius { get; set; }

            public void UpdatePolygon(int offset = 0)
            {
                this.Points.Clear();

                var outRadius = (this.Radius + offset) / (float)Math.Cos(2 * Math.PI / this.quality);
                this.Points.Add(this.Center);
                var side1 = this.Direction.Rotated(-this.Angle * 0.5f);

                for (var i = 0; i <= this.quality; i++)
                {
                    var cDirection = side1.Rotated(i * this.Angle / this.quality).Normalized();
                    this.Points.Add(
                        new Vector2(this.Center.X + (outRadius * cDirection.X), this.Center.Y + (outRadius * cDirection.Y)));
                }
            }
        }

        public class Arc : MyPolygon
        {
            private readonly int quality;

            public Arc(Vector3 start, Vector3 direction, float angle, float radius, int quality = 20)
                : this(start.To2D(), direction.To2D(), angle, radius, quality)
            { }


            public Arc(Vector2 start, Vector2 end, float angle, float radius, int quality = 20)
            {
                this.StartPos = start;
                this.EndPos = (end - start).Normalized();
                this.Angle = angle;
                this.Radius = radius;
                this.quality = quality;

                this.UpdatePolygon();
            }

            public float Angle { get; set; }

            public Vector2 EndPos { get; set; }

            public float Radius { get; set; }

            public Vector2 StartPos { get; set; }

            public void UpdatePolygon(int offset = 0)
            {
                this.Points.Clear();

                var outRadius = (this.Radius + offset) / (float)Math.Cos(2 * Math.PI / this.quality);
                var side1 = this.EndPos.Rotated(-this.Angle * 0.5f);

                for (var i = 0; i <= this.quality; i++)
                {
                    var cDirection = side1.Rotated(i * this.Angle / this.quality).Normalized();
                    this.Points.Add(
                        new Vector2(
                            this.StartPos.X + (outRadius * cDirection.X),
                            this.StartPos.Y + (outRadius * cDirection.Y)));
                }
            }
        }

        public class Ring : MyPolygon
        {
            private readonly int quality;

            public Ring(Vector3 center, float width, float outerRadius, int quality = 20)
                : this(center.To2D(), width, outerRadius, quality)
            { }

            public Ring(Vector2 center, float width, float outerRadius, int quality = 20)
            {
                this.Center = center;
                this.Width = width;
                this.OuterRadius = outerRadius;
                this.quality = quality;

                this.UpdatePolygon();
            }

            public Vector2 Center { get; set; }

            public float OuterRadius { get; set; }

            public float Width { get; set; }

            public void UpdatePolygon(int offset = 0)
            {
                this.Points.Clear();

                var outRadius = (offset + this.Width + this.OuterRadius) / (float)Math.Cos(2 * Math.PI / this.quality);
                var innerRadius = this.Width - this.OuterRadius - offset;

                for (var i = 0; i <= this.quality; i++)
                {
                    var angle = i * 2 * Math.PI / this.quality;
                    var point = new Vector2(
                        this.Center.X - (outRadius * (float)Math.Cos(angle)),
                        this.Center.Y - (outRadius * (float)Math.Sin(angle)));

                    this.Points.Add(point);
                }

                for (var i = 0; i <= this.quality; i++)
                {
                    var angle = i * 2 * Math.PI / this.quality;
                    var point = new Vector2(
                        this.Center.X + (innerRadius * (float)Math.Cos(angle)),
                        this.Center.Y - (innerRadius * (float)Math.Sin(angle)));

                    this.Points.Add(point);
                }
            }
        }
    }
}
