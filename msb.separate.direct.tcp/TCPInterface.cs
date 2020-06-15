using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Linq;
using msb.separate.Interfaces;

namespace msb.separate.direct.tcp
{
    public class TCPInterface : BaseInterface
    {
        private TCPConfiguration configuration;

        private List<TCPSubscriber> subscriber;
        private List<TCPPublisher> publisher;

        private Dictionary<String, List<TCPPublisher>> relevantClientsForPublishing;

        public TCPInterface(TCPConfiguration config)
        {
            this.configuration = config;

            List<String> events = new List<string>();
            foreach(var p in config.publications) events.Add(p.Value.EventId);

            List<KeyValuePair<String, UInt16>> subscriptions = new List<KeyValuePair<string, ushort>>();

            foreach(var s in config.subscriptions)
            {
                if(!subscriptions.Exists(e => e.Key == s.Value.Ip))
                {
                    subscriptions.Add(new KeyValuePair<string, ushort>(s.Value.Ip, s.Value.Port));
                }
            }

            if (subscriptions.Count != 0) {
                subscriber = new List<TCPSubscriber>();

                foreach (var s in subscriptions)
                {                    
                    var sub = new TCPSubscriber(s.Key, s.Value);
                    var subs = config.subscriptions.Where(e => e.Value.Ip == s.Key && e.Value.Port == s.Value);

                    foreach (var s_ in subs) sub.AddSubscription(s_.Key, s_.Value);

                    subscriber.Add(sub);
                }
            }

            List<KeyValuePair<String, UInt16>> publications = new List<KeyValuePair<string, ushort>>();
            relevantClientsForPublishing = new Dictionary<string, List<TCPPublisher>>();

            foreach (var s in config.publications)
            {
                if (!relevantClientsForPublishing.ContainsKey(s.Value.EventId)) relevantClientsForPublishing.Add(s.Value.EventId, new List<TCPPublisher>());

                if (!publications.Exists(e => e.Key == s.Value.Ip))
                {
                    publications.Add(new KeyValuePair<string, ushort>(s.Value.Ip, s.Value.Port));
                }
            }

            if (publications.Count != 0)
            {
                publisher = new List<TCPPublisher>();

                foreach (var p in publications)
                {                    
                    var pubs = config.publications.Where(e => e.Value.Ip == p.Key && e.Value.Port == p.Value);
                    List<String> eventList = new List<string>();
                    foreach (var p_ in pubs) eventList.Add(p_.Value.EventId);

                    var pub = new TCPPublisher(p.Key, p.Value, eventList);
                    publisher.Add(pub);
                }
            }
        }

        public void Start()
        {
            if (publisher != null)
            {
                foreach (var p in publisher)
                {
                    p.Start();
                }
            }

            if (subscriber != null)
            {
                foreach(var s in subscriber)
                {
                    s.Connect();
                    s.Listen();
                }
            }
        }

        public void Stop()
        {
            if (publisher != null)
            {
                foreach (var p in publisher)
                {
                    p.Stop();
                }
            }

            if (subscriber != null)
            {
                foreach (var s in subscriber)
                {
                    s.Disconnect();
                }
            }
        }

        public void PublishEvent(EventData eventToPublish)
        {
            foreach (var p in relevantClientsForPublishing[eventToPublish.Id]) p.PublishEvent(eventToPublish);
        }
    }

    public class TCPConfiguration
    {
        public class TCPSubscriptionInstruction : SubscriptionInstruction
        {
            public string Ip;
            public UInt16 Port;
        }

        public class TCPPublicationInstruction : PublicationInstruction
        {
            public string Ip;
            public UInt16 Port;
        }

        public Dictionary<String, TCPSubscriptionInstruction> subscriptions;
        public Dictionary<String, TCPPublicationInstruction> publications;

        public string publicationIp;
        public UInt16 publicationPort;
    }

    public class TCPSubscriber : Interfaces.BaseSubscriber
    {
        private TcpClient tcpClient;

        private readonly string Ip;
        private readonly UInt16 Port;
        private Dictionary<String, SubscriptionInstruction> Integrationen;

        private byte[] buffer = new byte[1024];

        public TCPSubscriber(string localIp, UInt16 localPort)
        {
            Ip = localIp;
            Port = localPort;

            Integrationen = new Dictionary<string, SubscriptionInstruction>();
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

            MakeSubscriptions();

            return tcpClient.Connected;
        }

        public void Disconnect()
        {
            tcpClient.Close();
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
            foreach(var s in Integrationen)
            {
                var d = new SubscriptionInstruction() { EventId = s.Value.EventId };
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

            var deserializedData = JsonConvert.DeserializeObject<EventData>(messageBufferAsUnicode);

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

            String messageBuffer = Encoding.ASCII.GetString(b);//messageBuffer);
            messageBuffer = messageBuffer.Trim('\0');

            cl.GetStream().BeginRead(SubscriberBuffer[cl], 0, SubscriberBuffer[cl].Length, ServerReadCallback, SubscriberBuffer[cl]);

            try
            {
                var deserializedData = JsonConvert.DeserializeObject<SubscriptionInstruction>(messageBuffer);

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

        public bool PublishEvent(EventData eventToPublish)
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
