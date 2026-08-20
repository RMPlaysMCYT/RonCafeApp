using Avalonia;
using System;
using Grpc.Core;
using RonCafeApp.Services;
using RonCafeApp.ViewModels;

namespace RonCafeApp;

class Program
{
    // Static variable to hold the gRPC server instance
    private static Server? _grpcServer;

    [STAThread]
    public static void Main(string[] args)
    {
        StartGrpcServer();
        
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    private void StartGrpcServer()
    {
        // Ensure WindowSelectionService is initialized
        var windowService = new WindowSelectionService();
        var adminService = new AdminServiceImpl(windowService);

        _grpcServer = new Server
        {
            Services = { AdminService.BindService(adminService) },
            Ports = { new ServerPort("localhost", 50051, ServerCredentials.Insecure) } // Use Insecure for local dev
        };
        _grpcServer.Start();

        Console.WriteLine("gRPC Admin server listening on port 50051");
    }

    protected void OnExit(EventArgs e)
    {
        _grpcServer?.ShutdownAsync().Wait();
        base.OnExit(e);
    }
}