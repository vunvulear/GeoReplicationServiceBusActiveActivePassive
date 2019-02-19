using Microsoft.Azure.ServiceBus;
using System;
using System.Threading;

namespace RB.SB.Core
{
    public class SenderActiveReplication
    {
        readonly object swapMutex = new object();
        QueueClient primaryQueueClient;
        QueueClient secondaryQueueClient;
        readonly bool primary = true;

        public async void Run()
        {
            primaryQueueClient = new QueueClient(Configuration.PrimaryConnectionStringQueue, Configuration.PrimaryQueueName);
            secondaryQueueClient = new QueueClient(Configuration.SecondaryConnectionStringQueue, Configuration.SecondaryQueueName);

            int i = 1;
            while (true)
            {
                try
                {
                    Message m1 = new Message()
                    {
                        MessageId = i.ToString() + "-" + DateTime.Now.ToString(),
                        TimeToLive = TimeSpan.FromMinutes(2.0)
                    };
                    var m2 = m1.Clone();

                    var exceptionCount = 0;

                    try
                    {
                        await primaryQueueClient.SendAsync(m1);
                        Console.WriteLine("Message {0} sent to primary queue",m1.MessageId);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("Unable to send message {0} to primary queue: Exception {1}", m1.MessageId, e);
                        exceptionCount++;
                    }

                    try
                    {
                        await secondaryQueueClient.SendAsync(m2);
                        Console.WriteLine("Message {0} sent to secondary queue", m1.MessageId);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to send message {0} to secondary queue: Exception {1}", m2.MessageId, e);
                        exceptionCount++;
                    }

                    if (exceptionCount > 1)
                    {
                        throw new Exception("Send Failure");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception {0}", e);
                    throw e;
                }

                Thread.Sleep(TimeSpan.FromSeconds(1.00));

                i++;
            }
        }
    }
}
