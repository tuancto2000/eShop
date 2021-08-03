using eShop.ViewModel.Catalog.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace eShop.ViewModel.Catalog.Products
{
    public class GetManageProductPagingRequest : PagingRequestBase
    {
        public string KeyWord { get; set; }
        public List<int> CategoryIds { get; set; }
    }
}
