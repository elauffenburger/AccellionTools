using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AccellionTools;
using AccellionTools.Helpers;
using AccellionTools.Exceptions;

namespace AccellionTest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Initialize class with static method
                AccellionInitializationOptions options = new AccellionInitializationOptions()
                {
                    ID = "",
                    Index = "",
                    Value = ""
                };
                options.handlePrefix = string.Format("{0}/files/", options.Index);
                Accellion.Initialize(options);

                // Get all files for domain, do include file byte[]
                List<AccellionFileRequest> result = Accellion.GetFiles("yourbox@yourdomain.edu", true);

                while (Console.ReadKey().Key != ConsoleKey.Enter)
                {

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
