using System;
using System.Collections.Generic;
using System.Text;

namespace DirectCommunication
{
    public class ServiceInterfaceParameter
    {
        public ServiceInterfaceType InterfaceType;
        public String ServerHost;
        public UInt16 ServerPort;

        public String ClientHost;
        public UInt16 ClientPort;

        public enum ServiceInterfaceType
        {
            MQTT,
            DirectServiceInterface
        }
    }

    public class MqttServiceInterfaceParameter : ServiceInterfaceParameter
    {
        public String Topic;

        public MqttServiceInterfaceParameter()
        {
            this.InterfaceType = ServiceInterfaceType.MQTT;
        }
    }

    public class TCPInterfaceParameter : ServiceInterfaceParameter
    {
        public TCPInterfaceParameter()
        {
            this.InterfaceType = ServiceInterfaceType.DirectServiceInterface;
        }
    }
}
