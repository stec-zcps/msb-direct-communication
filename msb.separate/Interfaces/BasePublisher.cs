using System;
using System.Collections.Generic;
using System.Text;

namespace msb.separate.Interfaces
{
    public interface BasePublisher
    {
        void Start();
        void Stop();
        bool PublishEvent<T>(EventData<T> eventToPublish);
    }
}
