using System;
using System.Collections.Generic;
using System.Text;

namespace msb.separate.Interfaces
{
    public interface BaseSubscriber
    {
        bool Connect();
        bool AddSubscription<T>(String id, BaseInterfaceUtils.SubscriptionReceivedCallback<T> callback);
    }
}
