using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Products.Queries;

public class GetProductBySlugQuery : IRequest<ApiResponse<ProductDto>>
{
    public string Slug { get; set; } = string.Empty;
}

public class GetProductBySlugQueryHandler : IRequestHandler<GetProductBySlugQuery, ApiResponse<ProductDto>>
{
    private readonly IAppDbContext _context;

    public GetProductBySlugQueryHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<ProductDto>> Handle(GetProductBySlugQuery request, CancellationToken ct)
    {
        var product = await _context.Products
            .Include(p => p.LicenseProducts)
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, ct);

        if (product == null)
            return ApiResponse<ProductDto>.Fail("Sản phẩm không tồn tại");

        return ApiResponse<ProductDto>.Ok(new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            IconUrl = product.IconUrl,
            WebsiteUrl = product.WebsiteUrl,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            LicensePlanCount = product.LicenseProducts.Count
        });
    }
}
