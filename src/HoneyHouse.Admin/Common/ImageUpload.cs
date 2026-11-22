using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Catalog;

namespace HoneyHouse.Admin.Common;

public static class ImageUpload
{
    private static readonly HashSet<string> Allowed = ["image/jpeg", "image/png", "image/webp"];

    public static bool IsValid(IFormFile file) =>
        Allowed.Contains(file.ContentType) && file.Length is > 0 and <= ProductAdminService.MaxImageBytes;

    public static Error Invalid => Error.Validation("image_invalid", "file");
}
