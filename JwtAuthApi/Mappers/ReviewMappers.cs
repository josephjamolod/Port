using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Reviews;
using JwtAuthApi.Models;

namespace JwtAuthApi.Mappers
{
    public static class ReviewMappers
    {
        public static ReviewResponse ReviewToReviewResponse(this Review review)
        {
            return new ReviewResponse()
            {
                Id = review.Id,
                Customer = new ReviewCustomer
                {
                    Name = $"{review.Customer.FirstName} {review.Customer.LastName}",
                    Email = review.Customer.Email!
                },
                OrderNumber = review.Order.OrderNumber,
                ReviewFoodItem = review.FoodItem != null ? new ReviewFoodItem
                {
                    FoodItemId = review.FoodItem.Id,
                    Name = review.FoodItem.Name
                } : null,
                Rating = review.Rating,
                Comment = review.Comment!,
                CreatedAt = review.CreatedAt
            };
        }
    }
}