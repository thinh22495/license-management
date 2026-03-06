using LicenseManagement.Application.Products.Commands;
using LicenseManagement.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LicenseManagement.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[SwaggerTag("Quản lý sản phẩm — tạo, cập nhật, xoá và tra cứu thông tin sản phẩm")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [SwaggerOperation(
        Summary = "Lấy danh sách sản phẩm",
        Description = "Trả về danh sách sản phẩm có phân trang. Hỗ trợ tìm kiếm theo tên và lọc theo trạng thái hoạt động."
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsQuery query)
    {
        return Ok(await _mediator.Send(query));
    }

    [HttpGet("{slug}")]
    [SwaggerOperation(
        Summary = "Lấy thông tin sản phẩm theo slug",
        Description = "Trả về chi tiết một sản phẩm dựa trên slug. Trả về 404 nếu không tìm thấy sản phẩm."
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(string slug)
    {
        var result = await _mediator.Send(new GetProductBySlugQuery { Slug = slug });
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Tạo sản phẩm mới",
        Description = "Tạo một sản phẩm mới trong hệ thống. Yêu cầu quyền Admin. Slug sẽ được tự động tạo từ tên nếu không được cung cấp."
    )]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success ? Created("", result) : BadRequest(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Cập nhật sản phẩm",
        Description = "Cập nhật thông tin sản phẩm theo ID. Yêu cầu quyền Admin. Chỉ các trường được gửi mới được cập nhật."
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(
        Summary = "Xoá sản phẩm",
        Description = "Xoá sản phẩm theo ID. Yêu cầu quyền Admin. Trả về 404 nếu không tìm thấy sản phẩm."
    )]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await _mediator.Send(new DeleteProductCommand { Id = id });
        return result.Success ? Ok(result) : NotFound(result);
    }
}
