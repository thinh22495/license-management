using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Licenses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Queries;

public class GetMyLicensesQuery : IRequest<ApiResponse<List<LicenseDto>>>
{
    public Guid UserId { get; set; }
}

public class GetMyLicensesQueryHandler : IRequestHandler<GetMyLicensesQuery, ApiResponse<List<LicenseDto>>>
{
    private readonly IAppDbContext _context;

    public GetMyLicensesQueryHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse<List<LicenseDto>>> Handle(GetMyLicensesQuery request, CancellationToken ct)
    {
        var licenses = await _context.UserLicenses
            .Include(ul => ul.User)
            .Include(ul => ul.LicenseProduct)
                .ThenInclude(lp => lp.Product)
            .Where(ul => ul.UserId == request.UserId)
            .OrderByDescending(ul => ul.CreatedAt)
            .Select(ul => new LicenseDto
            {
                Id = ul.Id,
                UserId = ul.UserId,
                UserEmail = ul.User != null ? ul.User.Email : "",
                ProductName = ul.LicenseProduct.Product.Name,
                PlanName = ul.LicenseProduct.Name,
                LicenseKey = ul.LicenseKey,
                Status = ul.Status.ToString(),
                ActivatedAt = ul.ActivatedAt,
                ExpiresAt = ul.ExpiresAt,
                CurrentActivations = ul.CurrentActivations,
                MaxActivations = ul.LicenseProduct.MaxActivations,
                CreatedAt = ul.CreatedAt,
            })
            .ToListAsync(ct);

        return ApiResponse<List<LicenseDto>>.Ok(licenses);
    }
}
