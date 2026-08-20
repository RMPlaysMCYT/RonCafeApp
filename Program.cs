using Avalonia;
using System;
using Grpc.Core;
using RonCafeApp.Services;
using RonCafeApp.ViewModels;
using RonCafeApp.Grpc; // Add this for the generated gRPC code

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

    private static void StartGrpcServer()
    {
        try
        {
            var windowService = new WindowSelectionService();
            var adminService = new AdminServiceImpl(windowService);

            _grpcServer = new Server
            {
                Services = { AdminService.BindService(adminService) },
                Ports = { new ServerPort("localhost", 50051, ServerCredentials.Insecure) }
            };
            _grpcServer.Start();

            Console.WriteLine("✅ gRPC Admin server listening on port 50051");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to start gRPC server: {ex.Message}");
        }
    }

    // This can be called from App.xaml.cs OnExit
    public static void ShutdownGrpcServer()
    {
        try
        {
            _grpcServer?.ShutdownAsync().Wait(TimeSpan.FromSeconds(5));
            _grpcServer = null;
            Console.WriteLine("✅ gRPC server shut down");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error shutting down gRPC server: {ex.Message}");
        }
    }
}