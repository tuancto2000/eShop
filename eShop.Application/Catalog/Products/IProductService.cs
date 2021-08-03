
using eShop.ViewModel.Catalog.Common;
using eShop.ViewModel.Catalog.ProductImages;
using eShop.ViewModel.Catalog.Products;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Application.Catalog.Products
{
    public interface IProductService
    {
        public Task<int> Create(ProductCreateRequest request);
        public Task<int> Update(ProductUpdateRequest request);
        public Task<int> Delete(int productId);
        public Task<bool> UpdatePrice(int productId , decimal newPrice);
        public Task<bool> UpdateStock(int productId , int addedQuantity);
        public Task AddViewCount(int productId);    

        //public Task<List<ProductViewModel>> GetAll();
        public Task<PageResult<ProductViewModel>> GetAllPaging(GetManageProductPagingRequest request );

        public Task<PageResult<ProductViewModel>> GetAllByCategoryId(GetPublicProductPagingRequest request);

        public Task<int> AddImage(int productId, ProductImageCreateRequest image);
        public Task<int> RemoveImage(int imageId);
        public Task<int> UpdateImage(int imageId , ProductImageUpdateRequest image);
        // public Task<PageResult<ProductImageViewModel>> GetAllImages();

        public Task<List<ProductViewModel>> GetAll();

    }
}
