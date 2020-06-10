using System;
using System.Collections.Generic;
using System.Text;
using static msb.separate.ServiceInterfaceParameter;

namespace msb.separate
{
    public class SubscriptionParameter
    {
        public ServiceInterfaceType ParameterType;
    }

    public class MqttSubscriptionParameter : SubscriptionParameter
    {
        public String Topic;
        public String Broker;
    }

    public class DirectSubscriptionParameter : SubscriptionParameter
    {
        public String EventId;
    }
}
