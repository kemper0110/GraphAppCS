using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphAppCS
{
    using VertexMap = System.Collections.Generic.Dictionary<int, int>;
    using ArcMap = System.Collections.Generic.Dictionary<(int, int), int>;
    using Graph = System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>>;
    internal class PreFlowPush
    {
        public Graph graph;
        public ArcMap c;
        public ArcMap f = new();
        public VertexMap h = new(), e = new();
        public int src, dst;
        public PreFlowPush(Graph graph, ArcMap c, int src, int dst)
        {
            this.graph = graph;
            this.c = c;
            this.src = src;
            this.dst = dst;
            init();
            run();
        }
        private void init()
        {
            foreach (var v in graph.Keys)
            {
                e.Add(v, 0);
                h.Add(v, 0);
                foreach (var u in graph.Keys)
                    f.Add((v, u), 0);
            }
            foreach (var u in graph[src])
            {
                var d = c[(src, u)];
                f[(src, u)] = d;
                f[(u, src)] = -d;
                e[u] = d;
                e[src] -= d;
            }
            h[src] = graph.Count;
        }
        private void run()
        {
            // вершина не совпадает с S и T и переполнена
            var fetch_excessive = () => from v_u in e
                                        where v_u.Key != src && v_u.Key != dst && v_u.Value > 0
                                        select v_u.Key;
            for (var fetched_i = fetch_excessive(); fetched_i.Any(); fetched_i = fetch_excessive())
            {
                var i = fetched_i.First();
                // поток меньше ограничения и соседняя вершина выше переполненной
                var fetched_j = from j in graph.Keys
                                where f[(i, j)] < c[(i, j)] && h[i] == h[j] + 1
                                select j;
                if (!fetched_j.Any())
                    lift(i);
                else
                    push(i, fetched_j.First());
            }
        }

        void push(int u, int v)
        {
            var d = System.Math.Min(e[u], c[(u, v)] - f[(u, v)]);
            f[(u, v)] += d;
            f[(v, u)] = -f[(u, v)];
            e[u] -= d;
            e[v] += d;
        }

        void lift(int u)
        {
            h[u] = 1 + (from v in graph.Keys where f[(u, v)] < c[(u, v)] select h[v]).Min();
        }
    }
}
