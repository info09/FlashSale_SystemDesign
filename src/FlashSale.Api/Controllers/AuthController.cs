using FlashSale.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlashSale.Api.Controllers;

/// <summary>
/// Controller quan ly xac thuc va dang ky tai khoan (Auth).
/// Thuat ngua chuan theo docs/domain-glossary.md: User, BearerToken, ApiGateway.
/// Khong duoc goi la: AccountController, AuthApi, LoginController.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[ApiExplorerSettings(GroupName = "v1")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Dang nhap client va tra ve Access Token (Bearer Token/JWT).
/// Neu xac thuc thanh cong, tra ve token de goi cac endpoint can phan quyen (FR-01, NFR-04).
    /// </summary>
    /// <param name="command">Thong tin dang nhap: UserName va Password.</param>
    /// <returns>Access Token kèm thong tin nguoi dung.</returns>
    /// <response code="200">Dang nhap thanh cong, tra ve Access Token.</response>
    /// <response code="401">UserName hoac Password khong chinh xac.</response>
    /// <response code="400">Yeu khong hop le.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new
            {
                status = "success",
                code = "LOGIN_SUCCESS",
                message = "Dang nhap thanh cong.",
                accessToken = result.AccessToken,
                tokenType = result.TokenType,
                userId = result.UserId,
                userName = result.UserName,
                email = result.Email,
                expiresAt = result.ExpiresAt
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                status = "error",
                code = "UNAUTHORIZED",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Dang ki tai khoan moi cho client.
/// Neu thanh cong, tra ve thong tin User profile da tao (FR-01, NFR-04).
    /// </summary>
    /// <param name="command">Thong tin dang ki: UserName, Email, Password.</param>
    /// <returns>Thong tin User profile da tao.</returns>
    /// <response code="200">Dang ki thanh cong.</response>
    /// <response code="400">UserName hoac Email da ton tai, hoac yeu khong hop le.</response>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var user = await _mediator.Send(command);
        return Ok(new
        {
            status = "success",
            code = "USER_REGISTERED",
            message = "Dang ki tai khoan thanh cong.",
            userId = user.Id,
            userName = user.UserName,
            email = user.Email
        });
    }
}
