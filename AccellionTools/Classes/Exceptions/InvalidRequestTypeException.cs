using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccellionTools.Exceptions
{
    public class InvalidRequestTypeException : Exception
    {
        public InvalidRequestTypeException()
            : base()
        {

        }

        public InvalidRequestTypeException(string msg)
            : base(msg)
        {

        }
    }
}
