using Microsoft.Azure.ServiceBus;
using System;
using System.Threading.Tasks;

namespace RB.SB.Core
{
    public class SenderPassiveReplication
    {
        readonly object swapMutex = new object();
        QueueClient primaryQueueClient;
        QueueClient secondaryQueueClient;
        bool primary = true;

        public async void Run()
        {
            primaryQueueClient = new QueueClient(Configuration.PrimaryConnectionStringQueue, Configuration.PrimaryQueueName);
            secondaryQueueClient = new QueueClient(Configuration.SecondaryConnectionStringQueue, Configuration.SecondaryQueueName);

#pragma warning disable 4014
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                    if (primaryQueueClient.IsClosedOrClosing)
                    {
                        primaryQueueClient = new QueueClient(Configuration.PrimaryConnectionStringQueue, Configuration.PrimaryQueueName);
                        secondaryQueueClient = new QueueClient(Configuration.SecondaryConnectionStringQueue, Configuration.SecondaryQueueName);
                    }
                    else
                    {
                        primaryQueueClient.CloseAsync();
                    }                    
                }
            });
#pragma warning restore 4014

            int i = 1;
            while(true)
            {
                // Create brokered message.
                var message = new Message()
                {
                    MessageId = (primary? "PR Active " : " PR Passive ") + i.ToString() + "-" + DateTime.Now.ToString(),
                    TimeToLive = TimeSpan.FromMinutes(2.0)
                };
                var m1 = message;

                try
                {
                    await SendMessage(m1);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to send to primary or secondary queue: Exception {0}", e);
                }
                i++;
            }
        }

        async Task SendMessage(Message m1, int maxSendRetries = 2)
        {
            do
            {
                Message m2 = m1.Clone();
                try
                {
                    await primaryQueueClient.SendAsync(m1);
                    Console.WriteLine("Message send: " + m1.MessageId);
                    return;
                }
                catch
                {
                    if (--maxSendRetries <= 0)
                    {
                        throw;
                    }

                    lock (this.swapMutex)
                    {
                        var c = primaryQueueClient;
                        this.primaryQueueClient = this.secondaryQueueClient;
                        this.secondaryQueueClient = c;
                        primary = !primary;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Switch to the seccond channel.");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    m1 = m2.Clone();
                }
            }
            while (true);
        }


    }
}
