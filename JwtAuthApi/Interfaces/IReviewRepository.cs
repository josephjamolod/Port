using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Helpers.HelperObjects;

namespace JwtAuthApi.Interfaces
{
    public interface IReviewRepository
    {
        Task<OperationResult<PaginatedResponse<object>, ErrorResult>> GetReviewsAsync(ReviewQuery queryObject);
    }
}