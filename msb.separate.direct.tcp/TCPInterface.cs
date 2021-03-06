﻿using System;
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
        private TCPConfiguration Configuration;

        private List<TCPSubscriber> subscriber;
        private List<TCPPublisher> publisher;

        public TCPInterface(TCPConfiguration config)
        {
            this.Configuration = config;

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

            foreach (var s in config.publications)
            {
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
                    //var pubs = config.publications.Where(e => e.Value.Ip == p.Key && e.Value.Port == p.Value); //funktioniert nicht?
                    List<String> eventList = new List<string>();
                    //foreach (var p_ in pubs) eventList.Add(p_.Value.EventId);
                    foreach(var e_ in config.publications)
                    {
                        if(e_.Value.Ip == p.Key && e_.Value.Port == p.Value)
                        {
                            eventList.Add(e_.Value.EventId);
                        }
                    }

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
                }
            }
        }

        public void Stop()
        {
            if (subscriber != null)
            {
                foreach (var s in subscriber)
                {
                    s.Disconnect();
                }
            }

            if (publisher != null)
            {
                foreach (var p in publisher)
                {
                    p.Stop();
                }
            }
        }

        public void PublishEvent(EventData eventToPublish)
        {
            foreach(var p in publisher) p.PublishEvent(eventToPublish);
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
    }

    public class TCPSubscriber : Interfaces.BaseSubscriber
    {
        private TcpClient tcpClient;

        private readonly string Ip;
        private readonly UInt16 Port;
        private Dictionary<String, SubscriptionInstruction> Subscriptions;

        private byte[] buffer = new byte[1024];

        public TCPSubscriber(string localIp, UInt16 localPort)
        {
            Ip = localIp;
            Port = localPort;

            Subscriptions = new Dictionary<string, SubscriptionInstruction>();
        }

        public bool Connect()
        {
            tcpClient = new TcpClient();

            tcpClient.BeginConnect(Ip, Port, ConnectCallback, null);

            return true;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                tcpClient.EndConnect(ar);

                MakeSubscriptions();

                Listen();
            }
            catch
            {
            }
        }

        public void Disconnect()
        {
            tcpClient.GetStream().Close();

            tcpClient.Close();
        }

        public bool AddSubscription(string id, SubscriptionInstruction instr)
        {
            if (Subscriptions.ContainsKey(id))
                return false;

            Subscriptions.Add(id, instr);

            return true;
        }

        public void MakeSubscriptions()
        {
            foreach(var s in Subscriptions)
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

            try
            {
                tcpClient.GetStream().BeginRead(buffer, 0, buffer.Length, ListenCallback, buffer);

                var deserializedData = JsonConvert.DeserializeObject<EventData>(messageBufferAsUnicode);

                foreach (var s in Subscriptions)
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
            catch
            {

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
            if(tcpListener != null) tcpListener.Stop();
            Subscribers.Clear();
            SubscriberBuffer.Clear();
            foreach (var t in TopicSubscriberlist) t.Value.Clear();
        }

        private void DoAcceptTcpClientCallback(IAsyncResult result)
        {
            TcpListener listener = (TcpListener)result.AsyncState;

            var cl = listener.EndAcceptTcpClient(result);

            Subscribers.Add(cl);
            SubscriberBuffer.Add(cl, new byte[1024]);

            cl.GetStream().BeginRead(SubscriberBuffer[cl], 0, SubscriberBuffer[cl].Length, ServerReadCallback, cl);
        }

        private void ServerReadCallback(IAsyncResult result)
        {
            try
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
