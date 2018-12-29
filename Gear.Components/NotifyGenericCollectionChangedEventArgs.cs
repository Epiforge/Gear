using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;

namespace Gear.Components
{
    public class NotifyGenericCollectionChangedEventArgs<T>
    {
        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action)
        {
            if (action != NotifyCollectionChangedAction.Reset)
                throw new ArgumentOutOfRangeException(nameof(action));
            InitializeAdd(action, null, -1);
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T changedItem)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItem != null)
                        throw new ArgumentException(nameof(changedItem));
                    InitializeAdd(action, null, -1);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    InitializeAddOrRemove(action, new T[] { changedItem }, -1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T changedItem, int index)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItem != null)
                        throw new ArgumentException(nameof(changedItem));
                    if (index != -1)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    InitializeAdd(action, null, -1);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    InitializeAddOrRemove(action, new T[] { changedItem }, index);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IReadOnlyList<T> changedItems)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItems != null)
                        throw new ArgumentException(nameof(changedItems));
                    InitializeAdd(action, null, -1);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (changedItems == null)
                        throw new ArgumentNullException(nameof(changedItems));
                    InitializeAddOrRemove(action, changedItems, -1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T> changedItems, int startingIndex)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Reset:
                    if (changedItems != null)
                        throw new ArgumentException(nameof(changedItems));
                    if (startingIndex != -1)
                        throw new ArgumentOutOfRangeException(nameof(startingIndex));
                    InitializeAdd(action, null, -1);
                    break;
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (changedItems == null)
                        throw new ArgumentNullException(nameof(changedItems));
                    if (startingIndex < -1)
                        throw new ArgumentOutOfRangeException(nameof(startingIndex));
                    InitializeAddOrRemove(action, changedItems, startingIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T newItem, T oldItem)
        {
            if (action != NotifyCollectionChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            InitializeMoveOrReplace(action, new T[] { newItem }, new T[] { oldItem }, -1, -1);
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T newItem, T oldItem, int index)
        {
            if (action != NotifyCollectionChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            InitializeMoveOrReplace(action, new T[] { newItem }, new T[] { oldItem }, index, index);
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T> newItems, IEnumerable<T> oldItems)
        {
            if (action != NotifyCollectionChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));
            if (oldItems == null)
                throw new ArgumentNullException(nameof(oldItems));
            InitializeMoveOrReplace(action, newItems, oldItems, -1, -1);
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T> newItems, IEnumerable<T> oldItems, int startingIndex)
        {
            if (action != NotifyCollectionChangedAction.Replace)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));
            if (oldItems == null)
                throw new ArgumentNullException(nameof(oldItems));
            InitializeMoveOrReplace(action, newItems, oldItems, startingIndex, startingIndex);
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, T changedItem, int index, int oldIndex)
        {
            if (action != NotifyCollectionChangedAction.Move)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            var changedItems = new T[] { changedItem };
            InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
        }

        public NotifyGenericCollectionChangedEventArgs(NotifyCollectionChangedAction action, IEnumerable<T> changedItems, int index, int oldIndex)
        {
            if (action != NotifyCollectionChangedAction.Move)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            InitializeMoveOrReplace(action, changedItems, changedItems, index, oldIndex);
        }

        void InitializeAdd(NotifyCollectionChangedAction action, IEnumerable<T> newItems, int newStartingIndex)
        {
            Action = action;
            NewItems = newItems?.ToImmutableArray();
            NewStartingIndex = newStartingIndex;
        }

        void InitializeAddOrRemove(NotifyCollectionChangedAction action, IEnumerable<T> changedItems, int startingIndex)
        {
            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    InitializeAdd(action, changedItems, startingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    InitializeRemove(action, changedItems, startingIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }
               
        void InitializeMoveOrReplace(NotifyCollectionChangedAction action, IEnumerable<T> newItems, IEnumerable<T> oldItems, int startingIndex, int oldStartingIndex)
        {
            InitializeAdd(action, newItems, startingIndex);
            InitializeRemove(action, oldItems, oldStartingIndex);
        }

        void InitializeRemove(NotifyCollectionChangedAction action, IEnumerable<T> oldItems, int oldStartingIndex)
        {
            Action = action;
            OldItems = oldItems?.ToImmutableArray();
            OldStartingIndex = oldStartingIndex;
        }

        public NotifyCollectionChangedAction Action { get; private set; }

        public IReadOnlyList<T> NewItems { get; private set; }

        public int NewStartingIndex { get; private set; }

        public IReadOnlyList<T> OldItems { get; private set; }

        public int OldStartingIndex { get; private set; }
    }
}
