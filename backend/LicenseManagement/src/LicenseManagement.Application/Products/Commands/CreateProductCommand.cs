using System.Text.RegularExpressions;
using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Products.DTOs;
using LicenseManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Products.Commands;

public class CreateProductCommand : IRequest<ApiResponse<ProductDto>>
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ApiResponse<ProductDto>>
{
    private readonly IAppDbContext _context;

    public CreateProductCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<ProductDto>> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var slug = string.IsNullOrEmpty(request.Slug)
            ? GenerateSlug(request.Name)
            : request.Slug;

        if (await _context.Products.AnyAsync(p => p.Slug == slug, ct))
            return ApiResponse<ProductDto>.Fail("Slug đã tồn tại");

        var product = new Product
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            IconUrl = request.IconUrl,
            WebsiteUrl = request.WebsiteUrl,
        };

        _context.Products.Add(product);
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
        });
    }

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace("đ", "d");
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return slug;
    }
}
