using LicenseManagement.Application.Common.Interfaces;
using LicenseManagement.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LicenseManagement.Application.Products.Commands;

public class DeleteProductCommand : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
}

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, ApiResponse>
{
    private readonly IAppDbContext _context;

    public DeleteProductCommandHandler(IAppDbContext context) => _context = context;

    public async Task<ApiResponse> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (product == null)
            return ApiResponse.Fail("Sản phẩm không tồn tại");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(ct);

        return ApiResponse.Ok("Đã xóa sản phẩm");
    }
}
