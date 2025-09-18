using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace AutoPlannerHelpers.ViewModels
{
    public class ObservableCollectionPropertyNotify<T> : ObservableCollection<T>
    {
        public void Refresh()
        {
            for (var i = 0; i < this.Count(); i++)
            {
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach(T itr in items) this.Items.Add(itr);
            this.Refresh();
        }
    }
}
