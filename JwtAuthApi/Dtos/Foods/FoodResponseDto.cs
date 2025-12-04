using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Models;

namespace JwtAuthApi.Dtos.Foods
{
    public class FoodResponseDto
    {

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string Category { get; set; } = string.Empty; // e.g., "Main Course", "Dessert", "Beverage"

        public List<FoodImageResponseDto> ImageUrls { get; set; } = new();
        public List<ReviewSummaryDto> Reviews { get; set; } = new();
        public bool IsAvailable { get; set; } = true;
        public int PreparationTime { get; set; } = 30; // In minutes
        public decimal Rating { get; set; } = 0;
        public int TotalRatings { get; set; } = 0;
        public int TotalSold { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Seller information
        public string SellerId { get; set; } = string.Empty;
        public string? SellerBusinessName { get; set; }

    }
}