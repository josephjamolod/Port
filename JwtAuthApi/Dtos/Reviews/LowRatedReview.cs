using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Dtos.Reviews
{
    public class LowRatedReview
    {
        public int Id { get; set; }
        public ReviewCustomer Customer { get; set; } = new();
        public string OrderNumber { get; set; } = string.Empty;
        public string FoodItem { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}