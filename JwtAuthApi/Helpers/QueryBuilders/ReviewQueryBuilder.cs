using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Models;

namespace JwtAuthApi.Helpers.QueryBuilders
{
    public static class ReviewQueryBuilder
    {
        public static IQueryable<Review> ApplyFilters(IQueryable<Review> query, ReviewQuery queryObject)
        {
            // Filter by food item
            if (queryObject.FoodItemId.HasValue)
                query = query.Where(r => r.FoodItemId == queryObject.FoodItemId.Value);

            // Filter by seller
            if (!string.IsNullOrEmpty(queryObject.SellerId))
                query = query.Where(r => r.SellerId == queryObject.SellerId);

            // Filter by rating
            if (queryObject.Rating.HasValue)
                query = query.Where(r => r.Rating == queryObject.Rating.Value);

            return query;
        }
    }
}