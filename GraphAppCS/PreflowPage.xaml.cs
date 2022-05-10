using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GraphAppCS
{
    using VertexMap = System.Collections.Generic.Dictionary<int, int>;
    using ArcMap = System.Collections.Generic.Dictionary<(int, int), int>;
    using Graph = System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>;
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PreflowPage : Page
    {
        Graph graph = new();
        ArcMap limits = new();
        int src = 0, dst = 0;

        public PreflowPage()
        {
            this.NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
            this.InitializeComponent();
        }

        private void MainPageButton_Click(object sender, RoutedEventArgs e)
        {
            PreFlowPushFrame.Navigate(typeof(MainPage), null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
        }

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(SharedData.window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add(".json");

            var file = await picker.PickSingleFileAsync();
            if (file is null)
                return;
            var path = file.Path;

            graph.Clear();
            limits.Clear();

            // parsing
            var input = File.ReadAllText(path);
            var js = Newtonsoft.Json.Linq.JObject.Parse(input);

            var js_graph = js["graph"];
            foreach (var node in js_graph.ToObject<Dictionary<string, HashSet<int>>>())
                graph.Add(int.Parse(node.Key), node.Value);

            var js_arc_map = js["arc_map"];
            foreach (var node in js_arc_map)
            {
                var ar = node.First.ToObject<int[]>();
                var edge = (ar[0], ar[1]);
                var w = ((int)node.Last);
                limits.Add(edge, w);
            }
            foreach (var v in graph.Keys)
                foreach (var u in graph.Keys)
                    if (!limits.ContainsKey((v, u)))
                        limits.Add((v, u), 0);

            GraphPainter.Paint(canvas, graph, limits);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker picker = new FileSavePicker();
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(SharedData.window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeChoices.Add(KeyValuePair.Create("json file only", (IList<string>)new List<string>() { ".json" }));
            var file = await picker.PickSaveFileAsync();
            if (file is null)
                return;

            var js = new Newtonsoft.Json.Linq.JObject();

            var graph_object = new Newtonsoft.Json.Linq.JObject();
            foreach (var (key, value) in graph)
                graph_object.Add(key.ToString(), new Newtonsoft.Json.Linq.JArray(value.ToArray()));
            js.Add("graph", graph_object);

            var arc_map_array = new Newtonsoft.Json.Linq.JArray();
            foreach (var ((v1, v2), value) in limits)
                arc_map_array.Add(new Newtonsoft.Json.Linq.JArray()
                {
                    new Newtonsoft.Json.Linq.JArray() { v1, v2 }, value
                });
            js.Add("arc_map", arc_map_array);

            var js_file = File.CreateText(file.Path);
            js_file.Write(js.ToString(Newtonsoft.Json.Formatting.Indented));
            js_file.Close();
        }
        private async void ShowErrorBox(string message)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Еггор",
                Content = message,
                PrimaryButtonText = "Да",
                SecondaryButtonText = "Тоже Да",
                CloseButtonText = "Ok",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: gen
            var vertex_n = (int)VertexN.Value;
            var edge_n = (int)EdgesN.Value;

            if (edge_n > vertex_n * (vertex_n - 1) / 2)
            {
                ShowErrorBox("Too much edges for this " + vertex_n + " vertices");
                return;
            }
            graph.Clear();
            limits.Clear();

            foreach (var i in Enumerable.Range(1, vertex_n))
                graph.Add(i, new());

            Random random = new();
            var generate = () => 1 + random.Next(0, vertex_n);
            int edge_count = 0;
            while (edge_count < edge_n)
            {
                var (v1, v2) = (generate(), generate());
                if (graph[v1].Contains(v2) || graph[v2].Contains(v1) || v1 == v2)
                    continue;
                graph[v1].Add(v2);
                var limit = random.Next(1, 10);
                limits.Add((v1, v2), limit);
                edge_count++;
            }
            foreach (var v in graph.Keys)
                foreach (var u in graph.Keys)
                    if (!limits.ContainsKey((v, u)))
                        limits.Add((v, u), 0);

            GraphPainter.Paint(canvas, graph, limits);
        }
        private void GenerateSettingsToggle_Click(object sender, RoutedEventArgs e)
        {
            var toggle = sender as AppBarToggleButton;
            splitView.IsPaneOpen = toggle.IsChecked.Value;
        }
        private void SourceBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            src = (int)sender.Value;
        }
        private void DestinationBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            dst = (int)sender.Value;
        }
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            PreFlowPush flow = new(graph, limits, src, dst);
            GraphPainter.Paint(canvas, flow);
        }
    }
}
