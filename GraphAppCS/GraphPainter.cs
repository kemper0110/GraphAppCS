using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;

namespace GraphAppCS
{

    using VertexMap = System.Collections.Generic.Dictionary<int, int>;
    using ArcMap = System.Collections.Generic.Dictionary<(int, int), int>;
    using Graph = System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>;
    internal class GraphPainter
    {
        static double vertex_circle_radius = 20;
        
        public static void Paint(Canvas canvas, Graph graph, ArcMap limits)
        {
            canvas.Children.Clear();
            Point offset = new(50, 50);

            double radius = Math.Min(canvas.Width - 2 * offset.X, canvas.Height - 2 * offset.Y) / 2;

            var vertex_table = generateVertexView(canvas, graph, radius);

            var fromCenter = (Point p) => new Point(p.X + radius + offset.X, p.Y + radius + offset.Y);

            foreach (var (vertex, adj_set) in graph)
            {
                foreach(var adj in adj_set)
                {
                    var (p1, p2) = (fromCenter(vertex_table[vertex]), fromCenter(vertex_table[adj]));
                    AddEdge(canvas, p1, p2);
                    
                    AddEdgeName(canvas, p1, p2, limits[(vertex, adj)].ToString());
                }
            }

            foreach (var vertex in graph.Keys) {
                var pos = fromCenter(vertex_table[vertex]);
                AddVertex(canvas, pos, vertex);
                AddVertexName(canvas, pos, vertex);
            }
        }
        public static void Paint(Canvas canvas, PreFlowPush flow)
        {
            canvas.Children.Clear();
            var graph = flow.graph;

            Point offset = new(50, 50);

            double radius = Math.Min(canvas.Width - 2 * offset.X, canvas.Height - 2 * offset.Y) / 2;

            var vertex_table = generateVertexView(canvas, graph, radius);

            var fromCenter = (Point p) => new Point(p.X + radius + offset.X, p.Y + radius + offset.Y);


            foreach (var (vertex, adj_set) in graph)
            {
                foreach (var adj in adj_set)
                {
                    var (p1, p2) = (fromCenter(vertex_table[vertex]), fromCenter(vertex_table[adj]));
                    AddEdge(canvas, p1, p2);

                    var name = flow.f[(vertex, adj)] + "/" + flow.c[(vertex, adj)].ToString();
                    AddEdgeName(canvas, p1, p2, name);
                }
            }

            foreach (var vertex in graph.Keys)
            {
                var pos = fromCenter(vertex_table[vertex]);
                AddVertex(canvas, pos, vertex);
                AddVertexName(canvas, pos, vertex);
            }

            

            TextBlock flow_text = new();
            flow_text.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray);
            flow_text.Text = flow.e[flow.dst].ToString();
            canvas.Children.Add(flow_text);
        }
        private static void AddEdgeName(Canvas canvas, Point p1, Point p2, string name)
        {
            TextBlock text = new();
            text.FontSize = 15;
            text.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray);
            text.Text = name;
            canvas.Children.Add(text);


            var dv = new Point(p2.X - p1.X, p2.Y - p1.Y);
            var dv_length = Math.Sqrt(dv.X * dv.X + dv.Y * dv.Y);
            var dv_normalized = new Point(dv.X / dv_length, dv.Y / dv_length);

            var normal = new Point(dv.Y, -dv.X);
            var normal_length = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y);
            var normal_normalized = new Point(normal.X / normal_length, normal.Y / normal_length);

            const double text_offset = 20;

            text.Measure(new Size(100, 100));
            var bounds = text.DesiredSize;

            const double step_back = 45;

            var pos = new Point(p1.X + (p2.X - p1.X) / 2 - step_back * dv_normalized.X, p1.Y + (p2.Y - p1.Y) / 2 - step_back * dv_normalized.Y);
            text.SetValue(Canvas.LeftProperty, pos.X - bounds.Width / 2 - normal_normalized.X * text_offset);
            text.SetValue(Canvas.TopProperty, pos.Y - bounds.Height / 2 - normal_normalized.Y * text_offset);
            text.SetValue(Canvas.ZIndexProperty, 5);
        }
        private static void AddEdge(Canvas canvas, Point p1, Point p2)
        {
            var line = new Line();
            line.Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            line.StrokeThickness = 7;
            line.X1 = p1.X; line.Y1 = p1.Y;
            line.X2 = p2.X; line.Y2 = p2.Y;

            canvas.Children.Add(line);

            var dv = new Point(p2.X - p1.X, p2.Y - p1.Y);
            var length = Math.Sqrt(dv.X * dv.X + dv.Y * dv.Y);
            var normalized = new Point(dv.X / length, dv.Y / length);


            var arrow = new Line();
            arrow.Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            arrow.StrokeThickness = 16;
            arrow.StrokeEndLineCap = PenLineCap.Triangle;

            const double arrow_length = 5;
            const double radius_ratio = 1.25;

            arrow.X1 = p2.X - normalized.X * (arrow_length + vertex_circle_radius * radius_ratio);
            arrow.X2 = p2.X - normalized.X * vertex_circle_radius * radius_ratio;
            arrow.Y1 = p2.Y - normalized.Y * (arrow_length + vertex_circle_radius * radius_ratio);
            arrow.Y2 = p2.Y - normalized.Y * vertex_circle_radius * radius_ratio;

            canvas.Children.Add(arrow);
        }
        private static void AddVertex(Canvas canvas, Point pos, int vertex)
        {
            var ellipse = new Ellipse();
            ellipse.Height = ellipse.Width = vertex_circle_radius * 2;
            ellipse.Fill = new SolidColorBrush(Microsoft.UI.Colors.LightSlateGray);
            canvas.Children.Add(ellipse);

            ellipse.SetValue(Canvas.LeftProperty, pos.X - vertex_circle_radius);
            ellipse.SetValue(Canvas.TopProperty, pos.Y - vertex_circle_radius);
        }
        private static void AddVertexName(Canvas canvas, Point pos, int vertex)
        {
            TextBlock text = new();
            text.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
            text.Text = vertex.ToString();
            canvas.Children.Add(text);
            text.Measure(new Size(100, 100));
            text.SetValue(Canvas.LeftProperty, pos.X - text.ActualWidth / 2);
            text.SetValue(Canvas.TopProperty, pos.Y - text.ActualWidth);
            text.SetValue(Canvas.ZIndexProperty, 5);
        }
        private static Dictionary<int, Point> generateVertexView(Canvas canvas, Graph graph, double radius)
        {
            Dictionary<int, Point> points = new Dictionary<int, Point>();
            foreach (var (key, value) in graph.Keys.Zip(generateCirclePoints(graph.Count, radius)))
                points.Add(key, value);
            return points;
        }

        private static IEnumerable<Point> generateCirclePoints(int n, double r)
        {
            
            for(int i = 0; i < n; ++i)
            {
                var angle = 2 * Math.PI * i / n;
                var x = r * Math.Cos(angle);
                var y = r * Math.Sin(angle);
                yield return new Point(x, -y);
            }
        }
    }
}
