using System;

namespace msb.client.integration.beispiel
{
    using System;
    using System.Threading;
    using Fraunhofer.IPA.MSB.Client.API.Attributes;
    using Fraunhofer.IPA.MSB.Client.API.Configuration;
    using Fraunhofer.IPA.MSB.Client.API.Model;
    using Fraunhofer.IPA.MSB.Client.Websocket.Sample.Functions;
    using Fraunhofer.IPA.MSB.Client.Websocket.Sample.Events;
    using msb.separate.direct.tcp;
    using Serilog;
    using System.Collections.Generic;

    public static void 

    public class Program
    {
        public static void Main(string[] args)
        {
            // Setup logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:yyyy-MM-dd - HH:mm:ss}] [{SourceContext:s}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Sample application started");

            /* Seite A */
            const string MyMsbSmartObjectUuid = "1a17b5e3-3a6a-4e62-97b0-82cfdd1cc818";
            const string MyMsbSmartObjectName = "C# Sample SmartObject";
            const string MyMsbSmartObjectDescription = "{"+
            "\"Separate\":[{" +
                "\"Type\":\"TCP\"," +
                "\"Patterns\":{" +
                    "\"Events\":{" +
                        "\"EventId\":\"String\"," +
                        "\"Ip\":\"String\"," +
                        "\"Port\":\"Int\"" +
                    "}," +
                    "\"Functions\":{" +
                        "\"EventId\":\"String\"," +
                        "\"Ip\":\"String\"," +
                        "\"Port\":\"Int\"" +
                    "}" +
                "}" +
            "}]" +
            "}";
            const string MyMsbSmartObjectToken = "30e47482-c140-49a9-a79f-6f2396d8e0ab";

            SmartObject myMsbSmartObject = new SmartObject(MyMsbSmartObjectUuid, MyMsbSmartObjectName, MyMsbSmartObjectDescription, MyMsbSmartObjectToken);

            var tcp_pub1 = new TCPConfiguration.TCPPublicationInstruction() { EventId = "SimpleEventId", Ip = "127.0.0.1", Port = 1884 };
            var tcp_pub2 = new TCPConfiguration.TCPPublicationInstruction() { EventId = "FlatEventId", Ip = "127.0.0.1", Port = 1884 };
            var tcp_pub3 = new TCPConfiguration.TCPPublicationInstruction() { EventId = "ComplexEventId", Ip = "127.0.0.1", Port = 1884 };

            myMsbSmartObject.AddConfigurationParameter("msb.separate.direct.tcp.publications", new ConfigurationParameterValue(new List<Object>() {tcp_pub1, tcp_pub2, tcp_pub3 }));

            Event simpleEvent = new Event("SimpleEventId", "Name of simple event", "Event with simple data format", typeof(string));
            Event flatEvent = new Event("FlatEventId", "Name of flat event", "Event with flat data format", typeof(SimpleEvent));
            Event complexEvent = new Event("ComplexEventId", "Name of complex event", "Event with nested data format", typeof(ComplexEvent));
            myMsbSmartObject.AddEvent(simpleEvent);
            myMsbSmartObject.AddEvent(flatEvent);
            myMsbSmartObject.AddEvent(complexEvent);

            var config_myMsbSmartObject = new TCPConfiguration();
            config_myMsbSmartObject.publications = new System.Collections.Generic.Dictionary<string, TCPConfiguration.TCPPublicationInstruction>();

            var list_pub = (List<TCPConfiguration.TCPPublicationInstruction>)myMsbSmartObject.Configuration.Parameters["msb.separate.direct.tcp.publications"].Value;
            foreach (var cp in list_pub)
            {
                config_myMsbSmartObject.publications.Add(cp.EventId, cp);
            }

            var tcp_myMsbSmartObject = new TCPInterface(config_myMsbSmartObject);
            tcp_myMsbSmartObject.Start();

            /* Seite B */
            const string MyMsbApplicationUuid = "46441dc8-c3ab-4c93-9632-d1f356afb8ca";
            const string MyMsbApplicationName = "C# Sample Application";
            const string MyMsbApplicationDescription = "{" +
            "\"Separate\":[{" +
                "\"Type\":\"TCP\"," +
                "\"Patterns\":{" +
                    "\"Events\":{" +
                        "\"EventId\":\"String\"," +
                        "\"Ip\":\"String\"," +
                        "\"Port\":\"Int\"" +
                    "}," +
                    "\"Functions\":{" +
                        "\"EventId\":\"String\"," +
                        "\"Ip\":\"String\"," +
                        "\"Port\":\"Int\"" +
                    "}" +
                "}" +
            "}]" +
            "}";
            const string MyMsbApplicationToken = "5b6b273b-18ff-420b-bbff-5f40288c18f9";

            Application myMsbApplication = new Application(MyMsbApplicationUuid, MyMsbApplicationName, MyMsbApplicationDescription, MyMsbApplicationToken);

            SampleFunctionHandler simpleFunctions = new SampleFunctionHandler();
            myMsbSmartObject.AddFunctionHandler(simpleFunctions);

            var tcp_sub1 = new TCPConfiguration.TCPSubscriptionInstruction() { EventId = "SimpleEventId", Ip = "127.0.0.1", Port = 1884, FunctionPointer = null, IntegrationFlow = new System.Collections.Generic.Dictionary<string, string>() { { "SampleFunctionWithParameters", "TestString" } } };

            myMsbApplication.AddConfigurationParameter("msb.separate.direct.tcp.subscriptions", new ConfigurationParameterValue(tcp_sub1));

            var config_myMsbApplication = new TCPConfiguration();
            config_myMsbApplication.subscriptions = new System.Collections.Generic.Dictionary<string, TCPConfiguration.TCPSubscriptionInstruction>();
            var fPtr = msb.separate.Interfaces.BaseInterfaceUtils.CreateFunctionPointer(typeof(SampleFunctionHandler).GetMethod("EmptySampleFunction"), null);

            var list_sub = (List<TCPConfiguration.TCPSubscriptionInstruction>)myMsbSmartObject.Configuration.Parameters["msb.separate.direct.tcp.subscriptions"].Value;
            foreach (var cp in list_sub)
            {
                cp.FunctionPointer = fPtr;
                config_myMsbApplication.subscriptions.Add(cp.EventId, cp);
            }

            var tcp_myMsbApplication = new TCPInterface(config_myMsbApplication);
            tcp_myMsbApplication.Start();

            //starten
            const string MsbWebsocketInterfaceUrl = "ws://localhost:8085";
            var myMsbClient = new Fraunhofer.IPA.MSB.Client.Websocket.MsbClient(MsbWebsocketInterfaceUrl);

            myMsbClient.ConnectAsync().Wait();

            myMsbClient.RegisterAsync(myMsbSmartObject).Wait();
            myMsbClient.RegisterAsync(myMsbApplication).Wait();

            // Publish events
            while (true)
            {
                EventData eventData_SimpleEvent = new EventDataBuilder(simpleEvent).SetValue("TestString").Build();
                myMsbClient.PublishAsync(myMsbSmartObject, eventData_SimpleEvent).Wait();
                tcp_myMsbSmartObject.PublishEvent(new msb.separate.EventData() { Id = "SimpleEventId", Data = new System.Collections.Generic.Dictionary<string, object> { { "TestString", "123" } } });

                EventData eventData_FlatEvent = new EventDataBuilder(flatEvent).SetValue(new SimpleEvent()).Build();
                myMsbClient.PublishAsync(myMsbSmartObject, eventData_FlatEvent).Wait();

                EventData eventData_ComplexEvent = new EventDataBuilder(complexEvent).SetValue(new ComplexEvent()).Build();
                myMsbClient.PublishAsync(myMsbSmartObject, eventData_ComplexEvent).Wait();

                Thread.Sleep(3000);
            }
        }
    }
}
