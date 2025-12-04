using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JwtAuthApi.Helpers.HelperObjects;
using JwtAuthApi.Models;


namespace JwtAuthApi.Helpers.QueryBuilders
{
    public static class FoodItemQueryBuilder
    {
        public static IQueryable<FoodItem> ApplyFilters(IQueryable<FoodItem> query, AllFoodsQuery queryObject)
        {
            if (!string.IsNullOrEmpty(queryObject.Name))
                query = query.Where(f => f.Name.Contains(queryObject.Name));

            if (!string.IsNullOrEmpty(queryObject.Description))
                query = query.Where(f => f.Description != null && f.Description.Contains(queryObject.Description));

            if (queryObject.Price > 0)
                query = query.Where(f => f.Price <= queryObject.Price);

            if (!string.IsNullOrEmpty(queryObject.Category))
                query = query.Where(f => f.Category == queryObject.Category);

            if (queryObject.IsAvailable != null)
                query = query.Where(f => f.IsAvailable == queryObject.IsAvailable);

            if (queryObject.PreparationTime > 0)
                query = query.Where(f => f.PreparationTime <= queryObject.PreparationTime);

            if (queryObject.Rating > 0)
                query = query.Where(f => f.Rating >= queryObject.Rating);

            return query;
        }

        public static IQueryable<FoodItem> ApplySorting(IQueryable<FoodItem> query, AllFoodsQuery queryObject)
        {
            if (!queryObject.SortBy.HasValue)
                return query.OrderByDescending(f => f.CreatedAt);

            switch (queryObject.SortBy.Value)
            {
                case FoodsQueryOption.Name:
                    query = queryObject.IsDescending
                        ? query.OrderByDescending(f => f.Name)
                        : query.OrderBy(f => f.Name);
                    break;

                case FoodsQueryOption.Description:
                    query = queryObject.IsDescending
                        ? query.OrderByDescending(f => f.Description)
                        : query.OrderBy(f => f.Description);
                    break;

                case FoodsQueryOption.Price:
                    query = queryObject.IsDescending
                        ? query.OrderByDescending(f => f.Price)
                        : query.OrderBy(f => f.Price);
                    break;

                case FoodsQueryOption.Category:
                    query = queryObject.IsDescending
                        ? query.OrderByDescending(f => f.Category)
                        : query.OrderBy(f => f.Category);
                    break;

                case FoodsQueryOption.IsAvailable:
                    query = queryObject.IsDescending
                        ? query.OrderByDescending(f => f.IsAvailable)
                        : query.OrderBy(f => f.IsAvailable);
                    break;

                case FoodsQueryOption.PreparationTime:
                    query = queryObject.IsDescending
                        ? query.OrderByDescending(f => f.PreparationTime)
                        : query.OrderBy(f => f.PreparationTime);
                    break;

                case FoodsQueryOption.Rating:
                    query = queryObject.IsDescending
                        ? query.OrderByDescending(f => f.Rating)
                        : query.OrderBy(f => f.Rating);
                    break;

                default:
                    query = query.OrderByDescending(f => f.CreatedAt);
                    break;
            }
            return query;
        }
    }
}