using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Data;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JwtAuthApi.Repository
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDBContext _context;
        public ReviewRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<OperationResult<PaginatedResponse<object>, ErrorResult>> GetReviewsAsync(ReviewQuery queryObject)
        {
            try
            {
                var query = _context.Reviews
                         .Include(r => r.Customer)
                         .Include(r => r.FoodItem)
                         .Include(r => r.Order)
                         .Where(r => r.FoodItemId == queryObject.FoodItemId);

                // Filter by rating
                if (queryObject.Rating.HasValue)
                    query = query.Where(r => r.Rating == queryObject.Rating.Value);

                var totalReviews = await query.CountAsync();

                var reviews = await query
                 .OrderByDescending(r => r.CreatedAt)
                 .Skip((queryObject.PageNumber - 1) * queryObject.PageSize)
                 .Take(queryObject.PageSize)
                 .Select(r => new
                 {
                     id = r.Id,
                     customer = new
                     {
                         name = $"{r.Customer.FirstName} {r.Customer.LastName}",
                         email = r.Customer.Email
                     },
                     orderNumber = r.Order.OrderNumber,
                     foodItem = r.FoodItem != null ? new
                     {
                         id = r.FoodItem.Id,
                         name = r.FoodItem.Name
                     } : null,
                     rating = r.Rating,
                     comment = r.Comment,
                     createdAt = r.CreatedAt
                 })
                 .ToListAsync();

                return OperationResult<PaginatedResponse<object>, ErrorResult>.Success(new PaginatedResponse<object>()
                {
                    Total = totalReviews,
                    PageNumber = queryObject.PageNumber,
                    PageSize = queryObject.PageSize,
                    Items = reviews.Cast<object>().ToList()
                });

            }
            catch (Exception)
            {
                return OperationResult<PaginatedResponse<object>, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Retrieving Reviews"
                });

            }
        }
    }
}