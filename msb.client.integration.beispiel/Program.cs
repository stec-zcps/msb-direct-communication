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

            const string MsbWebsocketInterfaceUrl = "ws://localhost:8085";

            const string MyMsbSmartObjectUuid = "1a17b5e3-3a6a-4e62-97b0-82cfdd1cc818";
            const string MyMsbSmartObjectName = "C# Sample SmartObject";
            const string MyMsbSmartObjectDescription = "Description of my C# sample SmartObject";
            const string MyMsbSmartObjectToken = "30e47482-c140-49a9-a79f-6f2396d8e0ab";

            const string MyMsbApplicationUuid = "46441dc8-c3ab-4c93-9632-d1f356afb8ca";
            const string MyMsbApplicationName = "C# Sample Application";
            const string MyMsbApplicationDescription = "Description of my C# sample Application";
            const string MyMsbApplicationToken = "5b6b273b-18ff-420b-bbff-5f40288c18f9";

            // Create a new MsbClient which allows SmartObjects and Applications to communicate with the MSB
            var myMsbClient = new Fraunhofer.IPA.MSB.Client.Websocket.MsbClient(MsbWebsocketInterfaceUrl);

            // Create the self description of a sample SmartObject and a sample Application
            SmartObject myMsbSmartObject = new SmartObject(MyMsbSmartObjectUuid, MyMsbSmartObjectName, MyMsbApplicationDescription, MyMsbSmartObjectToken);
            Application myMsbApplication = new Application(MyMsbApplicationUuid, MyMsbApplicationName, MyMsbSmartObjectDescription, MyMsbApplicationToken);

            // Add configuration parameters
            myMsbSmartObject.AddConfigurationParameter("sampleParameter1", new ConfigurationParameterValue(1337));
            myMsbSmartObject.AddConfigurationParameter("sampleParameter2", new ConfigurationParameterValue("SampleValue"));

            // Add events
            Event simpleEvent = new Event("SimpleEventId", "Name of simple event", "Event with simple data format", typeof(string));
            Event flatEvent = new Event("FlatEventId", "Name of flat event", "Event with flat data format", typeof(SimpleEvent));
            Event complexEvent = new Event("ComplexEventId", "Name of complex event", "Event with nested data format", typeof(ComplexEvent));
            myMsbApplication.AddEvent(simpleEvent);
            myMsbApplication.AddEvent(flatEvent);
            myMsbApplication.AddEvent(complexEvent);

            // Add functions
            SampleFunctionHandler simpleFunctions = new SampleFunctionHandler();
            myMsbSmartObject.AddFunctionHandler(simpleFunctions);

            var config_myMsbSmartObject = new TCPConfiguration();
            config_myMsbSmartObject.publications = new System.Collections.Generic.Dictionary<string, TCPConfiguration.TCPPublicationInstruction>();
            config_myMsbSmartObject.publications.Add("SimpleEventId", new TCPConfiguration.TCPPublicationInstruction() { EventId = "SimpleEventId", Ip = "127.0.0.1", Port = 1884 });
            config_myMsbSmartObject.publications.Add("FlatEventId", new TCPConfiguration.TCPPublicationInstruction() { EventId = "FlatEventId", Ip = "127.0.0.1", Port = 1884 });
            config_myMsbSmartObject.publications.Add("ComplexEventId", new TCPConfiguration.TCPPublicationInstruction() { EventId = "ComplexEventId", Ip = "127.0.0.1", Port = 1884 });
            var tcp_myMsbSmartObject = new TCPInterface(config_myMsbSmartObject);
            tcp_myMsbSmartObject.Start();
            
            var config_myMsbApplication = new TCPConfiguration();
            config_myMsbApplication.subscriptions = new System.Collections.Generic.Dictionary<string, TCPConfiguration.TCPSubscriptionInstruction>();
            var fPtr = msb.separate.Interfaces.BaseInterfaceUtils.CreateFunctionPointer(typeof(SampleFunctionHandler).GetMethod("EmptySampleFunction"), null);
            var intFlow = new System.Collections.Generic.Dictionary<string, string>() { { "SampleFunctionWithParameters", "TestString" } };
            config_myMsbApplication.subscriptions.Add("SimpleEventId", new TCPConfiguration.TCPSubscriptionInstruction() { EventId = "SimpleEventId", Ip = "127.0.0.1", Port = 1884, FunctionPointer = fPtr, IntegrationFlow = intFlow });
            var tcp_myMsbApplication = new TCPInterface(config_myMsbApplication);
            tcp_myMsbApplication.Start();

            // Connect to the MSB and register the sample SmartObject and sample Application via the MsbClient
            myMsbClient.ConnectAsync().Wait();

            myMsbClient.RegisterAsync(myMsbSmartObject).Wait();
            myMsbClient.RegisterAsync(myMsbApplication).Wait();

            // Publish events
            while (true)
            {
                EventData eventData_SimpleEvent = new EventDataBuilder(simpleEvent).SetValue("TestString").Build();
                myMsbClient.PublishAsync(myMsbSmartObject, eventData_SimpleEvent).Wait();
                tcp_myMsbSmartObject.PublishEvent(new msb.separate.EventData() { Id = "SimpleEventId", Data = new System.Collections.Generic.Dictionary<string, object> { { "TestString", "123" } });

                EventData eventData_FlatEvent = new EventDataBuilder(flatEvent).SetValue(new SimpleEvent()).Build();
                myMsbClient.PublishAsync(myMsbSmartObject, eventData_FlatEvent).Wait();

                EventData eventData_ComplexEvent = new EventDataBuilder(complexEvent).SetValue(new ComplexEvent()).Build();
                myMsbClient.PublishAsync(myMsbSmartObject, eventData_ComplexEvent).Wait();

                Thread.Sleep(3000);
            }
        }
    }
}
