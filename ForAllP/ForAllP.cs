using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForAllP
{
    public static class Utils
    {
        struct ProgressData<U>
        {
            public U item;
            public int n;
            public int t;
            public double p;
        }

        public static void ForAllP<T>(this IEnumerable<T> source,
                                      Action<T, Action<double>> body,
                                      Action<T, int, int, double> item_progress = null,
                                      Action<T, int, int> item_started = null,
                                      Action<T, int, int> item_finished = null,
                                      Action total_started = null,
                                      Action total_finished = null)
        {
            IProgress<ProgressData<T>> progress_started = new Progress<ProgressData<T>>(x => {
                item_started?.Invoke(x.item, x.n, x.t);
            });
            IProgress<ProgressData<T>> progress_progress = new Progress<ProgressData<T>>(x => {
                item_progress?.Invoke(x.item, x.n, x.t, x.p);
            });
            IProgress<ProgressData<T>> progress_finished = new Progress<ProgressData<T>>(x => {
                item_finished?.Invoke(x.item, x.n, x.t);
            });

            total_started?.Invoke();

            List<KeyValuePair<int, T>> kvs = new List<KeyValuePair<int, T>>();
            int idx = 0;
            foreach (T x in source)
            {
                kvs.Add(new KeyValuePair<int, T>(idx++, x));
            }

            Parallel.ForEach(kvs, x => {
                ProgressData<T> pack = new ProgressData<T> { item = x.Value, n = x.Key + 1, t = kvs.Count };

                progress_started.Report(pack);
                body(x.Value, perc => { pack.p = perc; progress_progress.Report(pack); });
                progress_finished.Report(pack);
            });

            total_finished?.Invoke();
        }
    }
}
