using System;
using System.Collections.Generic;
using System.Text;

namespace msb.separate.Interfaces
{
    public interface BaseSubscriber
    {
        bool Connect();
        void Disconnect();
        bool AddSubscription(String id, SubscriptionInstruction instr);
        void MakeSubscriptions();
    }
}