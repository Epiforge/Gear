using System;
using System.Collections.Generic;
using System.Linq;

namespace Gear.Components
{
    /// <summary>
    /// Represents an event that stores its event handlers with weak references
    /// </summary>
    public class WeakEventHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeakEventHandler"/> class
        /// </summary>
        /// <param name="sender">The object sending the event</param>
        public WeakEventHandler(object sender) => this.sender = sender;

        readonly object sender;
        readonly List<WeakReference<EventHandler>> weakEventHandlers = new List<WeakReference<EventHandler>>();
        readonly object weakEventHandlersAccess = new object();

        /// <summary>
        /// Removes all event handlers
        /// </summary>
        public void Clear() => weakEventHandlers.Clear();

        /// <summary>
        /// Raises the event
        /// </summary>
        /// <param name="e">The event's arguments</param>
        public void Raise(EventArgs e)
        {
            lock (weakEventHandlersAccess)
            {
                foreach (var eventHandler in weakEventHandlers.Select(weh => (exists: weh.TryGetTarget(out var eh), eh)).Where(t => t.exists).Select(t => t.eh))
                    eventHandler(sender, e);
            }
        }

        /// <summary>
        /// Subscribes an event handler
        /// </summary>
        /// <param name="eventHandler">The event handler</param>
        public void Subscribe(EventHandler eventHandler)
        {
            lock (weakEventHandlersAccess)
                weakEventHandlers.Add(new WeakReference<EventHandler>(eventHandler));
        }

        /// <summary>
        /// Unsubscribes an event handler
        /// </summary>
        /// <param name="eventHandler">The event handler</param>
        public void Unsubscribe(EventHandler eventHandler)
        {
            lock (weakEventHandlersAccess)
            {
                var firstIndex = weakEventHandlers.FindIndex(weh => weh.TryGetTarget(out var eh) && eventHandler == eh);
                if (firstIndex != -1)
                    weakEventHandlers.RemoveAt(firstIndex);
            }
        }
    }

    /// <summary>
    /// Represents an event that stores its event handlers with weak references
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event's arguments</typeparam>
    public class WeakEventHandler<TEventArgs> where TEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WeakEventHandler"/> class
        /// </summary>
        /// <param name="sender">The object sending the event</param>
        public WeakEventHandler(object sender) => this.sender = sender;

        readonly object sender;
        readonly List<WeakReference<EventHandler<TEventArgs>>> weakEventHandlers = new List<WeakReference<EventHandler<TEventArgs>>>();
        readonly object weakEventHandlersAccess = new object();

        /// <summary>
        /// Removes all event handlers
        /// </summary>
        public void Clear() => weakEventHandlers.Clear();

        /// <summary>
        /// Raises the event
        /// </summary>
        /// <param name="e">The event's arguments</param>
        public void Raise(TEventArgs e)
        {
            lock (weakEventHandlersAccess)
            {
                foreach (var eventHandler in weakEventHandlers.Select(weh => (exists: weh.TryGetTarget(out var eh), eh)).Where(t => t.exists).Select(t => t.eh))
                    eventHandler(sender, e);
            }
        }

        /// <summary>
        /// Subscribes an event handler
        /// </summary>
        /// <param name="eventHandler">The event handler</param>
        public void Subscribe(EventHandler<TEventArgs> eventHandler)
        {
            lock (weakEventHandlersAccess)
                weakEventHandlers.Add(new WeakReference<EventHandler<TEventArgs>>(eventHandler));
        }

        /// <summary>
        /// Unsubscribes an event handler
        /// </summary>
        /// <param name="eventHandler">The event handler</param>
        public void Unsubscribe(EventHandler<TEventArgs> eventHandler)
        {
            lock (weakEventHandlersAccess)
            {
                var firstIndex = weakEventHandlers.FindIndex(weh => weh.TryGetTarget(out var eh) && eventHandler == eh);
                if (firstIndex != -1)
                    weakEventHandlers.RemoveAt(firstIndex);
            }
        }
    }
}
