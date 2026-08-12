using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VideoCallApp.API.Models;
using VideoCallApp.Application.Common;
using VideoCallApp.Application.DTOs.Auth;
using VideoCallApp.Application.Interfaces;
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
    private readonly IPhotoService _photoService;

    public UsersController(UserManager<User> userManager, IPhotoService photoService)
    {
        _userManager = userManager;
        _photoService = photoService;
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

        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl) && user.ProfilePictureUrl.Contains("cloudinary.com"))
        {
            await _photoService.DeletePhotoAsync(user.ProfilePictureUrl);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var photoUrl = await _photoService.AddPhotoAsync(stream, file.FileName);
            if (photoUrl == null)
            {
                return BadRequest(ApiResponse<ProfileImageResponseDto>.ErrorResponse(
                    "Profile image upload failed",
                    "Could not upload image to cloud storage"));
            }
            user.ProfilePictureUrl = photoUrl;
            await _userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<ProfileImageResponseDto>.ErrorResponse(
                "Profile image upload failed",
                ex.Message));
        }

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

        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl) && user.ProfilePictureUrl.Contains("cloudinary.com"))
        {
            await _photoService.DeletePhotoAsync(user.ProfilePictureUrl);
        }
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
}
