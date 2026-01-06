using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Tarot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class FilesController(IFileStorageService fileStorage) : ControllerBase
{
    private readonly IFileStorageService _fileStorage = fileStorage;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            if (file == null) return BadRequest("No file uploaded");

            using var stream = file.OpenReadStream();
            // Only allow images for now
            var url = await _fileStorage.SaveFileAsync(stream, file.FileName, AllowedExtensions);
            return Ok(new { Url = url });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}