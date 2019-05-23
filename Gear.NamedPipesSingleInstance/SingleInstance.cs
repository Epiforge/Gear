using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gear.NamedPipesSingleInstance
{
    /// <summary>
    /// Represents an named-pipes-based implementation of single-instance.
    /// </summary>
    public class SingleInstance : IDisposable
    {
        static Task SendMessageAsync(PipeStream pipeStream, object message)
        {
            var encodedMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            return pipeStream.WriteAsync(encodedMessage, 0, encodedMessage.Length);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleInstance"/> class
        /// </summary>
        /// <param name="pipeName">The name of the pipe</param>
        /// <param name="onSecondaryInstanceMessageReceivedAsync">The synchronous method to invoke when a message is received from a secondary instance</param>
        public SingleInstance(string pipeName, Func<object, Task> onSecondaryInstanceMessageReceivedAsync)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            this.onSecondaryInstanceMessageReceivedAsync = onSecondaryInstanceMessageReceivedAsync ?? throw new ArgumentNullException(nameof(onSecondaryInstanceMessageReceivedAsync));
            try
            {
                serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                serverCancellationTokenSource = new CancellationTokenSource();
                IsFirstInstance = true;
                ThreadPool.QueueUserWorkItem(ServerLoop);
            }
            catch (IOException)
            {
                // nothing to see here
            }
        }

        /// <summary>
        /// Finalizes this object
        /// </summary>
        ~SingleInstance() => Dispose(false);

        readonly Func<object, Task> onSecondaryInstanceMessageReceivedAsync;
        readonly string pipeName;
        readonly CancellationTokenSource serverCancellationTokenSource;
        readonly NamedPipeServerStream serverStream;

        /// <summary>
        /// Occurs when the secondary instance message reception method throws an exception
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> SecondaryInstanceMessageReceptionUnhandledException;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees, releases, or resets unmanaged resources
        /// </summary>
        /// <param name="disposing"><c>false</c> if invoked by the finalizer because the object is being garbage collected; otherwise, <c>true</c></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                serverCancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Raises the <see cref="SecondaryInstanceMessageReceptionUnhandledException"/> event with specified arguments
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnSecondaryInstanceMessageReceptionUnhandledException(UnhandledExceptionEventArgs e) => SecondaryInstanceMessageReceptionUnhandledException?.Invoke(this, e);

        /// <summary>
        /// Sends a message to the currently connected other instance
        /// </summary>
        /// <param name="message">The message to be sent</param>
        /// <returns></returns>
        public async Task<object> SendMessageAsync(object message)
        {
            if (IsFirstInstance)
            {
                await SendMessageAsync(serverStream, message);
                return null;
            }
            try
            {
                using (var clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    await clientStream.ConnectAsync();
                    await SendMessageAsync(clientStream, message);
                    var buffer = new byte[4096];
                    var encodedMessage = new List<byte>();
                    do
                        encodedMessage.AddRange(buffer.Take(await clientStream.ReadAsync(buffer, 0, buffer.Length)));
                    while (!clientStream.IsMessageComplete);
                    return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(encodedMessage.ToArray()));
                }
            }
            catch
            {
                return null;
            }
        }

        async void ServerLoop(object state)
        {
            try
            {
                while (true)
                {
                    await serverStream.WaitForConnectionAsync(serverCancellationTokenSource.Token);
                    var buffer = new byte[4096];
                    var encodedMessage = new List<byte>();
                    do
                        encodedMessage.AddRange(buffer.Take(await serverStream.ReadAsync(buffer, 0, buffer.Length, serverCancellationTokenSource.Token)));
                    while (!serverStream.IsMessageComplete);
                    try
                    {
                        await onSecondaryInstanceMessageReceivedAsync(JsonConvert.DeserializeObject(Encoding.UTF8.GetString(encodedMessage.ToArray())));
                    }
                    catch (Exception ex)
                    {
                        OnSecondaryInstanceMessageReceptionUnhandledException(new UnhandledExceptionEventArgs(ex));
                    }
                    serverStream.Disconnect();
                }
            }
            catch (OperationCanceledException)
            {
                // closing
            }
            serverStream.Dispose();
        }

        /// <summary>
        /// Gets whether this instance is the first instance
        /// </summary>
        public bool IsFirstInstance { get; }
    }
}
