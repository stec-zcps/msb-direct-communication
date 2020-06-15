using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using msb.separate.Interfaces;

namespace msb.separate.broker.mqtt
{
    public class MQTTInterface : BaseInterface
    {
        private MQTTConfiguration configuration;

        private List<MQTTPubSub> subInterfaces;

        public MQTTInterface(MQTTConfiguration config)
        {
            this.configuration = config;

            List<String> events = new List<string>();
            foreach (var p in config.publications) events.Add(p.Value.EventId);

            List<KeyValuePair<String, UInt16>> connections = new List<KeyValuePair<string, ushort>>();

            foreach (var s in config.subscriptions)
            {
                if (!connections.Exists(e => e.Key == s.Value.Ip))
                {
                    connections.Add(new KeyValuePair<string, ushort>(s.Value.Ip, s.Value.Port));
                }
            }

            foreach (var s in config.publications)
            {
                if (!connections.Exists(e => e.Key == s.Value.Ip))
                {
                    connections.Add(new KeyValuePair<string, ushort>(s.Value.Ip, s.Value.Port));
                }
            }

            if (connections.Count != 0)
            {
                subInterfaces = new List<MQTTPubSub>();

                foreach (var s in connections)
                {
                    var sub = new MQTTPubSub(s.Key, s.Value);
                    var subs = config.subscriptions.Where(e => e.Value.Ip == s.Key && e.Value.Port == s.Value);

                    foreach (var s_ in subs) sub.AddSubscription(s_.Key, s_.Value);

                    subInterfaces.Add(sub);
                }
            }
        }

        public void Start()
        {
            foreach (var s in subInterfaces) s.Connect();
        }

        public void Stop()
        {
            foreach (var s in subInterfaces) s.Disconnect();
        }
    }

    public class MQTTConfiguration
    {
        public class MQTTSubscriptionInstruction : SubscriptionInstruction
        {
            public string Ip;
            public UInt16 Port;
        }

        public class MQTTPublicationInstruction : PublicationInstruction
        {
            public string Ip;
            public UInt16 Port;
        }

        public Dictionary<String, MQTTSubscriptionInstruction> subscriptions;
        public Dictionary<String, MQTTPublicationInstruction> publications;
    }
    public class MQTTPubSub : Interfaces.BaseSubscriber, Interfaces.BasePublisher
    {
        private MQTTnet.Client.IMqttClient mqttClient;

        private readonly string Ip;
        private readonly UInt16 Port;
        private Dictionary<String, SubscriptionInstruction> Integrationen;

        public MQTTPubSub(string brokerIp, UInt16 brokerPort, Dictionary<string, SubscriptionInstruction> subs = null)
        {
            Ip = brokerIp;
            Port = brokerPort;

            if (subs != null) Integrationen = subs;
        }

        public bool Connect()
        {
            try
            {
                var factory = new MQTTnet.MqttFactory();
                mqttClient = factory.CreateMqttClient();

                var options = new MQTTnet.Client.Options.MqttClientOptionsBuilder()
                    .WithTcpServer(this.Ip, this.Port)
                    .Build();

                mqttClient.ConnectAsync(options, System.Threading.CancellationToken.None);

                mqttClient.ConnectedHandler = new MQTTnet.Client.Connecting.MqttClientConnectedHandlerDelegate(e =>
                {
                    if(Integrationen != null) MakeSubscriptions();
                });

                mqttClient.ApplicationMessageReceivedHandler = new MQTTnet.Client.Receiving.MqttApplicationMessageReceivedHandlerDelegate(e =>
                {
                    String msg = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    var deserializedData = JsonConvert.DeserializeObject<EventData>(msg);
                    deserializedData.Id = e.ApplicationMessage.Topic;

                    foreach (var s in Integrationen)
                    {
                        if (s.Value.EventId == deserializedData.Id)
                        {
                            var pointer = s.Value.FunctionPointer;
                            var parameters = pointer.Method.GetParameters();
                            var parameterArrayForInvoke = new object[parameters.Length];

                            foreach (var eintrag in s.Value.IntegrationFlow)
                            {
                                int currentParameterCallIndex = 0;
                                for (; currentParameterCallIndex < parameters.Length; currentParameterCallIndex++)
                                {
                                    if (parameters[currentParameterCallIndex].Name == eintrag.Key)
                                    {
                                        var currentParameterCallType = pointer.Method.GetParameters()[currentParameterCallIndex].ParameterType;
                                        break;
                                    }
                                }

                                Object deserializedParameter = null;
                                deserializedParameter = deserializedData.Data[eintrag.Value];

                                parameterArrayForInvoke[currentParameterCallIndex] = deserializedParameter;
                            }

                            pointer.DynamicInvoke(parameterArrayForInvoke);
                        }
                    }
                });
            }
            catch
            {
                return false;
            }

            return true;
        }

        public void Disconnect()
        {
            mqttClient.DisconnectAsync(new MQTTnet.Client.Disconnecting.MqttClientDisconnectOptions(), System.Threading.CancellationToken.None);
        }

        public bool AddSubscription(string id, SubscriptionInstruction instr)
        {
            if (Integrationen.ContainsKey(id))
                return false;

            Integrationen.Add(id, instr);

            return true;
        }

        public void MakeSubscriptions()
        {
            foreach (var s in Integrationen)
            {
                mqttClient.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptionsBuilder().WithTopicFilter(s.Value.EventId).Build(), System.Threading.CancellationToken.None);
            }
        }

        public bool PublishEvent(EventData eventToPublish)
        {
            if (!mqttClient.IsConnected) return false;

            var s = Newtonsoft.Json.JsonConvert.SerializeObject(eventToPublish);

            var applicationMessage = new MQTTnet.MqttApplicationMessageBuilder()
                        .WithTopic(eventToPublish.Id)
                        .WithPayload(s)
                        .Build();

            mqttClient.PublishAsync(applicationMessage, System.Threading.CancellationToken.None);

            return true;
        }
    }
}