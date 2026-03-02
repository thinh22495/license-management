using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.LicensePlans.Commands;

public class DeleteLicensePlanCommand : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
}

public class DeleteLicensePlanCommandHandler : IRequestHandler<DeleteLicensePlanCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public DeleteLicensePlanCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse> Handle(DeleteLicensePlanCommand request, CancellationToken ct)
    {
        var plan = await _context.LicenseProducts.FirstOrDefaultAsync(lp => lp.Id == request.Id, ct);
        if (plan == null)
            return ApiResponse.Fail("Gói license không tồn tại");

        _context.LicenseProducts.Remove(plan);
        await _context.SaveChangesAsync(ct);

        return ApiResponse.Ok("Đã xóa gói license");
    }
}
