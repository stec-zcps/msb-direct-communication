using System;
using System.Collections.Generic;
using System.Text;

namespace msb.separate.Interfaces
{
    public interface BaseSubscriber
    {
        bool Connect();
        bool AddSubscription(String id, SubscriptionInstruction instr);
    }
}
