using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MCERemote.Controller
{
    /// <summary>
    /// Collection that can be resized with a single reset notification, rather than one
    /// notification per item, which would be slow for long lists.
    /// </summary>
    public class ResizableCollection<T> : ObservableCollection<T>
    {
        /// <param name="createItem">Creates the placeholder item for a 1-based position.</param>
        public ResizableCollection(Func<int, T> createItem)
        {
            this.createItem = createItem;
        }

        private readonly Func<int, T> createItem;

        /// <summary>
        /// Removes items from the end or adds placeholders at the end to reach the given length.
        /// </summary>
        public void Resize(int length)
        {
            if (length == Count)
                return;

            while (Items.Count > length)
                Items.RemoveAt(Items.Count - 1);

            while (Items.Count < length)
                Items.Add(createItem(Items.Count + 1));

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
