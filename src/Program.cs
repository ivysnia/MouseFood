using MouseFood;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Enable Windows Forms for system tray
System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.SystemAware);
System.Windows.Forms.Application.EnableVisualStyles();

// Notify user the app is starting
Console.WriteLine("MouseFood - Razer Mouse Battery Monitor");
Console.WriteLine("Starting... Look for the tray icon in your system tray!");
Console.WriteLine("Right-click the tray icon to refresh or exit.");
Console.WriteLine();

// Use the generic host so we can call UseWindowsService()
var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((context, services) =>
    {
        // Bind settings from appsettings.json
        services.Configure<MouseFoodSettings>(
            context.Configuration.GetSection("MouseFoodSettings"));

        // Register services
        services.AddSingleton<SynapseLogParser>();
        services.AddSingleton<RazerBatteryReader>();
        services.AddSingleton<TrayIconManager>();
        services.AddHostedService<Worker>();
    })
    .Build();

// CRITICAL: Create TrayIconManager on the main thread before starting the message loop
// This ensures NotifyIcon and its context menu are created on the UI thread
var trayIcon = host.Services.GetRequiredService<TrayIconManager>();

// Start the host in a background thread
var hostTask = Task.Run(() => host.RunAsync());

// Run the Windows Forms message loop on the main thread (required for NotifyIcon context menus)
System.Windows.Forms.Application.Run();

// Wait for the host to shutdown
await hostTask;
