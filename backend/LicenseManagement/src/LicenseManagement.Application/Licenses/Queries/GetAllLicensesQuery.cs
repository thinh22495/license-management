using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using LicenseManagement.Application.Licenses.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Licenses.Queries;

public class GetAllLicensesQuery : IRequest<PagedResult<LicenseDto>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public Guid? ProductId { get; set; }
    public string? Search { get; set; }
}

public class GetAllLicensesQueryHandler : IRequestHandler<GetAllLicensesQuery, PagedResult<LicenseDto>>
{
    private readonly IAppDbContext _context;

    public GetAllLicensesQueryHandler(IAppDbContext context) => _context = context;

    public async Task<PagedResult<LicenseDto>> Handle(GetAllLicensesQuery request, CancellationToken ct)
    {
        var query = _context.UserLicenses
            .Include(ul => ul.User)
            .Include(ul => ul.LicenseProduct)
                .ThenInclude(lp => lp.Product)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<Domain.Enums.LicenseStatus>(request.Status, true, out var status))
            query = query.Where(ul => ul.Status == status);

        if (request.ProductId.HasValue)
            query = query.Where(ul => ul.LicenseProduct.ProductId == request.ProductId.Value);

        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(ul => ul.LicenseKey.Contains(request.Search) || (ul.User != null && ul.User.Email.Contains(request.Search)));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(ul => ul.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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

        return new PagedResult<LicenseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
