using System.Diagnostics;
using Aorms.Bridge;
using AormsConnect.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AormsConnect;

public sealed partial class MainWindow : Window
{
    readonly AormsBridge _bridge;
    readonly ProjectCatalogStore _catalog = new();

    static readonly (string Title, string PackageHint, string Repo)[] SuiteApps =
    [
        ("AStudio", "AStudio", "https://github.com/HolagundiWorks/AStudio"),
        ("AConsulting", "AConsulting", "https://github.com/HolagundiWorks/AConsulting"),
        ("AQC Estimation", "AQC-Estimation", "https://github.com/HolagundiWorks/AQC-Estimation"),
        ("AQC BBS", "AQC-BBS", "https://github.com/HolagundiWorks/AQC-BBS"),
        ("AQC Project Management", "AQC-PM", "https://github.com/HolagundiWorks/AQC-PM"),
        ("AADT", "AADT", "https://github.com/HolagundiWorks/AADT"),
    ];

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = false;
        _bridge = AormsBridgeHost.CreateFromEnvironment();
        BuildSuiteAppsUi();
        RefreshSession("Ready.");
        ReloadProjects_Click(this, new RoutedEventArgs());
    }

    void BuildSuiteAppsUi()
    {
        var panel = new StackPanel { Spacing = 6 };
        foreach (var (title, hint, repo) in SuiteApps)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new TextBlock
            {
                Text = title,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 220,
            });
            var openBtn = new Button { Content = "Open", Tag = hint };
            openBtn.Click += OpenSuiteApp_Click;
            var getBtn = new Button { Content = "Get", Tag = repo };
            getBtn.Click += GetSuiteApp_Click;
            row.Children.Add(openBtn);
            row.Children.Add(getBtn);
            panel.Children.Add(row);
        }
        SuiteAppsList.Items.Clear();
        SuiteAppsList.Items.Add(panel);
    }

    void RefreshSession(string? note = null)
    {
        var cfg = _bridge.HubConfigured();
        SessionStatusText.Text =
            $"hub={cfg.HubUrl}  hasSyncToken={cfg.HasSyncToken}  syncReady={cfg.SyncReady}";
        if (!string.IsNullOrWhiteSpace(note))
            LogText.Text = note;
    }

    void Refresh_Click(object sender, RoutedEventArgs e) => RefreshSession("Status refreshed.");

    async void Activate_Click(object sender, RoutedEventArgs e)
    {
        var key = LicenseKeyBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(key))
        {
            RefreshSession("Enter a licence key first.");
            return;
        }
        try
        {
            LogText.Text = "Activating…";
            var grant = await _bridge.ActivateAsync(key);
            var cfg = _bridge.HubConfigured();
            var (sync, hub, installId) = _bridge.Db.ReadAuth();
            ConnectSession.Write(new ConnectSessionFile
            {
                SyncToken = string.IsNullOrWhiteSpace(grant.SyncToken) ? (sync ?? "") : grant.SyncToken,
                HubUrl = string.IsNullOrWhiteSpace(hub) ? cfg.HubUrl : hub!,
                LicenseApiUrl = cfg.LicenseApiUrl,
                LicenseToken = grant.LicenseToken,
                DeviceId = installId,
                WrittenAt = DateTimeOffset.UtcNow.ToString("O"),
            });
            RefreshSession(
                $"Activate OK · session.json written · syncToken length={grant.SyncToken.Length}");
        }
        catch (Exception ex)
        {
            RefreshSession($"Activate failed: {ex.Message}");
        }
    }

    void OpenDownloads_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://aorms.in/downloads");

    void OpenSuiteApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string hint }) return;
        var sessionPath = ConnectSession.DefaultPath();
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", hint, $"{hint}.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), hint, $"{hint}.exe"),
        };
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                var args = File.Exists(sessionPath)
                    ? $"{ConnectSession.FlagConnectSession} \"{sessionPath}\""
                    : "";
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true,
                    Arguments = args,
                });
                RefreshSession($"Launched {hint} with Connect session");
                return;
            }
        }
        RefreshSession($"Not installed locally ({hint}). Use Get → downloads / GitHub.");
    }

    void GetSuiteApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string url }) return;
        OpenUrl(url);
    }

    void AddProject_Click(object sender, RoutedEventArgs e)
    {
        var title = NewProjectTitleBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(title))
        {
            RefreshSession("Enter a project title.");
            return;
        }
        var row = _catalog.Add(title);
        NewProjectTitleBox.Text = "";
        RefreshSession($"Added {row.Ref} · {row.Title}");
        ReloadProjects_Click(sender, e);
    }

    void ReloadProjects_Click(object sender, RoutedEventArgs e)
    {
        var rows = _catalog.List();
        ProjectListText.Text = rows.Count == 0
            ? "(no projects yet — add one above)"
            : string.Join("\n", rows.Select(r => $"{r.Ref}  {r.Status}  {r.Title}  ({r.Id[..8]}…)"));
    }

    static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
