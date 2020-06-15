using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace msb.separate.broker.mqtt.beispiel
{
    class Program
    {
        public static class funktionen
        {
            public static void funktion(string a, string b)
            {
                Console.WriteLine("Received: {0} und {1}", a, b);
            }
        }

        static void Main(string[] args)
        {
            var config = new MQTTConfiguration();
            config.publications.Add("instr1", new MQTTConfiguration.MQTTPublicationInstruction() { EventId = "testEvent", Ip = "127.0.0.1", Port = 1884 });

            var fPtr = msb.separate.Interfaces.BaseInterfaceUtils.CreateFunctionPointer(typeof(funktionen).GetMethod("funktion"), null);
            var intFlow = new System.Collections.Generic.Dictionary<string, string>() { { "a", "hallo" }, { "b", "hallo2" } };
            config.subscriptions.Add("instr1", new MQTTConfiguration.MQTTSubscriptionInstruction() { EventId = "testEvent", Ip = "127.0.0.1", Port = 1884, FunctionPointer = fPtr, IntegrationFlow = intFlow});

            var mqtt = new MQTTInterface(config);

            mqtt.Start();

            System.Threading.Thread.Sleep(1000);

            mqtt.PublishEvent(new msb.separate.EventData() { Id = "testEvent", Data = new System.Collections.Generic.Dictionary<string, object> { {"hallo", "123" }, { "hallo2", "321" } } });

            mqtt.Stop();

            Console.ReadLine();
        }
    }
}
