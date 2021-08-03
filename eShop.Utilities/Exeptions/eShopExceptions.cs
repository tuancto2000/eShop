using System;
using System.Collections.Generic;
using System.Text;

namespace eShop.Utilities.Exeptions
{
    public class eShopExceptions : Exception
    {
        public eShopExceptions()
        {
        }

        public eShopExceptions(string message)
            : base(message)
        {
        }

        public eShopExceptions(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
