using System;
using System.Collections.Generic;
using System.Text;

namespace msb.separate.Interfaces
{
    public interface BaseInterface
    {
        bool Connect();
        bool PublishEvent<T>(EventData<T> eventToPublish);

        bool AddSubscription<T>(String id, BaseInterfaceUtils.SubscriptionReceivedCallback<T> callback);
        //bool ReceiveEvent(Subscription receivedEvent);
    }

    public class BaseInterfaceUtils
    {
        public delegate void SubscriptionReceivedCallback<T>(EventData<T> subscription);
    }
}
