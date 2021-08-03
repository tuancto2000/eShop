using eShop.Data.EF;
using eShop.Data.Entities;
using eShop.ViewModel.Catalog.Common;
using eShop.ViewModel.Catalog.Products;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using eShop.Utilities.Exeptions;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.IO;
using eShop.Application.Common;
using eShop.ViewModel.Catalog.ProductImages;

namespace eShop.Application.Catalog.Products
{
    public class ProductService : IProductService
    {
        private readonly EShopDBContext _context;
        private readonly IStorageService _storageService;
        private const string USER_CONTENT_FOLDER_NAME = "user-content";

        public ProductService(EShopDBContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }
        private async Task<string> SaveFile(IFormFile file)
        {
            var originalFileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}";
            await _storageService.SaveFileAsync(file.OpenReadStream(), fileName);
            return "/" + USER_CONTENT_FOLDER_NAME + "/" + fileName;
        }
        public async Task AddViewCount(int productId)
        {
            var product = await _context.Products.FindAsync(productId);

            product.ViewCount++;

            await _context.SaveChangesAsync();
        }

        public async Task<int> Create(ProductCreateRequest request)
        {
            var product = new Product()
            {
                Price = request.Price,
                OriginalPrice = request.OriginalPrice,
                Stock = request.Stock,
                ViewCount = 0,
                DateCreated = DateTime.Now,
                ProductTranslations = new List<ProductTranslation>
                {
                    new ProductTranslation
                    {
                        Description = request.Description,
                        Details = request.Details,
                        SeoDescription = request.SeoDescription,
                        SeoTitle = request.SeoTitle,
                        SeoAlias = request.SeoAlias,
                        LanguageId = request.LanguageId
                    }

                }
            };

            if (request.ThumbnailImage != null)
            {
                product.ProductImages = new List<ProductImage>()
                {
                    new ProductImage()
                    {
                        Caption = "Thumbnail image",
                        DateCreated = DateTime.Now,
                        FileSize = request.ThumbnailImage.Length,
                        ImagePath = await this.SaveFile(request.ThumbnailImage),
                        IsDefault = true,
                        SortOrder = 1
                    }
                };
            }
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product.Id;
        }
        
        public async Task<int> Delete(int productId)
        {
             
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new eShopExceptions("can not find product");

            var images =  _context.ProductImages.Where(x => x.ProductId == productId );
            foreach (var image in images)
            {
               await _storageService.DeleteFileAsync(image.ImagePath);
            }

            _context.Remove(product);
            
            return await _context.SaveChangesAsync();
        }


        public async Task<PageResult<ProductViewModel>> GetAllPaging(GetManageProductPagingRequest request)
        {
            //1. select 
            var query = from p in _context.Products
                        join pt in _context.ProductTranslations on p.Id equals pt.ProductId
                        join pic in _context.ProductInCategories on p.Id equals pic.ProductId
                        join c in _context.Categories on pic.CategoryId equals c.Id
                        join ct in _context.CategoryTranslations on c.Id equals ct.CategoryId
                        select new { p, pt, pic };
            // 2. filter
            if (!string.IsNullOrEmpty(request.KeyWord))
                query = query.Where(x => x.pt.Name.Contains(request.KeyWord));
            if (request.CategoryIds.Count > 0)
                query = query.Where(x => request.CategoryIds.Contains(x.pic.CategoryId));
            // 3. Paging
            int totalRow = await query.CountAsync();
            var data = await query.Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize)
                .Select(x => new ProductViewModel()
                {
                    Id = x.p.Id,
                    Name = x.pt.Name,
                    DateCreated = x.p.DateCreated,
                    Description = x.pt.Description,
                    Details = x.pt.Details,
                    LanguageId = x.pt.LanguageId,
                    OriginalPrice = x.p.OriginalPrice,
                    Price = x.p.Price,
                    SeoAlias = x.pt.SeoAlias,
                    SeoDescription = x.pt.SeoDescription,
                    SeoTitle = x.pt.SeoTitle,
                    Stock = x.p.Stock,
                    ViewCount = x.p.ViewCount
                }).ToListAsync();

            // 4. 
            var pageResult = new PageResult<ProductViewModel>()
            {
                Items = data,
                TotalRecord = totalRow
            };
            return pageResult;

        }
        public async Task<int> Update(ProductUpdateRequest request)
        {
            var product = await _context.Products.FindAsync(request.Id);
            var productTranslation = await _context.ProductTranslations.FirstOrDefaultAsync(x => x.ProductId == request.Id 
                                    && x.LanguageId == request.LanguageId);

            if (product == null || productTranslation == null)
                throw new eShopExceptions($"can not find product with id = {request.Id}");

            productTranslation.Name = request.Name;
            productTranslation.SeoAlias = request.SeoAlias;
            productTranslation.SeoDescription = request.SeoDescription;
            productTranslation.SeoTitle = request.SeoTitle;
            productTranslation.Description = request.Description;
            productTranslation.Details = request.Details;

            //Save image
            var thumbnail = await _context.ProductImages.FirstOrDefaultAsync(x => x.ProductId == request.Id && x.IsDefault == true);
            if (request.ThumbnailImage != null)
            {
                thumbnail.FileSize = request.ThumbnailImage.Length;
                thumbnail.ImagePath = await this.SaveFile(request.ThumbnailImage);
            }
            return await _context.SaveChangesAsync();


        }



        public async Task<bool> UpdatePrice(int productId, decimal newPrice)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
                throw new eShopExceptions($"can not find product with id = {productId}");

            product.Price = newPrice;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateStock(int productId, int addedQuantity)
        {
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
                throw new eShopExceptions($"can not find product with id = {productId}");

            product.Price += addedQuantity;

            return await _context.SaveChangesAsync() > 0;
        }

        //public async Task<List<ProductViewModel>> GetAll()
        //{
        //    throw new NotImplementedException();
        //}
        public async Task<PageResult<ProductViewModel>> GetAllByCategoryId(GetPublicProductPagingRequest request)
        {
            //1. select 
            var query = from p in _context.Products
                        join pt in _context.ProductTranslations on p.Id equals pt.ProductId
                        join pic in _context.ProductInCategories on p.Id equals pic.ProductId
                        join c in _context.Categories on pic.CategoryId equals c.Id
                        join ct in _context.CategoryTranslations on c.Id equals ct.CategoryId
                        select new { p, pt, pic };
            // 2. filter
            if (request.CategoryId > 0 && request.CategoryId.HasValue)
                query = query.Where(x => request.CategoryId == x.pic.CategoryId);

            // 3. Paging
            int totalRow = await query.CountAsync();
            var data = await query.Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize)
                .Select(x => new ProductViewModel()
                {
                    Id = x.p.Id,
                    Name = x.pt.Name,
                    DateCreated = x.p.DateCreated,
                    Description = x.pt.Description,
                    Details = x.pt.Details,
                    LanguageId = x.pt.LanguageId,
                    OriginalPrice = x.p.OriginalPrice,
                    Price = x.p.Price,
                    SeoAlias = x.pt.SeoAlias,
                    SeoDescription = x.pt.SeoDescription,
                    SeoTitle = x.pt.SeoTitle,
                    Stock = x.p.Stock,
                    ViewCount = x.p.ViewCount
                }).ToListAsync();

            // 4. 
            var pageResult = new PageResult<ProductViewModel>()
            {
                Items = data,
                TotalRecord = totalRow
            };
            return pageResult;
        }
        public async Task<int> RemoveImage( int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image != null) throw new eShopExceptions("can not find image");
            _context.ProductImages.Remove(image);
            await _storageService.DeleteFileAsync(image.ImagePath);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> AddImage(int productId , ProductImageCreateRequest request)
        {
            var productImage = new ProductImage()
            {
                Caption = request.Caption,
                DateCreated = DateTime.Now,
                IsDefault = request.IsDefault,
                ProductId = productId,
                SortOrder = request.SortOrder
            };

            if (request.ImageFile != null)
            {
                productImage.ImagePath = await this.SaveFile(request.ImageFile);
                productImage.FileSize = request.ImageFile.Length;
            }
            _context.ProductImages.Add(productImage);
            await _context.SaveChangesAsync();
            return productImage.Id;
        }

        public async Task<int> UpdateImage(int imageId , ProductImageUpdateRequest request)
        {
            var productImage = await _context.ProductImages.FindAsync(imageId);
            if (productImage == null)
                throw new eShopExceptions($"Cannot find an image with id {imageId}");

            if (request.ImageFile != null)
            {
                productImage.ImagePath = await this.SaveFile(request.ImageFile);
                productImage.FileSize = request.ImageFile.Length;
            }
            _context.ProductImages.Update(productImage);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<ProductViewModel>> GetAll()
        {
            var query = from p in _context.Products
                        join pt in _context.ProductTranslations on p.Id equals pt.ProductId
                        join pic in _context.ProductInCategories on p.Id equals pic.ProductId
                        join c in _context.Categories on pic.CategoryId equals c.Id
                        //join ct in _context.CategoryTranslations on c.Id equals ct.CategoryId
                        select new { p, pt, pic };
            var data = await query
                .Select(x => new ProductViewModel()
                {
                    Id = x.p.Id,
                    Name = x.pt.Name,
                    DateCreated = x.p.DateCreated,
                    Description = x.pt.Description,
                    Details = x.pt.Details,
                    LanguageId = x.pt.LanguageId,
                    OriginalPrice = x.p.OriginalPrice,
                    Price = x.p.Price,
                    SeoAlias = x.pt.SeoAlias,
                    SeoDescription = x.pt.SeoDescription,
                    SeoTitle = x.pt.SeoTitle,
                    Stock = x.p.Stock,
                    ViewCount = x.p.ViewCount
                }).ToListAsync();

            // 4. 
            return data;
        }
    }
}
