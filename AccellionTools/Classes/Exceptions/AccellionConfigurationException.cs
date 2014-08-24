using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccellionTools.Exceptions
{
    public class AccellionConfigurationException: SystemException
    {
        public override string Message
        {
            get
            {
                return "Attempted to make Accellion call without calling Initialize(...) on static class!";
            }
        }
    }
}
