using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Point = System.Windows.Point;

namespace Performance.Forms
{
    public partial class Visualiser : Form
    {
        public Point AtomPosition { get; set; } = new Point(0, 0);
        public List<List<Point>> CharacterShapes { get; set; } = new List<List<Point>>();

        public List<Point> Hull1 { get; set; } = new List<Point>();
        public List<Point> Hull2 { get; set; } = new List<Point>();
        public List<Point> Hull3 { get; set; } = new List<Point>();

        public Visualiser()
        {
            InitializeComponent();
        }

        private void Plot_Load(object sender, EventArgs e)
        {
            DoubleBuffered = true;
        }

        private void Plot_Paint(object sender, PaintEventArgs e)
        {
            if (CharacterShapes.Any())
            {
                foreach (List<Point> points in CharacterShapes)
                {
                    if (points.Any())
                    {
                        PointF[] pts = points.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();
                        using (Pen pen = new Pen(Color.Red, 2)) { e.Graphics.DrawPolygon(pen, pts); }
                    }
                }
            }

            if (Hull1.Any())
            {
                PointF[] pts = Hull1.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();
                using (Pen pen = new Pen(Color.Green, 2)) { e.Graphics.DrawPolygon(pen, pts); }
            }

            if (Hull2.Any())
            {
                PointF[] pts = Hull2.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();
                using (Pen pen = new Pen(Color.Blue, 2)) { e.Graphics.DrawPolygon(pen, pts); }
            }

            if (Hull3.Any())
            {
                PointF[] pts = Hull3.Select(p => new PointF((float)p.X, (float)p.Y)).ToArray();
                using (Pen pen = new Pen(Color.Orange, 2)) { e.Graphics.DrawPolygon(pen, pts); }
            }

            Rectangle r = new Rectangle((int)AtomPosition.X -5, (int)AtomPosition.Y - 5, 10, 10);
            e.Graphics.FillEllipse(new SolidBrush(Color.Chartreuse), r);
        }
    }
}
