using Grpc.Core;
using RonCafeApp.Grpc; // This is your generated gRPC namespace
using RonCafeApp.Services; // Add this for WindowSelectionService
using System.Threading.Tasks;
using Avalonia.Threading;

namespace RonCafeApp.Services // Add proper namespace
{
    public class AdminServiceImpl : AdminService.AdminServiceBase
    {
        private readonly WindowSelectionService _windowService;

        // Inject the service responsible for changing windows
        public AdminServiceImpl(WindowSelectionService windowService)
        {
            _windowService = windowService;
        }

        public override async Task<CommandResponse> SelectWindow(WindowRequest request, ServerCallContext context)
        {
            var response = new CommandResponse();

            // UI operations must be run on the UI thread
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    _windowService.ApplyWindowSelection(request.WindowType);
                    response.Success = true;
                    response.Message = $"Switched to {request.WindowType}";
                }
                catch (System.Exception ex)
                {
                    response.Success = false;
                    response.Message = $"Error: {ex.Message}";
                }
            });

            return response;
        }
    }
}