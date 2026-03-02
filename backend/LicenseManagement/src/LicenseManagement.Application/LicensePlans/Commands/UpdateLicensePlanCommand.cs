using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.LicensePlans.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.LicensePlans.Commands;

public class UpdateLicensePlanCommand : IRequest<ApiResponse<LicensePlanDto>>
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public int? DurationDays { get; set; }
    public int? MaxActivations { get; set; }
    public long? Price { get; set; }
    public string? Features { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateLicensePlanCommandHandler : IRequestHandler<UpdateLicensePlanCommand, ApiResponse<LicensePlanDto>>
{
    private readonly IAppDbContext _context;

    public UpdateLicensePlanCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<LicensePlanDto>> Handle(UpdateLicensePlanCommand request, CancellationToken ct)
    {
        var plan = await _context.LicenseProducts
            .Include(lp => lp.Product)
            .FirstOrDefaultAsync(lp => lp.Id == request.Id, ct);

        if (plan == null)
            return ApiResponse<LicensePlanDto>.Fail("Gói license không tồn tại");

        if (request.Name != null) plan.Name = request.Name;
        if (request.DurationDays.HasValue) plan.DurationDays = request.DurationDays.Value;
        if (request.MaxActivations.HasValue) plan.MaxActivations = request.MaxActivations.Value;
        if (request.Price.HasValue) plan.Price = request.Price.Value;
        if (request.Features != null) plan.Features = request.Features;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync(ct);

        return ApiResponse<LicensePlanDto>.Ok(new LicensePlanDto
        {
            Id = plan.Id,
            ProductId = plan.ProductId,
            ProductName = plan.Product.Name,
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
