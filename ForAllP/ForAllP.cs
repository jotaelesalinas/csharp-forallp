using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ForAllP
{
    public static class Utils
    {
        /// <summary>
        /// Utility class used to pass data to the Progress objects -they accept a single argument.
        /// </summary>
        /// <typeparam name="U">The type of the main item to be passed to the progress handler.</typeparam>
        protected struct ProgressData<U>
        {
            public U item;      // the item to be passed to the handler
            public int n;       // the index of the item in the collection (because of the nature of threads, it is usually not sequential)
            public int t;       // the number of items in the collection
            public double p;    // percentage of progress (of each individual item, not for the total)
            public string l;    // used for logging/debugging each item
        }

        /// <summary>
        /// Executes some code on each item of a collection, in parallel and with progress handlers.
        /// </summary>
        /// <typeparam name="T">Type of the items contained in the collection.</typeparam>
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
        public static void ForAllP<T>(this IEnumerable<T> source,
                                      Action<T, Action<double>, Action<string>> body,
                                      Action<T, int, int, double> item_progress = null,
                                      Action<T, int, int, string> item_log = null,
                                      Action<T, int, int> item_started = null,
                                      Action<T, int, int> item_finished = null,
                                      Action total_started = null,
                                      Action total_finished = null)
        {
            // To execute code in the main thread -capable of updating the UI- but called from inside the parallel code,
            // we need to call an IProgress, which in turn will call the Action passed as parameter.

            IProgress<ProgressData<T>> progress_progress = new Progress<ProgressData<T>>(x => {
                item_progress?.Invoke(x.item, x.n, x.t, x.p);
            });
            IProgress<ProgressData<T>> progress_log = new Progress<ProgressData<T>>(x => {
                item_log?.Invoke(x.item, x.n, x.t, x.l);
            });
            IProgress<ProgressData<T>> progress_started = new Progress<ProgressData<T>>(x => {
                item_started?.Invoke(x.item, x.n, x.t);
            });
            IProgress<ProgressData<T>> progress_finished = new Progress<ProgressData<T>>(x => {
                item_finished?.Invoke(x.item, x.n, x.t);
            });

            // trigger the total_started event
            total_started?.Invoke();

            // we create a key-value list in order to assign an numeric index to each item of the collection
            // (remember that not all collections have numeric indeces)
            // the items are linked by reference, so we don't duplicate space
            List<KeyValuePair<int, T>> kvs = new List<KeyValuePair<int, T>>();
            int idx = 0;
            foreach (T x in source) {
                kvs.Add(new KeyValuePair<int, T>(idx++, x));
            }

            // the parallel work starts here
            Parallel.ForEach(kvs, x => {
                // packed data to pass to the IProgresses (they can only accept one parameter)
                ProgressData<T> pack = new ProgressData<T> { item = x.Value, n = x.Key + 1, t = kvs.Count };

                // trigger the item_started event, indirectly through item_started
                progress_started.Report(pack);

                // at last! we execute the code for the item.
                // first parameter is the item to be processed.
                // second parameter is a callback that we provide to the body function, in order to trigger percentage updates of each item.
                // third parameter is another callback, in order to trigger text updates (logs) of each item.
                body(x.Value,
                     perc => { pack.p = perc; progress_progress.Report(pack); },
                     msg => { pack.l = msg; progress_log.Report(pack); });

                // trigger the item_finished event, indirectly through item_finished
                progress_finished.Report(pack);
            });

            // trigger the total_finished event
            total_finished?.Invoke();
        }
    }
}
