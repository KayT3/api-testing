using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Playwright;

var builder = WebApplication.CreateBuilder(args);
var http = new HttpClient();
var playwright = await Playwright.CreateAsync();
var browser = await playwright.Chromium.LaunchAsync(
    new BrowserTypeLaunchOptions { Headless = true });
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.Urls.Add("http://0.0.0.0:8111");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/echoBody", async (HttpRequest req) =>
{
    Console.WriteLine(req.Host.Host);
    using var reader = new StreamReader(req.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Text(body, "application/json");
});

static byte[] Base64UrlDecode(string input)
{
    input = input.Replace('-', '+').Replace('_', '/');
    switch (input.Length % 4)
    {
        case 2: input += "=="; break;
        case 3: input += "="; break;
    }

    return Convert.FromBase64String(input);
}

async Task<IResult> ImageResponse(string url)
{
    try
    {
        var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode) return Results.BadRequest($"Image fetch failed: {(int)resp.StatusCode}");
        var contentType = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var bytesOut = await resp.Content.ReadAsByteArrayAsync();
        return Results.File(bytesOut, contentType);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        return Results.BadRequest("invalid");
    }
}

app.MapGet("/html2canvas/{b64}.png", async (string b64) =>
{
    string json;
    try
    {
        json = Encoding.UTF8.GetString(Base64UrlDecode(b64));
    }
    catch (FormatException)
    {
        Console.WriteLine($"{b64} ||Invalid base64 in URL");
        return Results.BadRequest("Invalid base64 in URL.");
    }

    Html2CanvasReq? req;
    try
    {
        req = JsonSerializer.Deserialize<Html2CanvasReq>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
    catch (JsonException)
    {
        return await ImageResponse(
            "https://th.bing.com/th/id/OIP.YU4UmFmovboXAc9VYet8ZwHaE4?o=7rm=3&rs=1&pid=ImgDetMain&o=7&rm=3");
    }

    if (req is null)
        return await ImageResponse(
            "https://th.bing.com/th/id/OIP.YU4UmFmovboXAc9VYet8ZwHaE4?o=7rm=3&rs=1&pid=ImgDetMain&o=7&rm=3");


    var page = await browser.NewPageAsync();
    var html2CanvasJs = await GetResource("html2canvas.js");
    var styleJs = await GetResource("style.js");
    await page.GotoAsync(req.url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    await page.AddScriptTagAsync(new() { Content = html2CanvasJs });
    await page.WaitForFunctionAsync("() => typeof window.html2canvas !== 'undefined'");
    
    var script = @"
async ({ selector }) => {
    const element = document.querySelector(selector);
    if (!element) return null;

    for (let scale = 1.0; scale >= 0.3; scale -= 0.1) {
        const canvas = await html2canvas(element, { scale,backgroundColor: '#000000',useCORS: true,allowTaint: false, });
        const res = canvas.toDataURL('image/png');

        if (res && res !== 'data:,') {
            return res;
        }
    }

    return null;
}
";
    await page.EvaluateAsync(styleJs, req);


    await page.WaitForTimeoutAsync(1000);

    var r = await page.EvaluateAsync<string>(
        script,
        new { selector = req.qSelector }
    );
    await page.CloseAsync();
    if (string.IsNullOrEmpty(r) || !r.StartsWith("data:image/png;base64,"))
    {
        return await ImageResponse(req.fallbackImage);
    }

    var base64 = r.Replace("data:image/png;base64,", "");

    try
    {
        var bytes = Convert.FromBase64String(base64);
        return Results.File(bytes, "image/png");
    }
    catch
    {
        return await ImageResponse(req.fallbackImage);
    }
});

async Task<string> GetResource(string name)
{
    var assembly = Assembly.GetExecutingAssembly();
    await using var stream = assembly.GetManifestResourceStream($"WebApplication1.Resources.{name}");
    using var reader = new StreamReader(stream!);
    return reader.ReadToEnd();
}

app.Run();

public record Html2CanvasReq(string qSelector, string url, string? scripts, string fallbackImage,Html2CanvasReqStyle? style);
public record Html2CanvasReqStyle(
    List<string> hide,
    string fontSize,
    string addMarginTopTo,
    string marginTop
);