using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles();

app.MapGet("/", () =>
{
    var htmlPath = Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "index.html");

    if (File.Exists(htmlPath))
    {
        return Results.File(htmlPath, "text/html");
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapGet("/js/{path}", (string path) =>
{
    var jsFilePath = Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "js", path);

    if (File.Exists(jsFilePath))
    {
        return Results.File(jsFilePath, "text/javascript");
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapGet("/css/{path}", (string path) => 
{
    var cssFilePath = Path.Combine(builder.Environment.ContentRootPath, "ClientApp", "css", path);

    if (File.Exists(cssFilePath))
    {
        return Results.File(cssFilePath, "text/css");
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapPost("/upload", async (ImageInfo imageInfo) => {
    var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "images.json");
    
    if (!File.Exists(jsonPath))
    {
        var initialJson = "[]";
        await File.WriteAllTextAsync(jsonPath, initialJson);
    }

    string json = await File.ReadAllTextAsync(jsonPath);
    var imagesInfo = JsonSerializer.Deserialize<List<ImageInfo>>(json);
    imagesInfo.Add(imageInfo);

    // Save new list of images to file
    var updatedJson = JsonSerializer.Serialize(imagesInfo);
    await File.WriteAllTextAsync(jsonPath, updatedJson);

    return Results.Accepted();
});

app.MapGet("/picture/{uniqueId}", async (HttpContext context, string uniqueId) => {
    var jsonPath = Path.Combine(builder.Environment.ContentRootPath, "images.json");
    string json = await File.ReadAllTextAsync(jsonPath);
    var imagesInfo = JsonSerializer.Deserialize<List<ImageInfo>>(json);

    string imageContent = null, imageTitle = null;
    foreach (var image in imagesInfo)
    {
        if (image.Id == uniqueId) {
            imageContent = image.Content;
            imageTitle = image.Title;
            break;
        }
    }

    var htmlImage = $"<h1 style=\"text-align:center\">{imageTitle}</h1> <img style=\"display:block; margin:auto; width:800px; height:600px\" src=\"{imageContent}\" alt=\"{imageTitle}\"/>";

    context.Response.ContentType = "text/html";
    return context.Response.WriteAsync(htmlImage);
});

app.Run();

public class ImageInfo
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string Id { get; set; }
}
