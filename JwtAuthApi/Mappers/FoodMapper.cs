using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Foods;
using JwtAuthApi.Models;

namespace JwtAuthApi.Mappers
{
    public static class FoodMapper
    {
        public static FoodItem CreateFoodItemDtoToFoodItem(this CreateFoodItemDto foodItem)
        {
            return new FoodItem()
            {
                Name = foodItem.Name,
                Description = foodItem.Description,
                Price = foodItem.Price,
                Category = foodItem.Category,
                IsAvailable = foodItem.IsAvailable,
                PreparationTime = foodItem.PreparationTime,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static FoodResponseDto FoodItemToFoodResponseDto(this FoodItem f)
        {
            return new FoodResponseDto()
            {
                Id = f.Id,
                Name = f.Name,
                Description = f.Description,
                Price = f.Price,
                Category = f.Category,
                ImageUrls = f.ImageUrls
                    .Select(img => new FoodImageResponseDto
                    {
                        Id = img.Id,
                        Url = img.ImageUrl,
                        PublicId = img.PublicId,
                        IsMain = img.IsMainImage,
                        UploadedAt = img.UploadedAt
                    })
                    .OrderByDescending(img => img.IsMain)
                    .ToList(),
                Reviews = f.Reviews.OrderByDescending(r => r.CreatedAt).Take(10).Select(r => new ReviewSummaryDto()
                {
                    Id = r.Id,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CustomerName = $"{r.Customer.FirstName} {r.Customer.LastName}",
                    CreatedAt = r.CreatedAt
                }).ToList(),
                IsAvailable = f.IsAvailable,
                PreparationTime = f.PreparationTime,
                Rating = f.Rating,
                TotalRatings = f.TotalRatings,
                TotalSold = f.TotalSold,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt,
                SellerId = f.SellerId,
                SellerBusinessName = f.Seller?.BusinessName
            }
            ;
        }
    }
}