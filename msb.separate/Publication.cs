using System;
using System.Collections.Generic;
using System.Text;

namespace msb.separate
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
