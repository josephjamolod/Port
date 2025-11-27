using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Helpers.HelperObjects
{
    public class ReviewQuery
    {
        public int? Rating { get; set; } = null;
        public int? FoodItemId { get; set; } = null; // Filter by food item (optional)
        public string? SellerId { get; set; } = null; // Filter by seller (optional)
        public bool IsDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}