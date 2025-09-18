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
        private static Dispatcher _UIDispatcher;
        public static Dispatcher ESAPIDispatcher { get; private set; }
        public static Dispatcher UIDispatcher { get => _UIDispatcher; set => _UIDispatcher = value; }
        public static SynchronizationContext ESAPISyncContext { get; private set; }

        public static void Initialize(Dispatcher dispatch)
        {
            ESAPISyncContext = SynchronizationContext.Current ?? new SynchronizationContext();
            ESAPIDispatcher = dispatch;
        }

        public static void RunOnESAPIThread(Action action)
        {
            ESAPISyncContext?.Post(_ => action(), null);
        }

        public static void RunOnESAPIThreadSync(Action action)
        {
            ESAPISyncContext?.Send(_ => action(), null);
        }
    }
}
