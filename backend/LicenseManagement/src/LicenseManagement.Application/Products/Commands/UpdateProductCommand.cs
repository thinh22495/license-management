using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Products.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Products.Commands;

public class UpdateProductCommand : IRequest<ApiResponse<ProductDto>>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ApiResponse<ProductDto>>
{
    private readonly IAppDbContext _context;

    public UpdateProductCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<ProductDto>> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _context.Products
            .Include(p => p.LicenseProducts)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (product == null)
            return ApiResponse<ProductDto>.Fail("Sản phẩm không tồn tại");

        if (request.Name != null) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        if (request.IconUrl != null) product.IconUrl = request.IconUrl;
        if (request.WebsiteUrl != null) product.WebsiteUrl = request.WebsiteUrl;
        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(ct);

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
