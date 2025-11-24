using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Helpers.HelperObjects
{
    public class ReviewQuery
    {
        public int? Rating { get; set; } = null;
        public required int FoodItemId { get; set; }
        public bool IsDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}