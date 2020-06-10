using DirectCommunication.Interfaces;
using System;
using static DirectCommunication.Interfaces.BaseInterfaceUtils;

namespace DirectCommunication
{
    class Program
    {
        public class SampleStruct
        {
            public String someString;
            public Int32 someInt;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            TCPInterfaceParameter server1Config = new TCPInterfaceParameter() { ClientHost = "127.0.0.1", ClientPort = 8888, ServerHost = "127.0.0.1", ServerPort = 9999 };
            TCPInterfaceParameter server2Config = new TCPInterfaceParameter() { ClientHost = "127.0.0.1", ClientPort = 9999, ServerHost = "127.0.0.1", ServerPort = 8888 };

            TCPInterface server1 = new TCPInterface(server1Config);
            TCPInterface server2 = new TCPInterface(server2Config);

            server1.Connect();
            server2.Connect();

            server1.AddSubscription<SampleStruct>("testEvent", new BaseInterfaceUtils.SubscriptionReceivedCallback<SampleStruct>((dataStuff) =>
            {
                Console.WriteLine("Received: " + dataStuff.Data.someString);
            }));

            server2.PublishEvent<SampleStruct>(new DirectCommunication.EventData<SampleStruct>() { Id = "testEvent", Data = new SampleStruct() { someString = "some test data" } });
            Console.ReadLine();
        }
    }
}
