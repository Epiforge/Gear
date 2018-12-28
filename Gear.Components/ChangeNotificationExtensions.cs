using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.Components
{
    public static class ChangeNotificationExtensions
    {
        static readonly ConcurrentDictionary<PropertyInfo, FastMethodInfo> fastGetters = new ConcurrentDictionary<PropertyInfo, FastMethodInfo>();
        static readonly ConcurrentDictionary<(Type type, string name), PropertyInfo> properties = new ConcurrentDictionary<(Type type, string name), PropertyInfo>();

        static FastMethodInfo CreateFastGetter(PropertyInfo property) => new FastMethodInfo(property.GetMethod);

        static PropertyInfo GetProperty((Type type, string name) propertyDetails) => propertyDetails.type.GetRuntimeProperty(propertyDetails.name);

        public static Action OnCollectionChange(this INotifyCollectionChanged notifyingCollection, Action<IReadOnlyList<object>, int> onElementsAdded, Action<IReadOnlyList<object>, int, int> onElementsMoved, Action<IReadOnlyList<object>, int> onElementsRemoved, Action onReset)
        {
            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        onReset();
                        break;
                    case NotifyCollectionChangedAction.Move:
                        onElementsMoved(e.OldItems.Cast<object>().ToImmutableArray(), e.OldStartingIndex, e.NewStartingIndex);
                        break;
                    default:
                        if (e.OldItems != null && e.OldStartingIndex >= 0)
                            onElementsRemoved(e.OldItems.Cast<object>().ToImmutableArray(), e.OldStartingIndex);
                        if (e.NewItems != null && e.NewStartingIndex >= 0)
                            onElementsAdded(e.NewItems.Cast<object>().ToImmutableArray(), e.NewStartingIndex);
                        break;
                }
            }

            notifyingCollection.CollectionChanged += collectionChanged;
            return () => notifyingCollection.CollectionChanged -= collectionChanged;
        }

        public static Action OnCollectionChange<TElement, TNotifyingReadOnlyList>(this TNotifyingReadOnlyList notifyingReadOnlyList, Action<IReadOnlyList<TElement>, int> onElementsAdded, Action<IReadOnlyList<TElement>, int, int> onElementsMoved, Action<IReadOnlyList<TElement>, int> onElementsRemoved, Action onReset) where TNotifyingReadOnlyList : IReadOnlyList<TElement>, INotifyCollectionChanged
        {
            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        onReset();
                        break;
                    case NotifyCollectionChangedAction.Move:
                        onElementsMoved(e.OldItems.Cast<TElement>().ToImmutableArray(), e.OldStartingIndex, e.NewStartingIndex);
                        break;
                    default:
                        if (e.OldItems != null && e.OldStartingIndex >= 0)
                            onElementsRemoved(e.OldItems.Cast<TElement>().ToImmutableArray(), e.OldStartingIndex);
                        if (e.NewItems != null && e.NewStartingIndex >= 0)
                            onElementsAdded(e.NewItems.Cast<TElement>().ToImmutableArray(), e.NewStartingIndex);
                        break;
                }
            }

            notifyingReadOnlyList.CollectionChanged += collectionChanged;
            return () => notifyingReadOnlyList.CollectionChanged -= collectionChanged;
        }

        public static Action OnDictionaryChange<TKey, TValue>(this INotifyDictionaryChanged<TKey, TValue> notifyingDictionary, Action onReset, Action<TKey, TValue, TValue> onValueReplaced, Action<IReadOnlyList<KeyValuePair<TKey, TValue>>> onValuesAdded, Action<IReadOnlyList<KeyValuePair<TKey, TValue>>> onValuesRemoved)
        {
            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e)
            {
                switch (e.Action)
                {
                    case NotifyDictionaryChangedAction.Reset:
                        onReset();
                        break;
                    default:
                        if (e.OldItems != null)
                            onValuesRemoved(e.OldItems);
                        if (e.NewItems != null)
                            onValuesAdded(e.NewItems);
                        break;
                }
            }

            notifyingDictionary.DictionaryChanged += dictionaryChanged;
            return () => notifyingDictionary.DictionaryChanged -= dictionaryChanged;
        }

        public static Action OnPropertyChange<TInstance, TPropertyValue>(this TInstance obj, string propertyName, Action<TPropertyValue> onPropertyChanging, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged, INotifyPropertyChanging
        {
            var fastGetter = fastGetters.GetOrAdd(properties.GetOrAdd((typeof(TInstance), propertyName), GetProperty), CreateFastGetter);

            void propertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyName)
                    onPropertyChanged((TPropertyValue)fastGetter.Invoke(obj));
            }

            void propertyChanging(object sender, PropertyChangingEventArgs e)
            {
                if (e.PropertyName == propertyName)
                    onPropertyChanging((TPropertyValue)fastGetter.Invoke(obj));
            }

            obj.PropertyChanging += propertyChanging;
            obj.PropertyChanged += propertyChanged;
            return () =>
            {
                obj.PropertyChanging -= propertyChanging;
                obj.PropertyChanged -= propertyChanged;
            };
        }

        public static Action OnPropertyChange<TInstance, TPropertyValue>(this TInstance obj, Expression<Func<TInstance, TPropertyValue>> propertyExpression, Action<TPropertyValue> onPropertyChanging, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged, INotifyPropertyChanging
        {
            if (propertyExpression.Body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo property && memberExpression.Expression is ParameterExpression)
            {
                if (memberExpression.Expression.Type != typeof(TInstance))
                    throw new ArgumentException($"Accessed property must belong to {nameof(TInstance)}", nameof(propertyExpression));

                var propertyName = property.Name;
                var fastGetter = fastGetters.GetOrAdd(property, CreateFastGetter);

                void propertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == propertyName)
                        onPropertyChanged((TPropertyValue)fastGetter.Invoke(obj));
                }

                void propertyChanging(object sender, PropertyChangingEventArgs e)
                {
                    if (e.PropertyName == propertyName)
                        onPropertyChanging((TPropertyValue)fastGetter.Invoke(obj));
                }

                obj.PropertyChanging += propertyChanging;
                obj.PropertyChanged += propertyChanged;
                return () =>
                {
                    obj.PropertyChanging -= propertyChanging;
                    obj.PropertyChanged -= propertyChanged;
                };
            }
            throw new ArgumentException("Expression must only access a property", nameof(propertyExpression));
        }

        public static Action OnPropertyChanged<TInstance, TPropertyValue>(this TInstance obj, string propertyName, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged
        {
            var fastGetter = fastGetters.GetOrAdd(properties.GetOrAdd((typeof(TInstance), propertyName), GetProperty), CreateFastGetter);

            void propertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyName)
                    onPropertyChanged((TPropertyValue)fastGetter.Invoke(obj));
            }

            obj.PropertyChanged += propertyChanged;
            return () => obj.PropertyChanged -= propertyChanged;
        }

        public static Action OnPropertyChanged<TInstance, TPropertyValue>(this TInstance obj, Expression<Func<TInstance, TPropertyValue>> propertyExpression, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged
        {
            if (propertyExpression.Body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo property && memberExpression.Expression is ParameterExpression)
            {
                if (memberExpression.Expression.Type != typeof(TInstance))
                    throw new ArgumentException($"Accessed property must belong to {nameof(TInstance)}", nameof(propertyExpression));

                var propertyName = property.Name;
                var fastGetter = fastGetters.GetOrAdd(property, CreateFastGetter);

                void propertyChanged(object sender, PropertyChangedEventArgs e)
                {
                    if (e.PropertyName == propertyName)
                        onPropertyChanged((TPropertyValue)fastGetter.Invoke(obj));
                }

                obj.PropertyChanged += propertyChanged;
                return () => obj.PropertyChanged -= propertyChanged;
            }
            throw new ArgumentException("Expression must only access a property", nameof(propertyExpression));
        }

        public static Action OnPropertyChanging<TInstance, TPropertyValue>(this TInstance obj, string propertyName, Action<TPropertyValue> onPropertyChanging) where TInstance : INotifyPropertyChanging
        {
            var fastGetter = fastGetters.GetOrAdd(properties.GetOrAdd((typeof(TInstance), propertyName), GetProperty), CreateFastGetter);

            void propertyChanging(object sender, PropertyChangingEventArgs e)
            {
                if (e.PropertyName == propertyName)
                    onPropertyChanging((TPropertyValue)fastGetter.Invoke(obj));
            }

            obj.PropertyChanging += propertyChanging;
            return () => obj.PropertyChanging -= propertyChanging;
        }

        public static Action OnPropertyChanging<TInstance, TPropertyValue>(this TInstance obj, Expression<Func<TInstance, TPropertyValue>> propertyExpression, Action<TPropertyValue> onPropertyChanging) where TInstance : INotifyPropertyChanging
        {
            if (propertyExpression.Body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo property && memberExpression.Expression is ParameterExpression)
            {
                if (memberExpression.Expression.Type != typeof(TInstance))
                    throw new ArgumentException($"Accessed property must belong to {nameof(TInstance)}", nameof(propertyExpression));

                var propertyName = property.Name;
                var fastGetter = fastGetters.GetOrAdd(property, CreateFastGetter);

                void propertyChanging(object sender, PropertyChangingEventArgs e)
                {
                    if (e.PropertyName == propertyName)
                        onPropertyChanging((TPropertyValue)fastGetter.Invoke(obj));
                }

                obj.PropertyChanging += propertyChanging;
                return () => obj.PropertyChanging -= propertyChanging;
            }
            throw new ArgumentException("Expression must only access a property", nameof(propertyExpression));
        }
    }
}
