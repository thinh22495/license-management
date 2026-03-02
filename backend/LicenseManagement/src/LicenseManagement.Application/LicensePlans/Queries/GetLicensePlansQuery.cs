using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.LicensePlans.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.LicensePlans.Queries;

public class GetLicensePlansQuery : IRequest<List<LicensePlanDto>>
{
    public Guid ProductId { get; set; }
    public bool? ActiveOnly { get; set; }
}

public class GetLicensePlansQueryHandler : IRequestHandler<GetLicensePlansQuery, List<LicensePlanDto>>
{
    private readonly IAppDbContext _context;

    public GetLicensePlansQueryHandler(IAppDbContext context) => _context = context;

    public async Task<List<LicensePlanDto>> Handle(GetLicensePlansQuery request, CancellationToken ct)
    {
        var query = _context.LicenseProducts
            .Include(lp => lp.Product)
            .Where(lp => lp.ProductId == request.ProductId);

        if (request.ActiveOnly == true)
            query = query.Where(lp => lp.IsActive);

        return await query
            .OrderBy(lp => lp.Price)
            .Select(lp => new LicensePlanDto
            {
                Id = lp.Id,
                ProductId = lp.ProductId,
                ProductName = lp.Product.Name,
                Name = lp.Name,
                DurationDays = lp.DurationDays,
                MaxActivations = lp.MaxActivations,
                Price = lp.Price,
                Features = lp.Features,
                IsActive = lp.IsActive,
                CreatedAt = lp.CreatedAt
            })
            .ToListAsync(ct);
    }
}
