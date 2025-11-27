using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Data;
using JwtAuthApi.Dtos.Reviews;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Helpers.QueryBuilders;
using JwtAuthApi.Interfaces;
using JwtAuthApi.Mappers;
using JwtAuthApi.Models;
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

        public async Task<OperationResult<PaginatedResponse<ReviewResponse>, ErrorResult>> GetReviewsAsync(ReviewQuery queryObject)
        {
            try
            {
                var query = _context.Reviews
                         .Include(r => r.Customer)
                         .Include(r => r.FoodItem)
                         .Include(r => r.Order)
                         .AsQueryable();

                // Apply filters
                query = ReviewQueryBuilder.ApplyFilters(query, queryObject);
                // Apply sorting
                if (queryObject.IsDescending)
                    query = query.OrderByDescending(r => r.Rating);
                else
                    query = query.OrderBy(r => r.Rating);

                // Pagination
                var skip = (queryObject.PageNumber - 1) * queryObject.PageSize;
                var totalReviews = await query.CountAsync();

                var reviews = await query
                 .Skip(skip)
                 .Take(queryObject.PageSize)
                 .Select(r => r.ReviewToReviewResponse())
                 .ToListAsync();

                return OperationResult<PaginatedResponse<ReviewResponse>, ErrorResult>.Success(new PaginatedResponse<ReviewResponse>()
                {
                    Total = totalReviews,
                    PageNumber = queryObject.PageNumber,
                    PageSize = queryObject.PageSize,
                    Items = reviews
                });
            }
            catch (Exception)
            {
                return OperationResult<PaginatedResponse<ReviewResponse>, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Retrieving Reviews"
                });

            }
        }

        public async Task<OperationResult<object, ErrorResult>> CreateReviewAsync(CreateReviewRequest request, string customerId)
        {
            try
            {
                // Get the order with seller info
                var order = await _context.Orders
                    .Include(o => o.Seller)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId);

                if (order == null)
                    return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status404NotFound,
                        ErrDescription = "Order not found"
                    });

                // Verify the customer owns this order
                if (order.CustomerId != customerId)
                    return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status403Forbidden,
                        ErrDescription = "You can only review your own orders"
                    });

                // Check if order is delivered
                if (order.Status != OrderStatus.Delivered)
                    return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status400BadRequest,
                        ErrDescription = "You can only review delivered orders"
                    });

                // Check if order already has a review
                var existingReview = await _context.Reviews
                    .AnyAsync(r => r.OrderId == request.OrderId);

                if (existingReview)
                    return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                    {
                        ErrCode = StatusCodes.Status400BadRequest,
                        ErrDescription = "This order has already been reviewed"
                    });

                // Validate FoodItemId if provided
                if (request.FoodItemId.HasValue)
                {
                    var foodItemExists = await _context.OrderItems
                        .AnyAsync(oi => oi.OrderId == request.OrderId && oi.FoodItemId == request.FoodItemId.Value);

                    if (!foodItemExists)
                        return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                        {
                            ErrCode = StatusCodes.Status400BadRequest,
                            ErrDescription = "Food item is not part of this order"
                        });
                }

                // Create the review
                var review = new Review
                {
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CustomerId = customerId,
                    SellerId = order.SellerId,
                    FoodItemId = request.FoodItemId,
                    OrderId = request.OrderId
                };

                _context.Reviews.Add(review);

                // Update seller rating
                var seller = order.Seller;
                var newTotalRatings = seller.TotalRatings + 1;
                seller.Rating = ((seller.Rating * seller.TotalRatings) + request.Rating) / newTotalRatings;
                seller.TotalRatings = newTotalRatings;

                // Update food item rating if specified
                if (request.FoodItemId.HasValue)
                {
                    var foodItem = await _context.FoodItems.FindAsync(request.FoodItemId.Value);
                    if (foodItem != null)
                    {
                        var newFoodTotalRatings = foodItem.TotalRatings + 1;
                        foodItem.Rating = ((foodItem.Rating * foodItem.TotalRatings) + request.Rating) / newFoodTotalRatings;
                        foodItem.TotalRatings = newFoodTotalRatings;
                    }
                }

                await _context.SaveChangesAsync();

                return OperationResult<object, ErrorResult>.Success(new
                {
                    id = review.Id,
                    rating = review.Rating,
                    comment = review.Comment,
                    orderId = review.OrderId,
                    foodItemId = review.FoodItemId,
                    createdAt = review.CreatedAt
                });
            }
            catch (Exception)
            {
                return OperationResult<object, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Something Went Wrong Creating Review"
                });
            }
        }

        public async Task<OperationResult<PaginatedResponse<LowRatedReview>, ErrorResult>> GetLowRatedReviewsAsync(LowRatedReviewQuery queryObject, string sellerId)
        {
            try
            {
                var query = _context.Reviews
                                 .Include(r => r.Customer)
                                 .Include(r => r.FoodItem)
                                 .Include(r => r.Order)
                                 .Where(r => r.SellerId == sellerId && r.Rating <= 2);

                // Filter by food item if provided
                if (queryObject.FoodItemId.HasValue)
                    query = query.Where(r => r.FoodItemId == queryObject.FoodItemId.Value);

                // Order by most recent
                query = query.OrderByDescending(r => r.CreatedAt);

                // Pagination
                var skip = (queryObject.PageNumber - 1) * queryObject.PageSize;
                var totalReviews = await query.CountAsync();

                var lowRatedReviews = await query
                                 .Skip(skip)
                                 .Take(queryObject.PageSize)
                                 .Select(r => r.ReviewToLowRatedReview())
                                 .ToListAsync();

                return OperationResult<PaginatedResponse<LowRatedReview>, ErrorResult>.Success(new PaginatedResponse<LowRatedReview>()
                {
                    Total = totalReviews,
                    PageNumber = queryObject.PageNumber,
                    PageSize = queryObject.PageSize,
                    Items = lowRatedReviews
                });
            }
            catch (Exception)
            {
                return OperationResult<PaginatedResponse<LowRatedReview>, ErrorResult>.Failure(new ErrorResult()
                {
                    ErrCode = StatusCodes.Status500InternalServerError,
                    ErrDescription = "Error retrieving reviews"
                });
            }
        }
    }
}