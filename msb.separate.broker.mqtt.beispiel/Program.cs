using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace msb.separate.broker.mqtt.beispiel
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
            msb.separate.broker.mqtt.MQTTSubscriber s = new MQTTSubscriber("127.0.0.1", 1884);
            msb.separate.broker.mqtt.MQTTPublisher p = new MQTTPublisher("127.0.0.1", 1884);

            var m = msb.separate.Interfaces.BaseInterfaceUtils.CreateFunctionPointer(typeof(funktionen).GetMethod("funktion"), null);
            s.AddSubscription("testEvent", new SubscriptionInstruction() { EventId = "testEvent", FunctionPointer = m, IntegrationFlow = new System.Collections.Generic.Dictionary<string, string>() { { "a", "hallo" }, { "b", "hallo2" } } });

            p.Connect();
            s.Connect();

            System.Threading.Thread.Sleep(1000);

            p.PublishEvent(new msb.separate.EventData() { Id = "testEvent", Data = new System.Collections.Generic.Dictionary<string, object> { {"hallo", "123" }, { "hallo2", "321" } } });

            Console.ReadLine();
        }
    }
}
