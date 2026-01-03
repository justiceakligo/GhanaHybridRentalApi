using GhanaHybridRentalApi.Services;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Models;
using Microsoft.AspNetCore.Authorization;
using GhanaHybridRentalApi.Extensions; // Absolutize URL helper

namespace GhanaHybridRentalApi.Endpoints;

public static class FileUploadEndpoints
{
    public static void MapFileUploadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/upload/single", UploadSingleFileAsync)
            .DisableAntiforgery();

        app.MapPost("/api/v1/upload/multiple", UploadMultipleFilesAsync)
            .DisableAntiforgery();
        
            // Allow anonymous uploads for guest booking flow (keep anti-forgery disabled)
            app.MapPost("/api/v1/upload/anonymous", UploadSingleFileAnonymousAsync)
                .DisableAntiforgery();
    }

    private static async Task<IResult> UploadSingleFileAsync(
        HttpRequest request,
        IFormFile file,
        IFileUploadService fileUploadService,
        AppDbContext db)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "No file provided" });

        // Validate file size (10MB max)
        if (file.Length > 10 * 1024 * 1024)
            return Results.BadRequest(new { error = "File size exceeds 10MB limit" });

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid file type. Only JPEG, PNG, and PDF allowed" });

        try
        {
            using var stream = file.OpenReadStream();
            var url = await fileUploadService.UploadFileAsync(stream, file.FileName, file.ContentType);

            // Convert relative URLs to absolute so frontends on other origins can display them
            var absoluteUrl = request.AbsolutizeUrl(url);

            // Persist a Document row
            var document = new Document
            {
                FileName = file.FileName,
                Url = absoluteUrl,
                ContentType = file.ContentType,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };
            db.Documents.Add(document);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                fileName = file.FileName,
                url = absoluteUrl,
                size = file.Length,
                contentType = file.ContentType,
                id = document.Id
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }
    private static async Task<IResult> UploadSingleFileAnonymousAsync(
        HttpRequest request,
        IFormFile file,
        IFileUploadService fileUploadService,
        AppDbContext db)
    {
        // Reuse same validations as authenticated endpoint but allow anonymous access
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "No file provided" });

        // Validate file size (10MB max)
        if (file.Length > 10 * 1024 * 1024)
            return Results.BadRequest(new { error = "File size exceeds 10MB limit" });

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            return Results.BadRequest(new { error = "Invalid file type. Only JPEG, PNG, and PDF allowed" });

        try
        {
            using var stream = file.OpenReadStream();
            var url = await fileUploadService.UploadFileAsync(stream, file.FileName, file.ContentType);

            var absoluteUrl = request.AbsolutizeUrl(url);

            var document = new Document
            {
                FileName = file.FileName,
                Url = absoluteUrl,
                ContentType = file.ContentType,
                Size = file.Length,
                UploadedAt = DateTime.UtcNow
            };
            db.Documents.Add(document);
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                fileName = file.FileName,
                url = absoluteUrl,
                size = file.Length,
                contentType = file.ContentType,
                id = document.Id
            });
        }
        catch (Exception ex)
        {
            return Results.Problem(detail: ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> UploadMultipleFilesAsync(
        HttpRequest request,
        IFormFileCollection files,
        IFileUploadService fileUploadService,
        AppDbContext db)
    {
        if (files is null || files.Count == 0)
            return Results.BadRequest(new { error = "No files provided" });

        if (files.Count > 10)
            return Results.BadRequest(new { error = "Maximum 10 files allowed per request" });

        var uploadedFiles = new List<object>();
        var errors = new List<string>();

        foreach (var file in files)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                errors.Add($"{file.FileName}: File size exceeds 10MB");
                continue;
            }

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "application/pdf" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                errors.Add($"{file.FileName}: Invalid file type");
                continue;
            }

            try
            {
                using var stream = file.OpenReadStream();
                var url = await fileUploadService.UploadFileAsync(stream, file.FileName, file.ContentType);

                var absoluteUrl = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? url
                    : $"{request.Scheme}://{request.Host}{url}";

                var document = new Document
                {
                    FileName = file.FileName,
                    Url = absoluteUrl,
                    ContentType = file.ContentType,
                    Size = file.Length,
                    UploadedAt = DateTime.UtcNow
                };
                db.Documents.Add(document);
                await db.SaveChangesAsync();

                uploadedFiles.Add(new
                {
                    fileName = file.FileName,
                    url = absoluteUrl,
                    size = file.Length,
                    contentType = file.ContentType,
                    id = document.Id
                });
            }
            catch (Exception ex)
            {
                errors.Add($"{file.FileName}: {ex.Message}");
            }
        }

        return Results.Ok(new
        {
            uploadedCount = uploadedFiles.Count,
            files = uploadedFiles,
            errors = errors.Any() ? errors : null
        });
    }
}
