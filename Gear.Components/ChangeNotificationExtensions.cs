using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Gear.Components
{
    /// <summary>
    /// Provides extensions for managing event handlers for instances of <see cref="INotifyCollectionChanged"/>, <see cref="INotifyDictionaryChanged{TKey, TValue}"/>, <see cref="INotifyGenericCollectionChanged{T}"/>, <see cref="INotifyPropertyChanged"/>, and <see cref="INotifyPropertyChanging"/>
    /// </summary>
    public static class ChangeNotificationExtensions
    {
        static readonly ConcurrentDictionary<PropertyInfo, FastMethodInfo> fastGetters = new ConcurrentDictionary<PropertyInfo, FastMethodInfo>();
        static readonly ConcurrentDictionary<(Type type, string name), PropertyInfo> properties = new ConcurrentDictionary<(Type type, string name), PropertyInfo>();

        static FastMethodInfo CreateFastGetter(PropertyInfo property) => new FastMethodInfo(property.GetMethod);

        static PropertyInfo GetProperty((Type type, string name) propertyDetails) => propertyDetails.type.GetRuntimeProperty(propertyDetails.name);

        static string GetPropertyNameFromExpression<TInstance, TPropertyValue>(this TInstance obj, Expression<Func<TInstance, TPropertyValue>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo property && memberExpression.Expression is ParameterExpression)
            {
                if (memberExpression.Expression.Type != typeof(TInstance))
                    throw new ArgumentException($"Accessed property must belong to {nameof(TInstance)}", nameof(propertyExpression));
                return property.Name;
            }
            throw new ArgumentException("Expression must only access a property", nameof(propertyExpression));
        }

        /// <summary>
        /// Subscribes to the <see cref="INotifyCollectionChanged.CollectionChanged"/> event using the specified handler
        /// </summary>
        /// <param name="notifyingCollection">The notifying collection</param>
        /// <param name="onCollectionChanged">The handler</param>
        /// <returns>An action that will unsubscribe the handler from the <see cref="INotifyCollectionChanged.CollectionChanged"/> event</returns>
        public static Action OnCollectionChanged(this INotifyCollectionChanged notifyingCollection, Action<NotifyCollectionChangedEventArgs> onCollectionChanged)
        {
            void collectionChanged(object sender, NotifyCollectionChangedEventArgs e) => onCollectionChanged(e);
            notifyingCollection.CollectionChanged += collectionChanged;
            return () => notifyingCollection.CollectionChanged -= collectionChanged;
        }

        /// <summary>
        /// Subscribes to the <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/> event using the specified handler
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary</typeparam>
        /// <param name="notifyingDictionary">The notifying dictionary</param>
        /// <param name="onDictionaryChanged">The handler</param>
        /// <returns>An action that will unsubscribe the handler from the <see cref="INotifyDictionaryChanged{TKey, TValue}.DictionaryChanged"/> event</returns>
        public static Action OnDictionaryChanged<TKey, TValue>(this INotifyDictionaryChanged<TKey, TValue> notifyingDictionary, Action<NotifyDictionaryChangedEventArgs<TKey, TValue>> onDictionaryChanged)
        {
            void dictionaryChanged(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> e) => onDictionaryChanged(e);
            notifyingDictionary.DictionaryChanged += dictionaryChanged;
            return () => notifyingDictionary.DictionaryChanged -= dictionaryChanged;
        }

        /// <summary>
        /// Subscribes to the <see cref="INotifyGenericCollectionChanged{T}.GenericCollectionChanged"/> event using the specified handler
        /// </summary>
        /// <typeparam name="T">The type of the items in the collection</typeparam>
        /// <param name="notifyingCollection">The notifying collection</param>
        /// <param name="onGenericCollectionChanged">The handler</param>
        /// <returns>An action that will unsubscribe the handler from the <see cref="INotifyGenericCollectionChanged{T}.GenericCollectionChanged"/> event</returns>
        public static Action OnGenericCollectionChanged<T>(this INotifyGenericCollectionChanged<T> notifyingCollection, Action<INotifyGenericCollectionChangedEventArgs<T>> onGenericCollectionChanged)
        {
            void genericCollectionChanged(object sender, INotifyGenericCollectionChangedEventArgs<T> e) => onGenericCollectionChanged(e);
            notifyingCollection.GenericCollectionChanged += genericCollectionChanged;
            return () => notifyingCollection.GenericCollectionChanged -= genericCollectionChanged;
        }

        /// <summary>
        /// Subscribes to the <see cref="INotifyPropertyChanging.PropertyChanging"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events and executes the specified handlers when the specified property is changing or has changed
        /// </summary>
        /// <typeparam name="TInstance">The type of the notifying object</typeparam>
        /// <typeparam name="TPropertyValue">The type of the property</typeparam>
        /// <param name="obj">The notifying object</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="onPropertyChanging">The handler to execute when the property is changing</param>
        /// <param name="onPropertyChanged">The handler to execute when the property has changed</param>
        /// <returns>An action that will unsubscribe the handlers from the <see cref="INotifyPropertyChanging.PropertyChanging"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events</returns>
        public static Action OnPropertyChange<TInstance, TPropertyValue>(this TInstance obj, string propertyName, Action<TPropertyValue> onPropertyChanging, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged, INotifyPropertyChanging =>
            OnPropertyChanging(obj, propertyName, onPropertyChanging) + OnPropertyChanged(obj, propertyName, onPropertyChanged);

        /// <summary>
        /// Subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event and executes the specified handlers when the specified property is changing or has changed
        /// </summary>
        /// <typeparam name="TInstance">The type of the notifying object</typeparam>
        /// <typeparam name="TPropertyValue">The type of the property</typeparam>
        /// <param name="obj">The notifying object</param>
        /// <param name="propertyExpression">An expression indicating the property</param>
        /// <param name="onPropertyChanging">The handler to execute when the property is changing</param>
        /// <param name="onPropertyChanged">The handler to execute when the property has changed</param>
        /// <returns>An action that will unsubscribe the handlers from the <see cref="INotifyPropertyChanging.PropertyChanging"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> events</returns>
        public static Action OnPropertyChange<TInstance, TPropertyValue>(this TInstance obj, Expression<Func<TInstance, TPropertyValue>> propertyExpression, Action<TPropertyValue> onPropertyChanging, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged, INotifyPropertyChanging =>
            OnPropertyChange(obj, GetPropertyNameFromExpression(obj, propertyExpression), onPropertyChanging, onPropertyChanged);

        /// <summary>
        /// Subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event and executes the specified handler when the specified property has changed
        /// </summary>
        /// <typeparam name="TInstance">The type of the notifying object</typeparam>
        /// <typeparam name="TPropertyValue">The type of the property</typeparam>
        /// <param name="obj">The notifying object</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="onPropertyChanged">The handler</param>
        /// <returns>An action that will unsubscribe the handler from the <see cref="INotifyPropertyChanged.PropertyChanged"/> event</returns>
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

        /// <summary>
        /// Subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event and executes the specified handler when the specified property has changed
        /// </summary>
        /// <typeparam name="TInstance">The type of the notifying object</typeparam>
        /// <typeparam name="TPropertyValue">The type of the property</typeparam>
        /// <param name="obj">The notifying object</param>
        /// <param name="propertyExpression">An expression indicating the property</param>
        /// <param name="onPropertyChanged">The handler</param>
        /// <returns>An action that will unsubscribe the handler from the <see cref="INotifyPropertyChanged.PropertyChanged"/> event</returns>
        public static Action OnPropertyChanged<TInstance, TPropertyValue>(this TInstance obj, Expression<Func<TInstance, TPropertyValue>> propertyExpression, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged =>
            OnPropertyChanged(obj, GetPropertyNameFromExpression(obj, propertyExpression), onPropertyChanged);

        /// <summary>
        /// Subscribes to the <see cref="INotifyPropertyChanging.PropertyChanging"/> event and executes the specified handler when the specified property is changing
        /// </summary>
        /// <typeparam name="TInstance">The type of the notifying object</typeparam>
        /// <typeparam name="TPropertyValue">The type of the property</typeparam>
        /// <param name="obj">The notifying object</param>
        /// <param name="propertyName">The name of the property</param>
        /// <param name="onPropertyChanging">The handler</param>
        /// <returns>An action that will unsubscribe the handler from the <see cref="INotifyPropertyChanging.PropertyChanging"/> event</returns>
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

        /// <summary>
        /// Subscribes to the <see cref="INotifyPropertyChanging.PropertyChanging"/> event and executes the specified handler when the specified property is changing
        /// </summary>
        /// <typeparam name="TInstance">The type of the notifying object</typeparam>
        /// <typeparam name="TPropertyValue">The type of the property</typeparam>
        /// <param name="obj">The notifying object</param>
        /// <param name="propertyExpression">An expression indicating the property</param>
        /// <param name="onPropertyChanging">The handler</param>
        /// <returns>An action that will unsubscribe the handler from the <see cref="INotifyPropertyChanging.PropertyChanging"/> event</returns>
        public static Action OnPropertyChanging<TInstance, TPropertyValue>(this TInstance obj, Expression<Func<TInstance, TPropertyValue>> propertyExpression, Action<TPropertyValue> onPropertyChanging) where TInstance : INotifyPropertyChanging =>
            OnPropertyChanging(obj, GetPropertyNameFromExpression(obj, propertyExpression), onPropertyChanging);
    }
}
