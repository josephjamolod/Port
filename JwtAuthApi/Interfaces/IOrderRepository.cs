using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Orders;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Models;

namespace JwtAuthApi.Interfaces
{
    public interface IOrderRepository
    {
        Task<OperationResult<CheckoutSelectedResponse, ErrorResult>> CheckoutSelectedSellersAsync(CheckoutSelectedRequest request, string userId);
        Task<OperationResult<Order, ErrorResult>> GetOrderByIdAsync(int id);
        Task<OperationResult<OrderDto, ErrorResult>> BuyNowAsync(BuyNowRequest request, string userId);
        Task<OperationResult<PaginatedResponse<OrderDto>, ErrorResult>> GetMyOrdersAsync(MyOrdersQuery queryObject, string userId);
        Task<OperationResult<object, ErrorResult>> UpdateOrderStatusAsync(UpdateOrderStatusParams prop);
        Task<OperationResult<object, ErrorResult>> DeleteOrderAsync(int orderId);
    }
}