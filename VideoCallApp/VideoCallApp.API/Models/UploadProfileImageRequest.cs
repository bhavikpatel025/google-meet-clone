using Microsoft.AspNetCore.Http;

namespace VideoCallApp.API.Models;

public class UploadProfileImageRequest
{
    public IFormFile? File { get; set; }
}
