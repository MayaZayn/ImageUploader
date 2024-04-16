using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder();
builder.Services.AddAntiforgery();
var app = builder.Build();

app.UseAntiforgery();
app.UseStaticFiles();

app.MapGet("/", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    var html = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Image Uploader</title>
            <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-EVSTQN3/azprG1Anm3QDgpJLIm9Nao0Yz1ztcQTwFspd3yD65VohhpuuCOmLASjC" crossorigin="anonymous">
        </head>
        <body>
            <main class="container">
            <h1 class="my-3">Image Uploader</h1>
            <form action="upload" method="POST" enctype="multipart/form-data">
                <div class="mb-3">
                <input name="{token.FormFieldName}" type="hidden" value="{token.RequestToken}" />
                <input name="title" type="text" class="form-control" id="imageTitle" placeholder="Enter the image's title" required>
                </div>
                <div class="mb-3">
                <label for="file" class="form-label">Pick an Image</label>
                <input name="file"" class="form-control" type="file" id="file" required>
                </div>
                <button type="submit" class="btn btn-primary">Upload Image</button>
            </form>
            </main>

            <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.0.2/dist/js/bootstrap.bundle.min.js" integrity="sha384-MrcW6ZMFYlzcLA8Nl+NtUVF0sA7MsXsP1UyJoMp4YLEuNSfAP+JcXn/tWtIaxVXM" crossorigin="anonymous"></script>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html");
});

app.MapPost("/upload", async (
                            [FromForm] string title, 
                            IFormFile file, 
                            IWebHostEnvironment env
                            )
    => {
        string imageTitle = title;
        string fileExtension = Path.GetExtension(file.FileName).ToLower();

        // validate extension
        if (fileExtension != ".png" && fileExtension != ".jpg" && fileExtension != ".gif" && fileExtension != ".jpeg")
        {
            return Results.LocalRedirect("/");
        }

        // initialize json
        var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "images.json");
        if (!File.Exists(jsonPath))
        {
            var initialJson = "[]";
            await File.WriteAllTextAsync(jsonPath, initialJson);
        }

        // upload image
        if (file is not null && file.Length > 0)
        {
            string uniqueId = Guid.NewGuid().ToString();
            string url = "/picture/" + uniqueId;

            var folder = Path.Combine(env.ContentRootPath, "uploaded");
            if (Directory.Exists(folder) is false)
                Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, uniqueId + fileExtension);
            using var stream = System.IO.File.OpenWrite(path);
            await file.CopyToAsync(stream); 

            ImageInfo imageInfo = new()
            {
                Title = title,
                Id = uniqueId,
                Path = Path.Combine("uploaded", uniqueId),
                Extension = fileExtension,
            };

            // update json
            string json = await File.ReadAllTextAsync(jsonPath);
            var imagesInfo = JsonSerializer.Deserialize<List<ImageInfo>>(json);
            imagesInfo.Add(imageInfo);

            var updatedJson = JsonSerializer.Serialize(imagesInfo);
            await File.WriteAllTextAsync(jsonPath, updatedJson);

            return Results.LocalRedirect($"/picture/{uniqueId}");
        } else {
            return Results.LocalRedirect("/");
        }
    });

app.MapGet("/picture/{uniqueId}", async ([FromRoute] string uniqueId) => {
    var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "images.json");
    string json = await File.ReadAllTextAsync(jsonPath);
    var imagesInfo = JsonSerializer.Deserialize<List<ImageInfo>>(json);

    string imagePath = null, imageTitle = null;
    foreach (var image in imagesInfo)
    {
        if (image.Id == uniqueId) 
        {
            imagePath = image.Path;
            imageTitle = image.Title;
            break;
        }
    }

    var html = $"""
        <h1 style="text-align:center">{imageTitle}</h1>
        <img style="display:block; margin:auto; width:800px; height:600px" src="/{imagePath}" alt="{imageTitle}"/>
    """;

    return Results.Content(html, "text/html");
});

app.MapGet("/uploaded/{uniqueId}", async ([FromRoute] string uniqueId) =>
{
    var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "images.json");
    string json = await File.ReadAllTextAsync(jsonPath);
    var imagesInfo = JsonSerializer.Deserialize<List<ImageInfo>>(json);

    var imgQuery =
        from img in imagesInfo
        where img.Id == uniqueId
        select img;

    string imagePath = null, imageExtension = null;
    foreach (var image in imgQuery)
    {
        imagePath = Path.Combine(builder.Environment.ContentRootPath, image.Path + image.Extension);
        imageExtension = image.Extension;
    }

    if (File.Exists(imagePath))
    {
        try
        {
            FileStream file = File.OpenRead(imagePath);
            return Results.File(file, imageExtension);
        }
        catch(Exception ex)
        {
            return Results.Problem(ex.Message ?? string.Empty);
        }
    }
    else
    {
        return Results.NotFound();
    }
});

app.Run();

public class ImageInfo
{
    public string Title { get; set; }
    public string Path { get; set; }
    public string Id { get; set; }
    public string Extension { get; set; }
}
