using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Dtos.Reviews
{
    public class ReviewResponse
    {
        public int Id { get; set; }
        public ReviewCustomer Customer { get; set; } = new();

        public string OrderNumber { get; set; } = string.Empty;
        public ReviewFoodItem? ReviewFoodItem { get; set; } = null;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }

    public class ReviewCustomer
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ReviewFoodItem
    {
        public int FoodItemId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}