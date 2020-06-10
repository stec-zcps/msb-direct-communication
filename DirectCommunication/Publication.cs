using System;
using System.Collections.Generic;
using System.Text;

namespace DirectCommunication
{
    public class Publication
    {
        public String Id;
        public Object Data;

        public Publication(String id, Object data)
        {
            this.Id = id;
            this.Data = data;
        }
    }
}
