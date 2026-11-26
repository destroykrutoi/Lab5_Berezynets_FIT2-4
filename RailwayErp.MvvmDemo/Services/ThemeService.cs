namespace RailwayErp.MvvmDemo.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    void ApplySaved();
    void SetTheme(AppTheme theme);
}

public class ThemeService : IThemeService
{
    private const string Key = "app_theme";
    private readonly IAppStateService _state;

    public ThemeService(IAppStateService state) => _state = state;

    public AppTheme CurrentTheme => Application.Current?.UserAppTheme ?? AppTheme.Unspecified;

    public void ApplySaved()
    {
        var saved = Preferences.Get(Key, nameof(AppTheme.Unspecified));
        if (Enum.TryParse(saved, out AppTheme theme) && Application.Current is not null)
            Application.Current.UserAppTheme = theme;
    }

    public void SetTheme(AppTheme theme)
    {
        if (Application.Current is null) return;
        // тема застосовується до всього застосунку, а отже — до всіх сторінок
        Application.Current.UserAppTheme = theme;
        Preferences.Set(Key, theme.ToString());
        _state.Log($"Тему змінено на {theme}, вибір збережено в Preferences");
    }
}
