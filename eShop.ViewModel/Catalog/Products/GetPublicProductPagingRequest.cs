
using eShop.ViewModel.Catalog.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace eShop.ViewModel.Catalog.Products
{
    public class GetPublicProductPagingRequest : PagingRequestBase
    {
        public int? CategoryId { get; set; }
    }
}
