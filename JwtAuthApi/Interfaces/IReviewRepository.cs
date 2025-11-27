using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Dtos.Reviews;
using JwtAuthApi.Helpers.HelperObjects;

namespace JwtAuthApi.Interfaces
{
    public interface IReviewRepository
    {
        Task<OperationResult<PaginatedResponse<ReviewResponse>, ErrorResult>> GetReviewsAsync(ReviewQuery queryObject);
        Task<OperationResult<object, ErrorResult>> CreateReviewAsync(CreateReviewRequest request, string customerId);
        Task<OperationResult<PaginatedResponse<LowRatedReview>, ErrorResult>> GetLowRatedReviewsAsync(LowRatedReviewQuery queryObject, string sellerId);
    }
}