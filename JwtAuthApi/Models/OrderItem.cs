using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Quantity { get; set; }
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; } // Price at time of order
        [StringLength(200)]
        public string? SpecialInstructions { get; set; }

        // Foreign Key that connect each orderItem to a Order
        [Required]
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        // Foreign Key that connect each foodItemId to a Order
        [Required]
        public int FoodItemId { get; set; }
        public FoodItem FoodItem { get; set; } = null!;
    }
}