using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RB.SB.Core
{

    public class Receiver
    {
        public void Run()
        {
            var primaryQueueClient = new QueueClient(Configuration.PrimaryConnectionStringQueue, Configuration.PrimaryQueueName);
            var secondaryQueueClient = new QueueClient(Configuration.SecondaryConnectionStringQueue, Configuration.SecondaryQueueName);

            OnMessageAsync(
                    primaryQueueClient,
                    secondaryQueueClient,
                    async (m, customMessage ) => 
                        {
                            lock(this)
                            {
                                var originalColor = Console.ForegroundColor;
                                ConsoleColor textColor = !m.MessageId.Contains("PR") 
                                                            ? ConsoleColor.Yellow 
                                                            : m.MessageId.Contains("Active")
                                                                ? ConsoleColor.Green
                                                                : ConsoleColor.Red;
                                Console.ForegroundColor = textColor;
                                Console.Out.WriteLineAsync($"Message received: {customMessage} - {m.MessageId}");
                                Console.ForegroundColor = originalColor;

                            }                        
                        });
        }

        void OnMessageAsync(
            QueueClient primaryQueueClient,
            QueueClient secondaryQueueClient,
            Func<Message, string, Task> handlerCallback,
            int maxDeduplicationListLength = 4000)
        {
            var receivedMessageList = new List<string>();
            var receivedMessageListLock = new object();

            Func<int, Func<Message, string, Task>, Message, QueueClient, string, Task> callback = async (maxCount, fwd, message, queueClient, customMessage) =>
            {
                // Detect if a message with an identical ID has been received through the other queue.
                bool duplicate;
                lock (receivedMessageListLock)
                {
                    duplicate = receivedMessageList.Remove(message.MessageId);
                    if (!duplicate)
                    {
                        receivedMessageList.Add(message.MessageId);
                        if (receivedMessageList.Count > maxCount)
                        {
                            receivedMessageList.RemoveAt(0);
                        }
                    }
                }
                if (!duplicate)
                {
                    await fwd(message, customMessage);
                }
                else
                {
                    await queueClient.CompleteAsync(message.SystemProperties.LockToken);
                }
            };

            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 3,
                AutoComplete = false
            };

            primaryQueueClient.RegisterMessageHandler((msg, cancellationToken) => callback(maxDeduplicationListLength, handlerCallback, msg, primaryQueueClient, "Primary"), messageHandlerOptions);
            secondaryQueueClient.RegisterMessageHandler((msg, cancellationToken) => callback(maxDeduplicationListLength, handlerCallback, msg, secondaryQueueClient, "Secondary"), messageHandlerOptions);
        }

        Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }
    }
}
