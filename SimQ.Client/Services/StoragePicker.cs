using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace SimQ.Client.Services;

/// <summary>
/// Thin wrapper around <see cref="IStorageProvider"/> that resolves the
/// application's main window automatically. Centralises the boilerplate that
/// was previously duplicated across import/export commands.
/// </summary>
internal static class StoragePicker
{
    private static Window? GetMainWindow()
        => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public static async Task<IStorageFile?> SaveAsync(
        string title,
        string defaultExtension,
        string suggestedName,
        FilePickerFileType fileType)
    {
        var window = GetMainWindow();
        if (window is null) return null;

        return await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = defaultExtension,
            SuggestedFileName = suggestedName,
            FileTypeChoices = new[] { fileType },
        });
    }

    public static async Task<IStorageFile?> OpenAsync(
        string title,
        FilePickerFileType fileType,
        bool includeAllFiles = true)
    {
        var window = GetMainWindow();
        if (window is null) return null;

        var filters = includeAllFiles
            ? new[] { fileType, FilePickerFileTypes.All }
            : new[] { fileType };

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters,
        });

        return files.Count == 0 ? null : files[0];
    }

    public static FilePickerFileType Json
        => new("JSON") { Patterns = new[] { "*.json" } };

    public static FilePickerFileType Csv
        => new("CSV") { Patterns = new[] { "*.csv" } };
}
