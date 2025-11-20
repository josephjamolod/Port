using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Orders;
using JwtAuthApi.Dtos.SellerAnalytics;
using JwtAuthApi.Helpers.HelperObjects;

namespace JwtAuthApi.Interfaces
{
    public interface ISellerAnalyticsRepository
    {
        Task<OperationResult<PaginatedResponse<OrderDto>, ErrorResult>> GetSellerOrdersAsync(MyOrdersQuery queryObject, string sellerId);
        Task<OperationResult<object, ErrorResult>> GetTopSellingItemsAsync(int limit, string sellerId);
        Task<OperationResult<DashboardStatsResponseDto, ErrorResult>> GetDashboardStatsAsync(string sellerId);
        Task<OperationResult<OrderStatistics, ErrorResult>> GetOrderStatistics(string userId);
    }
}