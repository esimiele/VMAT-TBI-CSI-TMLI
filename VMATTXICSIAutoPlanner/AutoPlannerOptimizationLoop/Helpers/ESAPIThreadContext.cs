using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AutoPlannerOptimizationLoop.Helpers
{
    public static class ESAPIThreadContext
    {
        private static Dispatcher _uiDispatcher;
        public static Dispatcher UIDispatcher { get => _uiDispatcher; set => _uiDispatcher = value; }
        public static Dispatcher ESAPIDispatcher { get; private set; }

        public static void Initialize(Dispatcher dispatcher)
        {
            ESAPIDispatcher = dispatcher;
        }

        public static Task RunOnESAPIThreadAsync(Action action)
        {
            return ESAPIDispatcher.InvokeAsync(action).Task;
        }

        public static void RunOnESAPIThreadSync(Action action)
        {
            ESAPIDispatcher.Invoke(() => action());
        }

        public static Task RunOnUIThreadAsync(Action action)
        {
            return UIDispatcher.InvokeAsync(action).Task;
        }

        public static void RunOnUIThreadSync(Action action)
        {
            UIDispatcher.Invoke(() => action());
        }
    }
}
