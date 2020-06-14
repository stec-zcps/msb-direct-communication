using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using Newtonsoft.Json;
using static msb.separate.Interfaces.BaseInterfaceUtils;

namespace msb.separate.broker.mqtt
{
    public class MQTTSubscriber : Interfaces.BaseSubscriber
    {
        private MQTTnet.Client.IMqttClient mqttClient;

        private readonly string Ip;
        private readonly UInt16 Port;
        private Dictionary<String, SubscriptionInstruction> Integrationen;

        public MQTTSubscriber(string brokerIp, UInt16 brokerPort)
        {
            Ip = brokerIp;
            Port = brokerPort;

            Integrationen = new Dictionary<string, SubscriptionInstruction>();
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
                    MakeSubscriptions();                
                });

                mqttClient.ApplicationMessageReceivedHandler = new MQTTnet.Client.Receiving.MqttApplicationMessageReceivedHandlerDelegate(e =>
                {
                    String msg = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var deserializedData = JsonConvert.DeserializeObject<EventData>(msg);
                    deserializedData.Id = e.ApplicationMessage.Topic;

                    foreach(var s in Integrationen)
                    {
                        if (s.Value.EventId == deserializedData.Id)
                        {
                            var pointer = s.Value.fPointer;
                            var parameters = pointer.Method.GetParameters();
                            var parameterArrayForInvoke = new object[parameters.Length];

                            foreach (var eintrag in s.Value.paramMapping)
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
    }

    public class MQTTPublisher : Interfaces.BasePublisher
    {
        private MQTTnet.Client.IMqttClient mqttClient;

        private readonly string Ip;
        private readonly UInt16 Port;

        public MQTTPublisher(string brokerIp, UInt16 brokerPort)
        {
            Ip = brokerIp;
            Port = brokerPort;
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
            }
            catch
            {
                return false;
            }

            return true;
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
