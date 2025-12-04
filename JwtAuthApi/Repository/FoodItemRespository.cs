using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Data;
using JwtAuthApi.Dtos.Foods;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Helpers.QueryBuilders;
using JwtAuthApi.Interfaces;
using JwtAuthApi.Mappers;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthApi.Repository
{
    public class FoodItemRespository : IFoodItemRespository
    {
        private readonly ApplicationDBContext _context;
        public FoodItemRespository(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task<PaginatedResponse<FoodResponseDto>> GetAllFoodItemsAsync(AllFoodsQuery queryObject)
        {
            var query = _context.FoodItems
                  .Include(f => f.Seller)
                  .Include(f => f.ImageUrls)
                  .AsQueryable();

            // Apply filters
            query = FoodItemQueryBuilder.ApplyFilters(query, queryObject);
            // Apply sorting
            query = FoodItemQueryBuilder.ApplySorting(query, queryObject);

            // Get total count before pagination
            var totalCount = await query.CountAsync();
            // Apply pagination
            var skip = (queryObject.PageNumber - 1) * queryObject.PageSize;

            //  Materialize the data from database
            var foodItemsFromDb = await query
                .Skip(skip)
                .Take(queryObject.PageSize)
                .ToListAsync();

            //  Apply the mapper in-memory
            var foodItems = foodItemsFromDb
                .Select(f => f.FoodItemToFoodResponseDto())
                .ToList();

            return new PaginatedResponse<FoodResponseDto>()
            {
                Total = totalCount,
                PageNumber = queryObject.PageNumber,
                PageSize = queryObject.PageSize,
                Items = foodItems
            };
        }

        public async Task<OperationResult<FoodResponseDto, string>> GetFoodItemByIdAsync(int foodId)
        {
            var foodItem = await _context.FoodItems
                .Include(f => f.Seller)
                .Include(f => f.ImageUrls)
                .Include(f => f.Reviews)
                    .ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(f => f.Id == foodId);

            if (foodItem == null)
                return OperationResult<FoodResponseDto, string>.Failure("Food item not found"); ;

            var foodItemResponse = foodItem.FoodItemToFoodResponseDto();
            return OperationResult<FoodResponseDto, string>.Success(foodItemResponse);
        }

        public async Task<List<FoodResponseDto>> GetFeaturedItemsAsync()
        {
            var limit = 6;
            var items = await _context.FoodItems
                    .Include(f => f.Seller)
                    .Include(f => f.ImageUrls)
                    .Where(f => f.IsAvailable)
                    .OrderByDescending(f => f.Rating)
                    .ThenByDescending(f => f.TotalSold)
                    .Take(limit)
                    .ToListAsync();
            var foodItems = items.Select(f => f.FoodItemToFoodResponseDto()).ToList();
            return foodItems;
        }

        public async Task<PaginatedResponse<FoodResponseDto>> GetSellerMenuAsync(AllFoodsQuery queryObject, string sellerId)
        {
            var query = _context.FoodItems
                  .Include(f => f.Seller)
                  .Include(f => f.ImageUrls)
                  .Where(f => f.SellerId == sellerId);

            // Apply filters
            query = FoodItemQueryBuilder.ApplyFilters(query, queryObject);
            // Apply sorting
            query = FoodItemQueryBuilder.ApplySorting(query, queryObject);

            // Get total count before pagination
            var totalCount = await query.CountAsync();
            // Apply pagination
            var skip = (queryObject.PageNumber - 1) * queryObject.PageSize;

            //  Materialize the data from database
            var foodItemsFromDb = await query
                .Skip(skip)
                .Take(queryObject.PageSize)
                .ToListAsync();

            //  Apply the mapper in-memory
            var foodItems = foodItemsFromDb
                .Select(f => f.FoodItemToFoodResponseDto())
                .ToList();

            return new PaginatedResponse<FoodResponseDto>()
            {
                Total = totalCount,
                PageNumber = queryObject.PageNumber,
                PageSize = queryObject.PageSize,
                Items = foodItems
            };
        }
    }
}