using Gear.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

namespace Gear.ActiveExpressions
{
    class ActiveNewExpression : ActiveExpression, IEquatable<ActiveNewExpression>
    {
        static readonly object instanceManagementLock = new object();
        static readonly Dictionary<(Type type, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions options), ActiveNewExpression> instances = new Dictionary<(Type type, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions options), ActiveNewExpression>();

        public static bool operator ==(ActiveNewExpression a, ActiveNewExpression b) => a?.Equals(b) ?? b is null;

        public static bool operator !=(ActiveNewExpression a, ActiveNewExpression b) => !(a == b);

        public static ActiveNewExpression Create(NewExpression newExpression, ActiveExpressionOptions options)
        {
            var type = newExpression.Type;
            var arguments = new EquatableList<ActiveExpression>(newExpression.Arguments.Select(argument => Create(argument, options)).ToList());
            var key = (type, arguments, options);
            lock (instanceManagementLock)
            {
                if (!instances.TryGetValue(key, out var activeNewExpression))
                {
                    activeNewExpression = new ActiveNewExpression(type, arguments, options);
                    instances.Add(key, activeNewExpression);
                }
                ++activeNewExpression.disposalCount;
                return activeNewExpression;
            }
        }

        ActiveNewExpression(Type type, EquatableList<ActiveExpression> arguments, ActiveExpressionOptions options) : base(type, ExpressionType.New, options)
        {
            this.arguments = arguments;
            constructorParameterTypes = new EquatableList<Type>(this.arguments.Select(argument => argument.Type).ToList());
            foreach (var argument in this.arguments)
                argument.PropertyChanged += ArgumentPropertyChanged;
            Evaluate();
        }

        readonly EquatableList<ActiveExpression> arguments;
        readonly EquatableList<Type> constructorParameterTypes;
        int disposalCount;

        void ArgumentPropertyChanged(object sender, PropertyChangedEventArgs e) => Evaluate();

        protected override bool Dispose(bool disposing)
        {
            var result = false;
            lock (instanceManagementLock)
            {
                if (--disposalCount == 0)
                {
                    foreach (var argument in arguments)
                    {
                        argument.PropertyChanged -= ArgumentPropertyChanged;
                        argument.Dispose();
                    }
                    instances.Remove((Type, arguments, options));
                    result = true;
                }
            }
            if (result)
                DisposeValueIfNecessary();
            return result;
        }

        void DisposeValueIfNecessary()
        {
            if (ApplicableOptions.IsConstructedTypeDisposed(Type, constructorParameterTypes))
            {
                var value = Value;
                try
                {
                    if (value is IDisposable disposable)
                        disposable.Dispose();
                    else if (value is IAsyncDisposable asyncDisposable)
                        asyncDisposable.DisposeAsync().Wait();
                }
                catch (Exception ex)
                {
                    throw new Exception("Disposal of constructed object failed", ex);
                }
            }
        }

        public override bool Equals(object obj) => Equals(obj as ActiveNewExpression);

        public bool Equals(ActiveNewExpression other) => other?.Type == Type && other?.arguments == arguments && other?.options == options;

        void Evaluate()
        {
            try
            {
                DisposeValueIfNecessary();
                var argumentFault = arguments.Select(argument => argument.Fault).Where(fault => fault != null).FirstOrDefault();
                if (argumentFault != null)
                    Fault = argumentFault;
                else
                    Value = Activator.CreateInstance(Type, arguments.Select(argument => argument.Value).ToArray());
            }
            catch (Exception ex)
            {
                Fault = ex;
            }
        }

        public override int GetHashCode() => HashCodes.CombineObjects(typeof(ActiveNewExpression), Type, arguments, options);
    }
}
