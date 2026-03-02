using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.LicensePlans.DTOs;
using LicenseManagement.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.LicensePlans.Commands;

public class CreateLicensePlanCommand : IRequest<ApiResponse<LicensePlanDto>>
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationDays { get; set; }
    public int MaxActivations { get; set; } = 1;
    public long Price { get; set; }
    public string Features { get; set; } = "[]";
}

public class CreateLicensePlanCommandHandler : IRequestHandler<CreateLicensePlanCommand, ApiResponse<LicensePlanDto>>
{
    private readonly IAppDbContext _context;

    public CreateLicensePlanCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<LicensePlanDto>> Handle(CreateLicensePlanCommand request, CancellationToken ct)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
        if (product == null)
            return ApiResponse<LicensePlanDto>.Fail("Sản phẩm không tồn tại");

        var plan = new LicenseProduct
        {
            ProductId = request.ProductId,
            Name = request.Name,
            DurationDays = request.DurationDays,
            MaxActivations = request.MaxActivations,
            Price = request.Price,
            Features = request.Features,
        };

        _context.LicenseProducts.Add(plan);
        await _context.SaveChangesAsync(ct);

        return ApiResponse<LicensePlanDto>.Ok(new LicensePlanDto
        {
            Id = plan.Id,
            ProductId = plan.ProductId,
            ProductName = product.Name,
            Name = plan.Name,
            DurationDays = plan.DurationDays,
            MaxActivations = plan.MaxActivations,
            Price = plan.Price,
            Features = plan.Features,
            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt
        });
    }
}
