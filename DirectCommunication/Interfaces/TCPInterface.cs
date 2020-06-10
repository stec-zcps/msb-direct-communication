using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json;
using static DirectCommunication.Interfaces.BaseInterfaceUtils;

namespace DirectCommunication.Interfaces
{
    public class TCPInterface : BaseInterface
    {
        private TCPInterfaceParameter _interfaceParameters;

        private TcpClient tcpClient;
        private TcpListener tcpListener;
        private TcpClient tcpListenerClient;

        private GenericDictionary _subscriptionMapping;

        private byte[] sizeBufferServer = new byte[4];

        public TCPInterface(TCPInterfaceParameter interfaceParameters)
        {
            this._interfaceParameters = interfaceParameters;
            this._subscriptionMapping = new GenericDictionary();
        }

        public bool Connect()
        {
            try
            {
                tcpListener = new TcpListener(IPAddress.Parse(_interfaceParameters.ServerHost), _interfaceParameters.ServerPort);
                tcpListener.Start();

                // Client will be initialized once we try to send an event
                tcpClient = new TcpClient();
                //tcpClient = new TcpClient(_interfaceParameters.ClientHost, _interfaceParameters.ClientPort);

                tcpListener.BeginAcceptTcpClient(new AsyncCallback(DoAcceptTcpClientCallback), tcpListener);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("Failed to create TCP listener: {0}", e.ToString()));
                return false;
            }
            return true;
        }

        private void DoAcceptTcpClientCallback(IAsyncResult result)
        {
            // Get the listener that handles the client request.
            TcpListener listener = (TcpListener)result.AsyncState;

            // End the operation and display the received data on 
            // the console.
            this.tcpListenerClient = listener.EndAcceptTcpClient(result);

            tcpListenerClient.GetStream().BeginRead(sizeBufferServer, 0, sizeBufferServer.Length, ServerReadCallback, sizeBufferServer);

            Debug.WriteLine(String.Format("Client connected from: {0}", this.tcpListenerClient.Client.RemoteEndPoint.ToString()));

        }

        private void ServerReadCallback(IAsyncResult result)
        {
            var stream = tcpListenerClient.GetStream();
            var sizeOfMessage = BitConverter.ToInt32(result.AsyncState as byte[]);

            var messageBuffer = new byte[sizeOfMessage];
            stream.Read(messageBuffer, 0, sizeOfMessage);

            String messageBufferAsUnicode = Encoding.Unicode.GetString(messageBuffer);

            tcpListenerClient.GetStream().BeginRead(sizeBufferServer, 0, 4, ServerReadCallback, sizeBufferServer);

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

        private bool TryConnect()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(_interfaceParameters.ClientHost, _interfaceParameters.ClientPort);
                
            }
            catch
            {
                return false;
            }
            return tcpClient.Connected;
        }

        public bool PublishEvent<T>(EventData<T> eventToPublish)
        {
            if (!tcpClient.Connected)
                if (!TryConnect())
                    throw new Exception(String.Format("Unable to connect to remote server: {0}:{1}", this._interfaceParameters.ClientHost, this._interfaceParameters.ClientPort));

            var writer = tcpClient.GetStream();

            var bufferToSend = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(eventToPublish));
            writer.Write(BitConverter.GetBytes(bufferToSend.Length));
            writer.Write(bufferToSend, 0, bufferToSend.Length);

            return true;
        }

        public bool AddSubscription<T>(string id, BaseInterfaceUtils.SubscriptionReceivedCallback<T> callback)
        {
            if (_subscriptionMapping.ContainsKey(id))
                return false;

            _subscriptionMapping.Add(id, callback);

            return true;
        }
    }
}
