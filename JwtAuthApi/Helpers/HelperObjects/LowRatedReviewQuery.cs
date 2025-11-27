using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Helpers.HelperObjects
{
    public class LowRatedReviewQuery
    {
        public int? FoodItemId { get; set; } = null; // Optional: filter by food item
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }
}