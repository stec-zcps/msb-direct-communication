using System;

namespace msb.separate.beispiel
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
            /*Console.WriteLine("Hello World!");

            TCPInterfaceParameter server1Config = new TCPInterfaceParameter() { ClientHost = "127.0.0.1", ClientPort = 8888, ServerHost = "127.0.0.1", ServerPort = 9999 };
            TCPInterfaceParameter server2Config = new TCPInterfaceParameter() { ClientHost = "127.0.0.1", ClientPort = 9999, ServerHost = "127.0.0.1", ServerPort = 8888 };

            var server1 = new msb.separate.direct.tcp.TCPInterface(server1Config);
            var server2 = new msb.separate.direct.tcp.TCPInterface(server2Config);

            server1.Connect();
            server2.Connect();

            server1.AddSubscription<SampleStruct>("testEvent", new msb.separate.Interfaces.BaseInterfaceUtils.SubscriptionReceivedCallback<SampleStruct>((dataStuff) =>
            {
                Console.WriteLine("Received: " + dataStuff.Data.someString);
            }));

            server2.PublishEvent<SampleStruct>(new msb.separate.EventData<SampleStruct>() { Id = "testEvent", Data = new SampleStruct() { someString = "some test data" } });
            Console.ReadLine();*/

            msb.separate.direct.tcp.TCPSubscriber s = new direct.tcp.TCPSubscriber("127.0.0.1", 9999);
            msb.separate.direct.tcp.TCPPublisher p = new direct.tcp.TCPPublisher("127.0.0.1", 9999, new System.Collections.Generic.List<string>() { "testEvent" });

            p.Start();

            s.Connect();
            s.AddSubscription<SampleStruct>("testEvent", new msb.separate.Interfaces.BaseInterfaceUtils.SubscriptionReceivedCallback<SampleStruct>((dataStuff) =>
            {
                Console.WriteLine("Received: " + dataStuff.Data.someString);
            }));
            s.MakeSubscriptions();
            s.Listen();

            System.Threading.Thread.Sleep(2000);

            p.PublishEvent<SampleStruct>(new msb.separate.EventData<SampleStruct>() { Id = "testEvent", Data = new SampleStruct() { someString = "some test data" } });

            Console.ReadLine();
        }
    }
}
