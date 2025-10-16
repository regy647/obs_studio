using System.Threading.Channels;
using TestSonioxLocal.Hubs;
using TestSonioxLocal.Models;
using TestSonioxLocal.Models.Enums;
using TestSonioxLocal.Services;
using TestSonioxLocal.Services.HttpClients;


var builder = WebApplication.CreateBuilder(args);

// Check command line arguments for protocol choice
var useHttps = args.Contains("--https") || args.Contains("-s");
var useHttp = args.Contains("--http") || args.Contains("-h");

// Also check launchSettings.json for Rider/Visual Studio compatibility
if (!useHttps && !useHttp)
{
    var urls = builder.Configuration["urls"] ?? builder.Configuration["applicationUrl"];
    if (urls != null)
    {
        if (urls.Contains("https://"))
        {
            useHttps = true;
            Console.WriteLine("🔍 Detected HTTPS from launchSettings.json");
        }
        else if (urls.Contains("http://"))
        {
            useHttp = true;
            Console.WriteLine("🔍 Detected HTTP from launchSettings.json");
        }
    }
}

// Default to HTTP if no protocol specified
if (!useHttps && !useHttp)
{
    useHttp = true;
}

// Configure Kestrel based on protocol choice
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    if (useHttps)
    {
        // HTTPS configuration - better for styling but may not work in OBS
        serverOptions.ListenLocalhost(50001, listenOptions =>
        {
            listenOptions.UseHttps();
        });
        Console.WriteLine("🔒 Starting with HTTPS on port 50001 (better styling support)");
        Console.WriteLine("   URL: https://localhost:50001");
        Console.WriteLine("   Note: May not display in OBS Browser Source");
    }
    else
    {
        // HTTP configuration - works in OBS but styling may not persist
        serverOptions.ListenLocalhost(50000);
        Console.WriteLine("🌐 Starting with HTTP on port 50000 (OBS compatible)");
        Console.WriteLine("   URL: http://localhost:50000");
        Console.WriteLine("   Note: Styling may not persist in OBS Browser Source");
    }
});

// Override the default URLs to prevent conflicts with launchSettings.json
if (useHttps)
{
    builder.WebHost.UseUrls("https://localhost:50001");
}
else
{
    builder.WebHost.UseUrls("http://localhost:50000");
}

Console.WriteLine("💡 Usage:");
Console.WriteLine("   dotnet run --http    or  dotnet run -h  (OBS compatible, styling issues)");
Console.WriteLine("   dotnet run --https   or  dotnet run -s  (Better styling, may not work in OBS)");
Console.WriteLine();


//vhodni 
Channel<WsMessage> loopbackSendChannel = Channel.CreateBounded<WsMessage>(
    new BoundedChannelOptions(capacity: 1000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
builder.Services.AddSingleton(loopbackSendChannel);

//vhodni
Channel<WsMessage> micSendChannel = Channel.CreateBounded<WsMessage>(
    new BoundedChannelOptions(capacity: 1000)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false
    });
builder.Services.AddSingleton(micSendChannel);

//izhodni 
Channel<CaptionMessage> captionChannel = Channel.CreateBounded<CaptionMessage>(
    new BoundedChannelOptions(capacity: 1000)
    {
        FullMode = BoundedChannelFullMode.DropOldest
    });
builder.Services.AddSingleton(captionChannel);

builder.Services.AddHttpClient<ISonioxHttpClient, SonioxHttpClient>();

// OBS WebSocket Service
builder.Services.Configure<TestSonioxLocal.Services.OBSWebSocketOptions>(builder.Configuration.GetSection("OBSWebSocket"));
builder.Services.AddHttpClient<TestSonioxLocal.Services.IOBSWebSocketService, TestSonioxLocal.Services.OBSWebSocketService>();

builder.Services.AddScoped<ICaptureAudioService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<CaptureAudioService>>();
    var sonioxWsService = provider.GetRequiredService<ILoopbackSonioxWsService>();
    return new CaptureAudioService(logger, loopbackSendChannel, sonioxWsService, ECaptureSourceType.Loopback);
});

builder.Services.AddScoped<ICaptureAudioService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<CaptureAudioService>>();
    var sonioxWsService = provider.GetRequiredService<IMicSonioxWsService>();
    return new CaptureAudioService(logger, micSendChannel, sonioxWsService, ECaptureSourceType.Microphone);
});

builder.Services.AddSingleton<ILoopbackSonioxWsService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<SonioxWsService>>();
    var sonioxHttpClient = provider.GetRequiredService<ISonioxHttpClient>();

    return new SonioxWsService(logger, sonioxHttpClient, loopbackSendChannel, captionChannel);
});

builder.Services.AddSingleton<ISonioxWsService>(provider =>
    provider.GetRequiredService<ILoopbackSonioxWsService>());

builder.Services.AddSingleton<IMicSonioxWsService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<SonioxWsService>>();
    var sonioxHttpClient = provider.GetRequiredService<ISonioxHttpClient>();

    return new SonioxWsService(logger, sonioxHttpClient, micSendChannel, captionChannel);
});

builder.Services.AddSingleton<ISonioxWsService>(provider =>
    provider.GetRequiredService<IMicSonioxWsService>());

builder.Services.AddScoped<ICaptionService, CaptionService>();

// Register InitService as both a hosted service AND a singleton so we can access it from CaptionHub
builder.Services.AddSingleton<InitService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<InitService>());

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();


var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Only redirect to HTTPS if we're running in HTTPS mode
if (useHttps)
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapRazorPages();
app.MapHub<CaptionHub>("/caption-hub");

// Redirect root to settings page for better user experience
app.MapGet("/", () => Results.Redirect("/settings"));

app.Run();
