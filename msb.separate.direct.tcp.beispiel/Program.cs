using System;

namespace msb.separate.direct.tcp.beispiel
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
            var config = new TCPConfiguration();
            config.publications = new System.Collections.Generic.Dictionary<string, TCPConfiguration.TCPPublicationInstruction>();
            config.subscriptions = new System.Collections.Generic.Dictionary<string, TCPConfiguration.TCPSubscriptionInstruction>();

            config.publications.Add("instr1", new TCPConfiguration.TCPPublicationInstruction() { EventId = "testEvent", Ip = "127.0.0.1", Port = 1884 });

            var fPtr = msb.separate.Interfaces.BaseInterfaceUtils.CreateFunctionPointer(typeof(funktionen).GetMethod("funktion"), null);
            var intFlow = new System.Collections.Generic.Dictionary<string, string>() { { "a", "hallo" }, { "b", "hallo2" } };
            config.subscriptions.Add("instr1", new TCPConfiguration.TCPSubscriptionInstruction() { EventId = "testEvent", Ip = "127.0.0.1", Port = 1884, FunctionPointer = fPtr, IntegrationFlow = intFlow });

            var tcp = new TCPInterface(config);

            tcp.Start();

            System.Threading.Thread.Sleep(1000);

            tcp.PublishEvent(new msb.separate.EventData() { Id = "testEvent", Data = new System.Collections.Generic.Dictionary<string, object> { { "hallo", "123" }, { "hallo2", "321" } } });

            System.Threading.Thread.Sleep(1000);

            tcp.Stop();

            Console.ReadLine();
        }
    }
}
