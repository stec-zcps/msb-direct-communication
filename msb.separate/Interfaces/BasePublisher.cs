﻿using System;
using System.Collections.Generic;
using System.Text;

namespace msb.separate.Interfaces
{
    public interface BasePublisher
    {
        bool PublishEvent(EventData eventToPublish);
    }
}
