using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json;
using static msb.separate.Interfaces.BaseInterfaceUtils;

namespace msb.separate.direct.tcp
{
    public class TCPSubscriber : Interfaces.BaseSubscriber
    {
        private TcpClient tcpClient;

        private readonly string Ip;
        private readonly UInt16 Port;
        private GenericDictionary _subscriptionMapping;

        private byte[] buffer = new byte[1024];

        public TCPSubscriber(string localIp, UInt16 localPort)
        {
            Ip = localIp;
            Port = localPort;

            _subscriptionMapping = new GenericDictionary();
        }

        public bool Connect()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(Ip, Port);
            }
            catch
            {
                return false;
            }
            return tcpClient.Connected;
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
            foreach(var s in _subscriptionMapping.Keys())
            {
                var d = new SubscriptionParameter() { EventId = s };
                var j = Newtonsoft.Json.JsonConvert.SerializeObject(d);
                var b = System.Text.ASCIIEncoding.ASCII.GetBytes(j);
                tcpClient.GetStream().Write(b, 0, b.Length);
            }
        }

        public void Listen()
        {
            tcpClient.GetStream().BeginRead(buffer, 0, buffer.Length, ListenCallback, buffer);
        }

        private void ListenCallback(IAsyncResult result)
        {
            String messageBufferAsUnicode = Encoding.ASCII.GetString(buffer);
            messageBufferAsUnicode = messageBufferAsUnicode.Trim('\0');

            tcpClient.GetStream().BeginRead(buffer, 0, buffer.Length, ListenCallback, buffer);

            var deserializedData = JsonConvert.DeserializeObject<EventData<object>>(messageBufferAsUnicode);

            if (!this._subscriptionMapping.ContainsKey(deserializedData.Id))
            {
                Debug.WriteLine("Warning: Unknown subscription id: " + deserializedData.Id);
                return;
            }

            var callbackAsObjectType = this._subscriptionMapping.GetValue<object>(deserializedData.Id);
            var genericArgumentType = callbackAsObjectType.GetType().GenericTypeArguments[0];

            var genericCallbackObjectType = typeof(SubscriptionReceivedCallback<>);
            var realType = genericCallbackObjectType.MakeGenericType(genericArgumentType);

            var realTypeDelegate = Convert.ChangeType(callbackAsObjectType, realType);
            var realTypeDelegateReal = realTypeDelegate as Delegate;

            var deserializedDataWithRealType = JsonConvert.DeserializeObject(messageBufferAsUnicode, typeof(EventData<>).MakeGenericType(genericArgumentType));

            // TODO: Start in own thread? Or is aync enough anyway?
            realTypeDelegateReal.DynamicInvoke(deserializedDataWithRealType);
        }
    }

    public class TCPPublisher : Interfaces.BasePublisher
    {
        private TcpListener tcpListener;

        private readonly string Ip;
        private readonly UInt16 Port;

        private List<TcpClient> Subscribers;
        private Dictionary<TcpClient, byte[]> SubscriberBuffer;
        private Dictionary<String, List<TcpClient>> TopicSubscriberlist;

        public TCPPublisher(string localIp, UInt16 localPort, List<string> topics)
        {
            Ip = localIp;
            Port = localPort;
            Subscribers = new List<TcpClient>();
            SubscriberBuffer = new Dictionary<TcpClient, byte[]>();
            TopicSubscriberlist = new Dictionary<string, List<TcpClient>>();

            foreach (var t in topics)
            {
                TopicSubscriberlist.Add(t, new List<TcpClient>());
            }
        }

        public void Start()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Parse(Ip), Port);
                tcpListener.Start();

                tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), tcpListener);
            }
            catch
            {

            }
        }

        public void Stop()
        {
            tcpListener.Stop();
            Subscribers.Clear();
            SubscriberBuffer.Clear();
            foreach (var t in TopicSubscriberlist) t.Value.Clear();
        }

        private void DoAcceptTcpClientCallback(IAsyncResult result)
        {
            // Get the listener that handles the client request.
            TcpListener listener = (TcpListener)result.AsyncState;

            // End the operation and display the received data on 
            // the console.
            var cl = listener.EndAcceptTcpClient(result);

            Subscribers.Add(cl);
            SubscriberBuffer.Add(cl, new byte[1024]);

            cl.GetStream().BeginRead(SubscriberBuffer[cl], 0, SubscriberBuffer[cl].Length, ServerReadCallback, cl);

            Debug.WriteLine(String.Format("Client connected from: {0}", cl.Client.RemoteEndPoint.ToString()));
        }

        private void ServerReadCallback(IAsyncResult result)
        {
            var cl = (TcpClient)result.AsyncState;
            var b = SubscriberBuffer[cl];

            /*var stream = cl.GetStream();
            var sizeOfMessage = BitConverter.ToInt32(SubscriberBuffer[cl] as byte[]);

            var messageBuffer = new byte[sizeOfMessage];
            stream.Read(messageBuffer, 0, sizeOfMessage);*/


            String messageBuffer = Encoding.ASCII.GetString(b);//messageBuffer);
            messageBuffer = messageBuffer.Trim('\0');

            cl.GetStream().BeginRead(SubscriberBuffer[cl], 0, SubscriberBuffer[cl].Length, ServerReadCallback, SubscriberBuffer[cl]);

            try
            {
                var deserializedData = JsonConvert.DeserializeObject<SubscriptionParameter>(messageBuffer);

                if (!this.TopicSubscriberlist.ContainsKey(deserializedData.EventId))
                {
                    return;
                }
                else
                {
                    TopicSubscriberlist[deserializedData.EventId].Add(cl);
                }
            }
            catch
            {

            }
        }

        public bool PublishEvent<T>(EventData<T> eventToPublish)
        {
            if (this.TopicSubscriberlist.ContainsKey(eventToPublish.Id)){
                var c_liste = TopicSubscriberlist[eventToPublish.Id];
                foreach (var c in c_liste)
                {
                    var s = Newtonsoft.Json.JsonConvert.SerializeObject(eventToPublish);
                    var b = System.Text.ASCIIEncoding.ASCII.GetBytes(s);
                    c.GetStream().Write(b, 0, b.Length);
                }
            }

            return true;
        }
    }
}
