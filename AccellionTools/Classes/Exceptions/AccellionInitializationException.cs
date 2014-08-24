using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccellionTools.Exceptions
{
    public class AccellionInitializationException : Exception
    {
        public AccellionInitializationException(string msg)
            : base(msg)
        {

        }
    }
}
