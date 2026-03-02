using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Products.Queries;

public class GetProductsQuery : IRequest<PagedResult<ProductDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly IAppDbContext _context;

    public GetProductsQueryHandler(IAppDbContext context) => _context = context;

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(p => p.Name.Contains(request.Search) || p.Slug.Contains(request.Search));

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                IconUrl = p.IconUrl,
                WebsiteUrl = p.WebsiteUrl,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                LicensePlanCount = p.LicenseProducts.Count
            })
            .ToListAsync(ct);

        return new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
