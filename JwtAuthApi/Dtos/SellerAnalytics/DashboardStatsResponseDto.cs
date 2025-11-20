using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Dtos.SellerAnalytics
{
    public class DashboardStatsResponseDto
    {
        public Today Today { get; set; } = new();
        public ThisMonth ThisMonth { get; set; } = new();
        public Overview Overview { get; set; } = new();
    }

    public class Today
    {
        public int Orders { get; set; }
        public decimal TodayRevenue { get; set; }
        public int Pending { get; set; }
    }

    public class ThisMonth
    {
        public int MonthOrders { get; set; }
        public decimal MonthRevenue { get; set; }
    }

    public class Overview
    {
        public int PendingOrders { get; set; }
        public int TotalItems { get; set; }
        public int AvailableItems { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}