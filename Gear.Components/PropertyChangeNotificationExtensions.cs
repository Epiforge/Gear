using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace Gear.Components
{
    public static class PropertyChangeNotificationExtensions
    {
        static readonly ConcurrentDictionary<(Type type, string name), FastMethodInfo> fastPropertyGetters = new ConcurrentDictionary<(Type type, string name), FastMethodInfo>();

        static FastMethodInfo CreateFastGetter((Type type, string name) propertyDetails) => new FastMethodInfo(propertyDetails.type.GetRuntimeProperty(propertyDetails.name).GetMethod);

        public static Action OnPropertyChange<TInstance, TPropertyValue>(TInstance obj, string propertyName, Action<TPropertyValue> onPropertyChanging, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged, INotifyPropertyChanging
        {
            var fastGetter = fastPropertyGetters.GetOrAdd((typeof(TInstance), propertyName), CreateFastGetter);

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

            obj.PropertyChanged += propertyChanged;
            obj.PropertyChanging += propertyChanging;
            return () =>
            {
                obj.PropertyChanged -= propertyChanged;
                obj.PropertyChanging -= propertyChanging;
            };
        }

        public static Action OnPropertyChanged<TInstance, TPropertyValue>(TInstance obj, string propertyName, Action<TPropertyValue> onPropertyChanged) where TInstance : INotifyPropertyChanged
        {
            var fastGetter = fastPropertyGetters.GetOrAdd((typeof(TInstance), propertyName), CreateFastGetter);

            void propertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == propertyName)
                    onPropertyChanged((TPropertyValue)fastGetter.Invoke(obj));
            }

            obj.PropertyChanged += propertyChanged;
            return () => obj.PropertyChanged -= propertyChanged;
        }

        public static Action OnPropertyChanging<TInstance, TPropertyValue>(TInstance obj, string propertyName, Action<TPropertyValue> onPropertyChanging) where TInstance : INotifyPropertyChanging
        {
            var fastGetter = fastPropertyGetters.GetOrAdd((typeof(TInstance), propertyName), CreateFastGetter);

            void propertyChanging(object sender, PropertyChangingEventArgs e)
            {
                if (e.PropertyName == propertyName)
                    onPropertyChanging((TPropertyValue)fastGetter.Invoke(obj));
            }

            obj.PropertyChanging += propertyChanging;
            return () => obj.PropertyChanging -= propertyChanging;
        }
    }
}
