using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VideoCallApp.API.Models;
using VideoCallApp.Application.Common;
using VideoCallApp.Application.DTOs.Auth;
using VideoCallApp.Domain.Entities;

namespace VideoCallApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png"
    };

    private const long MaxProfileImageSize = 2 * 1024 * 1024;

    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _environment;

    public UsersController(UserManager<User> userManager, IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _environment = environment;
    }

    [HttpPost("upload-profile-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxProfileImageSize)]
    public async Task<IActionResult> UploadProfileImage([FromForm] UploadProfileImageRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<ProfileImageResponseDto>.ErrorResponse(
                "Profile image upload failed",
                "Please select an image file"));
        }

        if (file.Length > MaxProfileImageSize)
        {
            return BadRequest(ApiResponse<ProfileImageResponseDto>.ErrorResponse(
                "Profile image upload failed",
                "Profile image must be 2MB or smaller"));
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return BadRequest(ApiResponse<ProfileImageResponseDto>.ErrorResponse(
                "Profile image upload failed",
                "Only JPG, JPEG, and PNG files are allowed"));
        }

        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ApiResponse<ProfileImageResponseDto>.ErrorResponse(
                "Profile image upload failed",
                "User not found"));
        }

        var profileDirectory = GetProfileDirectory();
        Directory.CreateDirectory(profileDirectory);

        DeleteExistingProfileImage(user.ProfilePictureUrl);

        var fileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(profileDirectory, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        user.ProfilePictureUrl = $"/profile/{fileName}";
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse<ProfileImageResponseDto>.SuccessResponse(
            new ProfileImageResponseDto { ProfileImageUrl = user.ProfilePictureUrl },
            "Profile image updated successfully"));
    }

    [HttpDelete("profile-image")]
    public async Task<IActionResult> RemoveProfileImage()
    {
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return Unauthorized(ApiResponse<ProfileImageResponseDto>.ErrorResponse(
                "Profile image removal failed",
                "User not found"));
        }

        DeleteExistingProfileImage(user.ProfilePictureUrl);
        user.ProfilePictureUrl = null;
        await _userManager.UpdateAsync(user);

        return Ok(ApiResponse<ProfileImageResponseDto>.SuccessResponse(
            new ProfileImageResponseDto { ProfileImageUrl = null },
            "Profile image removed successfully"));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrWhiteSpace(userId) ? null : await _userManager.FindByIdAsync(userId);
    }

    private string GetProfileDirectory()
    {
        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        return Path.Combine(webRootPath, "profile");
    }

    private void DeleteExistingProfileImage(string? profileImageUrl)
    {
        if (string.IsNullOrWhiteSpace(profileImageUrl))
        {
            return;
        }

        var fileName = Path.GetFileName(profileImageUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var existingPath = Path.Combine(GetProfileDirectory(), fileName);
        if (System.IO.File.Exists(existingPath))
        {
            System.IO.File.Delete(existingPath);
        }
    }
}
