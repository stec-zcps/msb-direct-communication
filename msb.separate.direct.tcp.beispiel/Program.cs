using System;

namespace msb.separate.direct.tcp.beispiel
{
    class Program
    {
        public class SampleStruct
        {
            public String someString;
            public Int32 someInt;
        }

        public static class funktionen
        {
            public static void funktion(string a, string b)
            {
                Console.WriteLine("Received: {0} und {1}", a, b);
            }
        }

        static void Main(string[] args)
        {
            msb.separate.direct.tcp.TCPSubscriber s = new direct.tcp.TCPSubscriber("127.0.0.1", 9999);
            msb.separate.direct.tcp.TCPPublisher p = new direct.tcp.TCPPublisher("127.0.0.1", 9999, new System.Collections.Generic.List<string>() { "testEvent" });

            p.Start();

            var m = msb.separate.Interfaces.BaseInterfaceUtils.CreateFunctionPointer(typeof(funktionen).GetMethod("funktion"), null);
            s.AddSubscription("testEvent", new SubscriptionInstruction() { EventId = "testEvent", FunctionPointer = m, IntegrationFlow = new System.Collections.Generic.Dictionary<string, string>() { { "a", "hallo" }, { "b", "hallo2" } } });

            s.Connect();
            s.Listen();

            System.Threading.Thread.Sleep(1000);

            p.PublishEvent(new msb.separate.EventData() { Id = "testEvent", Data = new System.Collections.Generic.Dictionary<string, object> { { "hallo", "123" }, { "hallo2", "321" } } });

            Console.ReadLine();
        }
    }
}
