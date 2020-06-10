using System;
using Newtonsoft.Json;
using static msb.separate.Interfaces.BaseInterfaceUtils;

namespace msb.separate.broker.mqtt
{
    public class MQTTSubscriber : Interfaces.BaseSubscriber
    {
        private MQTTnet.Client.IMqttClient mqttClient;

        private readonly string Ip;
        private readonly UInt16 Port;
        private GenericDictionary _subscriptionMapping;

        public MQTTSubscriber(string brokerIp, UInt16 brokerPort)
        {
            Ip = brokerIp;
            Port = brokerPort;

            _subscriptionMapping = new GenericDictionary();
        }

        public bool Connect()
        {
            try
            {
                var factory = new MQTTnet.MqttFactory();
                mqttClient = factory.CreateMqttClient();

                var options = new MQTTnet.Client.Options.MqttClientOptionsBuilder()
                    .WithTcpServer(this.Ip + ":" + Port.ToString())
                    .Build();

                mqttClient.ConnectAsync(options, System.Threading.CancellationToken.None);

                mqttClient.ApplicationMessageReceivedHandler = new MQTTnet.Client.Receiving.MqttApplicationMessageReceivedHandlerDelegate(e =>
                {
                    String msg = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    var deserializedData = JsonConvert.DeserializeObject<EventData<object>>(msg);
                    deserializedData.Id = e.ApplicationMessage.Topic;

                    if (!this._subscriptionMapping.ContainsKey(deserializedData.Id))
                    {
                        System.Diagnostics.Debug.WriteLine("Warning: Unknown subscription id: " + deserializedData.Id);
                        return;
                    }

                    var callbackAsObjectType = this._subscriptionMapping.GetValue<object>(deserializedData.Id);
                    var genericArgumentType = callbackAsObjectType.GetType().GenericTypeArguments[0];

                    var genericCallbackObjectType = typeof(SubscriptionReceivedCallback<>);
                    var realType = genericCallbackObjectType.MakeGenericType(genericArgumentType);

                    var realTypeDelegate = Convert.ChangeType(callbackAsObjectType, realType);
                    var realTypeDelegateReal = realTypeDelegate as Delegate;

                    var deserializedDataWithRealType = JsonConvert.DeserializeObject(msg, typeof(EventData<>).MakeGenericType(genericArgumentType));

                    // TODO: Start in own thread? Or is aync enough anyway?
                    realTypeDelegateReal.DynamicInvoke(deserializedDataWithRealType);
                });
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool AddSubscription<T>(string id, Interfaces.BaseInterfaceUtils.SubscriptionReceivedCallback<T> callback)
        {
            if (_subscriptionMapping.ContainsKey(id))
                return false;

            _subscriptionMapping.Add(id, callback);

            return true;
        }

        public void MakeSubscriptions()
        {
            foreach (var s in _subscriptionMapping.Keys())
            {                
                mqttClient.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptionsBuilder().WithTopicFilter(s).Build(), System.Threading.CancellationToken.None);
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
                    .WithTcpServer(this.Ip + ":" + Port.ToString())
                    .Build();

                mqttClient.ConnectAsync(options, System.Threading.CancellationToken.None);                
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool PublishEvent<T>(EventData<T> eventToPublish)
        {
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
