using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Helpers.HelperObjects
{
    public enum FoodsQueryOption
    {
        Name,
        Description,
        Price,
        Category,
        IsAvailable,
        PreparationTime,
        Rating
    }
    public class AllFoodsQuery
    {
        public string? Name { get; set; } = null;
        public string? Description { get; set; } = null;
        [Column(TypeName = "decimal(10,2)")]
        public int Price { get; set; }
        public string? Category { get; set; } = null;
        public bool? IsAvailable { get; set; } = null;
        public int PreparationTime { get; set; } = 30; // In minutes

        public FoodsQueryOption? SortBy { get; set; } = null;
        public decimal Rating { get; set; } = 0;
        public bool IsDescending { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}