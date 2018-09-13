using Gear.Components;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.ActiveExpressions
{
    class ActiveIndexExpression : ActiveExpression
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(ActiveExpression @object, PropertyInfo indexer, EquatableList<ActiveExpression> arguments), ActiveIndexExpression> instances = new Dictionary<(ActiveExpression @object, PropertyInfo indexer, EquatableList<ActiveExpression> arguments), ActiveIndexExpression>();

        public static ActiveIndexExpression Create(IndexExpression indexExpression)
        {
            var @object = Create(indexExpression.Object);
            var indexer = indexExpression.Indexer;
            var arguments = new EquatableList<ActiveExpression>(indexExpression.Arguments.Select(argument => Create(argument)).ToList());
            var key = (@object, indexer, arguments);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeIndexExpression))
                {
                    activeIndexExpression = new ActiveIndexExpression(indexExpression.Type, @object, indexer, arguments);
                    instances.Add(key, activeIndexExpression);
                }
                ++activeIndexExpression.disposalCount;
                return activeIndexExpression;
            }
        }

        ActiveIndexExpression(Type type, ActiveExpression @object, PropertyInfo indexer, EquatableList<ActiveExpression> arguments) : base(type, ExpressionType.Index)
        {
            this.indexer = indexer;
            fastGetter = GetFastMethodInfo(this.indexer.GetMethod);
            this.@object = @object;
            this.@object.PropertyChanged += ObjectPropertyChanged;
            this.arguments = arguments;
            foreach (var argument in this.arguments)
                argument.PropertyChanged += ArgumentPropertyChanged;
            objectValue = this.@object.Value;
            SubscribeToObjectValueNotifications();
            Evaluate();
        }

        readonly EquatableList<ActiveExpression> arguments;
        int disposalCount;
        readonly FastMethodInfo fastGetter;
        readonly PropertyInfo indexer;
        readonly ActiveExpression @object;
        object objectValue;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            lock (instanceManagementLock)
            {
                if (--disposalCount > 0)
                    return false;
                UnsubscribeFromObjectValueNotifications();
                @object.PropertyChanged -= ObjectPropertyChanged;
                @object.Dispose();
                foreach (var argument in arguments)
                {
                    argument.PropertyChanged -= ArgumentPropertyChanged;
                    argument.Dispose();
                }
                instances.Remove((@object, indexer, arguments));
                return true;
            }
        }

        void Evaluate()
        {
            try
            {
                var objectFault = @object.Fault;
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault != null).FirstOrDefault();
                if (objectFault != null)
                    Fault = objectFault;
                else if (argumentFault != null)
                    Fault = argumentFault;
                else
                {
                    var newObjectValue = @object.Value;
                    if (newObjectValue != objectValue)
                    {
                        UnsubscribeFromObjectValueNotifications();
                        objectValue = newObjectValue;
                        SubscribeToObjectValueNotifications();
                    }
                    Value = fastGetter.Invoke(objectValue, arguments.Select(argument => argument.Value).ToArray());
                }
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        void ObjectPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        void ObjectValueCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        if (e.NewStartingIndex >= 0 && (e.NewItems?.Count ?? 0) > 0 && arguments.Count == 1 && arguments[0].Value is int index)
                        {
                            if (e.NewStartingIndex <= index)
                                Evaluate();
                        }
                        else
                            Evaluate();
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    {
                        var movingCount = Math.Max(e.OldItems?.Count ?? 0, e.NewItems?.Count ?? 0);
                        if (e.OldStartingIndex >= 0 && e.NewStartingIndex >= 0 && movingCount > 0 && arguments.Count == 1 && arguments[0].Value is int index)
                        {
                            if ((index >= e.OldStartingIndex && index < e.OldStartingIndex + movingCount) || (index >= e.NewStartingIndex && index < e.NewStartingIndex + movingCount))
                                Evaluate();
                        }
                        else
                            Evaluate();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        if (e.OldStartingIndex >= 0 && (e.OldItems?.Count ?? 0) > 0 && arguments.Count == 1 && arguments[0].Value is int index)
                        {
                            if (e.OldStartingIndex <= index)
                                Evaluate();
                        }
                        else
                            Evaluate();
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    {
                        if (arguments.Count == 1 && arguments[0].Value is int index)
                        {
                            var oldCount = e.OldItems?.Count ?? 0;
                            var newCount = e.NewItems?.Count ?? 0;
                            if ((oldCount != newCount && (e.OldStartingIndex >= 0 || e.NewStartingIndex >= 0) && index >= Math.Min(Math.Max(e.OldStartingIndex, 0), Math.Max(e.NewStartingIndex, 0))) || (e.OldStartingIndex >= 0 && index >= e.OldStartingIndex && index < e.OldStartingIndex + oldCount) || (e.NewStartingIndex >= 0 && index >= e.NewStartingIndex && index < e.NewStartingIndex + newCount))
                                Evaluate();
                        }
                        else
                            Evaluate();
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Evaluate();
                    break;
            }
        }

        void ObjectValueDictionaryValueAdded(object sender, NotifyDictionaryValueEventArgs e)
        {
            if (arguments.Count == 1 && (arguments[0].Value?.Equals(e.Key) ?? false))
                Value = e.Value;
        }

        void ObjectValueDictionaryValueRemoved(object sender, NotifyDictionaryValueEventArgs e)
        {
            if (arguments.Count == 1)
            {
                var key = arguments[0].Value;
                if (key?.Equals(e.Key) ?? false)
                    Fault = new KeyNotFoundException($"Key '{key}' was removed");
            }
        }

        void ObjectValueDictionaryValueReplaced(object sender, NotifyDictionaryValueReplacedEventArgs e)
        {
            if (arguments.Count == 1 && (arguments[0].Value?.Equals(e.Key) ?? false))
                Value = e.NewValue;
        }

        void ObjectValueDictionaryValuesAdded(object sender, NotifyDictionaryValuesEventArgs e)
        {
            if (arguments.Count == 1)
            {
                var key = arguments[0].Value;
                if (key != null)
                {
                    var keyValuePair = e.KeyValuePairs?.FirstOrDefault(kv => key.Equals(kv.Key));
                    if (keyValuePair != null)
                        Value = keyValuePair.Value.Value;
                }
            }
        }

        void ObjectValueDictionaryValuesRemoved(object sender, NotifyDictionaryValuesEventArgs e)
        {
            if (arguments.Count == 1)
            {
                var key = arguments[0].Value;
                if (key != null)
                {
                    var keyValuePair = e.KeyValuePairs?.FirstOrDefault(kv => key.Equals(kv.Key));
                    if (keyValuePair != null)
                        Fault = new KeyNotFoundException($"Key '{key}' was removed");
                }
            }
        }

        void ObjectValuePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == indexer.Name)
                Evaluate();
        }

        void SubscribeToObjectValueNotifications()
        {
            if (objectValue is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged += ObjectValueCollectionChanged;
            if (objectValue is INotifyDictionaryChanged dictionaryChangedNotifier)
            {
                dictionaryChangedNotifier.ValueAdded += ObjectValueDictionaryValueAdded;
                dictionaryChangedNotifier.ValueRemoved += ObjectValueDictionaryValueRemoved;
                dictionaryChangedNotifier.ValueReplaced += ObjectValueDictionaryValueReplaced;
                dictionaryChangedNotifier.ValuesAdded += ObjectValueDictionaryValuesAdded;
                dictionaryChangedNotifier.ValuesRemoved += ObjectValueDictionaryValuesRemoved;
            }
            if (objectValue is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged += ObjectValuePropertyChanged;
        }

        void UnsubscribeFromObjectValueNotifications()
        {
            if (objectValue is INotifyCollectionChanged collectionChangedNotifier)
                collectionChangedNotifier.CollectionChanged -= ObjectValueCollectionChanged;
            if (objectValue is INotifyDictionaryChanged dictionaryChangedNotifier)
            {
                dictionaryChangedNotifier.ValueAdded -= ObjectValueDictionaryValueAdded;
                dictionaryChangedNotifier.ValueRemoved -= ObjectValueDictionaryValueRemoved;
                dictionaryChangedNotifier.ValueReplaced -= ObjectValueDictionaryValueReplaced;
                dictionaryChangedNotifier.ValuesAdded -= ObjectValueDictionaryValuesAdded;
                dictionaryChangedNotifier.ValuesRemoved -= ObjectValueDictionaryValuesRemoved;
            }
            if (objectValue is INotifyPropertyChanged propertyChangedNotifier)
                propertyChangedNotifier.PropertyChanged -= ObjectValuePropertyChanged;
        }
    }
}