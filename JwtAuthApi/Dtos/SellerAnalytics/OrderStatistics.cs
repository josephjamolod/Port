using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JwtAuthApi.Dtos.SellerAnalytics
{
    public class OrderStatistics
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int PreparingOrders { get; set; }
        public int ReadyOrders { get; set; }
        public int OutForDeliveryOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}