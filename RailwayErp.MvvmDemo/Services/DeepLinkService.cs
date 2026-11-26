namespace RailwayErp.MvvmDemo.Services;

public interface IDeepLinkService
{
    Task<bool> HandleAsync(Uri uri);
    Task HandlePendingAsync();
}

// Обробка посилань виду railwayerp://train/{id}
public class DeepLinkService : IDeepLinkService
{
    public const string Scheme = "railwayerp";

    // посилання, отримане платформою до створення Shell
    public static Uri? PendingUri { get; set; }

    private readonly IAppStateService _state;

    public DeepLinkService(IAppStateService state) => _state = state;

    public async Task HandlePendingAsync()
    {
        if (PendingUri is null) return;
        var uri = PendingUri;
        PendingUri = null;
        await HandleAsync(uri);
    }

    public async Task<bool> HandleAsync(Uri uri)
    {
        _state.Log($"Отримано deep link: {uri}");

        if (!uri.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase))
            return false;

        // для railwayerp://train/3 Host = "train", шлях = "/3"
        var segment = uri.AbsolutePath.Trim('/');
        if (uri.Host.Equals("train", StringComparison.OrdinalIgnoreCase) && int.TryParse(segment, out var id))
        {
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync($"//routes/routeedit?id={id}"));
            _state.Log($"Deep link: відкрито маршрут з Id = {id}");
            return true;
        }

        _state.Log("Deep link не розпізнано");
        return false;
    }
}
