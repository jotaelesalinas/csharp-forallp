using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Putils
{
    public static class Putils
    {
        /// <summary>
        /// Utility class used to pass data to the Progress objects -they accept a single argument.
        /// </summary>
        /// <typeparam name="Tidx"></typeparam>
        /// <typeparam name="Tval"></typeparam>
        protected struct ProgressData<Tidx, Tval>
        {
            public Tidx key;    // the key of the item to be passed to the handler
            public Tval item;   // the item to be passed to the handler
            public int count;   // the number of items in the collection
            public double perc; // percentage of progress (of each individual item, not for the total)
            public string msg;  // used for logging/debugging each item
            public Object obj;  // used for passing arbitrary data
        }

        /// <summary>
        /// Executes some code on each value of a dictionary, in parallel and with progress handlers.
        /// </summary>
        /// <typeparam name="Tidx">Type of the keys in the dictionary.</typeparam>
        /// <typeparam name="Tval">Type of the values in the dictionary.</typeparam>
        /// <param name="source">Collection containing the items to process.</param>
        /// <param name="body">The action that will be done on each item of the collection.
        ///                    It accepts the item, a callback to update the percentage
        ///                    and a callback to receive text messages.</param>
        /// <param name="item_perc">Callback that handles the update of the percentage of each item.</param>
        /// <param name="item_log">Callback that handles the output of text in each item.</param>
        /// <param name="item_started">Callback that handles the start of each item.</param>
        /// <param name="item_finished">Callback that handles the end of each item.</param>
        /// <param name="total_started">Callback that handles the start of the whole process.</param>
        /// <param name="total_finished">Callback that handles the end of the whole process.</param>
        /// 
        public static void Peach<Tidx, Tval>(this IDictionary<Tidx, Tval> source,
                                             Action<Tval, Tidx, Action<double>, Action<string>> body,
                                             Action<Tval, Tidx>         item_started = null,
                                             Action<Tval, Tidx, double> item_perc = null,
                                             Action<Tval, Tidx, string> item_log = null,
                                             Action<Tval, Tidx>         item_finished = null,
                                             Action                     total_started = null,
                                             Action<double>             total_perc = null,
                                             Action                     total_finished = null)
        {
            // To execute code in the main thread -capable of updating the UI- but called from inside the parallel code,
            // we need to call an IProgress, which in turn will call the Action passed as parameter.

            ConcurrentDictionary<Tidx, double> done = new ConcurrentDictionary<Tidx, double>();

            IProgress<ProgressData<Tidx, Tval>> progress_started = item_started == null ? null :
                new Progress<ProgressData<Tidx, Tval>>(x => {
                    item_started.Invoke(x.item, x.key);
                });
            IProgress<ProgressData<Tidx, Tval>> progress_perc = item_perc == null ? null :
                new Progress<ProgressData<Tidx, Tval>>(x => {
                    item_perc.Invoke(x.item, x.key, x.perc);
                    done.AddOrUpdate(x.key, x.perc / 100, (idx, p) => x.perc / 100);
                    total_perc?.Invoke(done.Values.Sum() / source.Count * 100);
                });
            IProgress<ProgressData<Tidx, Tval>> progress_log = item_log == null ? null :
                new Progress<ProgressData<Tidx, Tval>>(x => {
                    item_log.Invoke(x.item, x.key, x.msg);
                });
            IProgress<ProgressData<Tidx, Tval>> progress_finished = item_finished == null ? null :
                new Progress<ProgressData<Tidx, Tval>>(x => {
                    item_finished.Invoke(x.item, x.key);
                    done.AddOrUpdate(x.key, 1, (idx, p) => 1);
                    total_perc?.Invoke(done.Values.Sum() / source.Count * 100);
                });

            // trigger the total_started event
            total_started?.Invoke();

            // the parallel work starts here
            Parallel.ForEach(source, x => {
                // packed data to pass to the IProgresses (they can only accept one parameter)
                ProgressData<Tidx, Tval> pack = new ProgressData<Tidx, Tval> { key = x.Key, item = x.Value };
                
                // trigger the item_started event
                progress_started?.Report(pack);

                // at last! we execute the code for the item.
                // first parameter is the item to be processed.
                // second parameter is a callback that we provide to the body function, in order to trigger percentage updates of each item.
                // third parameter is another callback, in order to trigger text updates (logs) of each item.
                body(x.Value, x.Key,
                     perc => { pack.perc = perc; progress_perc?.Report(pack); },
                     msg => { pack.msg = msg; progress_log?.Report(pack); }
                     );

                // trigger the item_finished event
                progress_finished?.Report(pack);
            });

            // trigger the total_finished event
            total_finished?.Invoke();
        }

        /// <summary>
        /// Executes some code on each item of a collection, in parallel and with progress handlers.
        /// </summary>
        /// <typeparam name="T">Type of the items in the collection.</typeparam>
        /// <param name="source">Collection containing the items to process.</param>
        /// <param name="body">The action that will be done on each item of the collection.
        ///                    It accepts the item, a callback to update the percentage
        ///                    and a callback to receive text messages.</param>
        /// <param name="item_progress">Callback that handles the update of the percentage of each item.</param>
        /// <param name="item_log">Callback that handles the output of text in each item.</param>
        /// <param name="item_started">Callback that handles the start of each item.</param>
        /// <param name="item_finished">Callback that handles the end of each item.</param>
        /// <param name="total_started">Callback that handles the start of the whole process.</param>
        /// <param name="total_finished">Callback that handles the end of the whole process.</param>
        public static void Peach<T>(this IEnumerable<T> source,
                                    Action<T, int, Action<double>, Action<string>> body,
                                    Action<T, int>         item_started = null,
                                    Action<T, int, double> item_perc = null,
                                    Action<T, int, string> item_log = null,
                                    Action<T, int>         item_finished = null,
                                    Action                 total_started = null,
                                    Action<double>         total_perc = null,
                                    Action                 total_finished = null)
        {
            // we create a key-value list in order to assign an numeric index to each item of the collection
            // (remember that not all collections have numeric indeces)
            // the items are linked by reference, so we don't duplicate space
            Dictionary<int, T> dict = new Dictionary<int, T>();
            int idx = 0;
            foreach (T x in source) {
                dict.Add(idx++, x);
            }

            Peach(dict, body, item_started, item_perc, item_log, item_finished, total_started, total_perc, total_finished);
        }

        /// <summary>
        /// Applies a transformation function, called mapper, to all items of a collection and returns the transformed elements.
        /// </summary>
        /// <typeparam name="Tsource">Type of the elements in the original collection.</typeparam>
        /// <typeparam name="Tdest">Type of the transformed elements.</typeparam>
        /// <param name="source"></param>
        /// <param name="mapper"></param>
        /// <param name="total_started"></param>
        /// <param name="total_perc"></param>
        /// <param name="total_finished"></param>
        /// <returns></returns>
        public static IEnumerable<Tdest> Pmap<Tsource, Tdest>(this IEnumerable<Tsource> source,
                                                              Func<Tsource, Tdest> mapper,
                                                              Action         total_started = null,
                                                              Action<double> total_perc = null,
                                                              Action         total_finished = null)
        {
            ConcurrentBag<Tdest> bag = new ConcurrentBag<Tdest>();

            source.Peach((x, i, cb_perc, cb_log) => { bag.Add(mapper(x)); },
                         total_started: total_started, total_perc: total_perc, total_finished: total_finished);

            return bag;
        }

        public static IDictionary<Tidx, Tdest> Pmap<Tidx, Tsource, Tdest>(this IDictionary<Tidx, Tsource> source,
                                                                          Func<Tsource, Tdest> mapper,
                                                                          Action         total_started = null,
                                                                          Action<double> total_perc = null,
                                                                          Action         total_finished = null)
        {
            ConcurrentDictionary<Tidx, Tdest> dict = new ConcurrentDictionary<Tidx, Tdest>();

            source.Peach((x, idx, cb_perc, cb_log) => { dict.TryAdd(idx, mapper(x)); },
                         total_started: total_started, total_perc: total_perc, total_finished: total_finished);

            return dict;
        }

        public static Tdest MapReduce<Tsource, Tdest>(this IEnumerable<Tsource> source,
                                                                    Func<Tsource, Tdest> mapper,
                                                                    Func<Tdest, Tdest, Tdest> reducer,
                                                                    bool Parallel = false)
        {
            return (Parallel ? source.AsParallel() : source).Select(mapper).Aggregate(reducer);
        }

    }
}
