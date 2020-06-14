using System;
using System.Collections.Generic;

namespace msb.separate
{
    public class SubscriptionInstruction
    {
        public String EventId;
        public Delegate fPointer;
        public Dictionary<String, String> paramMapping;
    }
}
