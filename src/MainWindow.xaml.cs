// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Human Centric Works, Hospet

using System.Diagnostics;
using Aorms.Bridge;
using AormsConnect.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace AormsConnect;

public sealed partial class MainWindow : Window
{
    readonly AormsBridge _bridge;
    readonly ProjectCatalogStore _catalog = new();

    const string LicenseManagerUrl = "https://admin.aorms.in";
    const string CanonUrl =
        "https://github.com/HolagundiWorks/aorms/blob/main/docs/esti/AORMS-CONNECT.md";

    enum AppGroup { Managers, Technical, Drafting }

    sealed record SuiteApp(string Title, string PackageHint, string Repo, AppGroup Group, string Blurb);

    sealed class CatalogRowVm
    {
        public string Id { get; init; } = "";
        public string IdShort { get; init; } = "";
        public string Ref { get; init; } = "";
        public string Title { get; init; } = "";
        public string Status { get; init; } = "";
    }

    static readonly SuiteApp[] SuiteApps =
    [
        new("AStudio", "AStudio", "https://github.com/HolagundiWorks/AStudio",
            AppGroup.Managers, "Architecture practice"),
        new("AConsulting", "AConsulting", "https://github.com/HolagundiWorks/AConsulting",
            AppGroup.Managers, "Engineering practice"),
        new("AQC Core", "AQCCore", "https://github.com/HolagundiWorks/AQC",
            AppGroup.Technical, "Full quantity · BBS host"),
        new("AQC Estimation", "AQC-Estimation", "https://github.com/HolagundiWorks/AQC-Estimation",
            AppGroup.Technical, "Estimation shell"),
        new("AQC BBS", "AQC-BBS", "https://github.com/HolagundiWorks/AQC-BBS",
            AppGroup.Technical, "BBS shell"),
        new("AQC PM", "AQC-PM", "https://github.com/HolagundiWorks/AQC-PM",
            AppGroup.Technical, "Project management"),
        new("AADT", "AADT", "https://github.com/HolagundiWorks/AADT",
            AppGroup.Drafting, "2D drafting"),
    ];

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = false;
        _bridge = AormsBridgeHost.CreateFromEnvironment();
        EnsureSessionFileForSuite();
        BuildSuiteAppsUi();
        RefreshSession("Ready.");
        RefreshDbConnector();
        ReloadProjects_Click(this, new RoutedEventArgs());
    }

    /// <summary>
    /// If firm.db already has a syncToken but session.json is missing, rewrite it
    /// so sibling apps can import licence without re-Activate.
    /// </summary>
    void EnsureSessionFileForSuite()
    {
        if (File.Exists(ConnectSession.DefaultPath())) return;
        if (!_bridge.HubConfigured().HasSyncToken) return;
        try
        {
            WriteSessionFromBridge();
        }
        catch
        {
            /* best-effort — Activate / Export session still available */
        }
    }

    void BuildSuiteAppsUi()
    {
        FillAppGroup(ManagersList, AppGroup.Managers);
        FillAppGroup(TechnicalList, AppGroup.Technical);
        FillAppGroup(DraftingList, AppGroup.Drafting);
    }

    void FillAppGroup(ItemsControl host, AppGroup group)
    {
        host.Items.Clear();
        foreach (var app in SuiteApps.Where(a => a.Group == group))
            host.Items.Add(BuildAppTile(app));
    }

    FrameworkElement BuildAppTile(SuiteApp app)
    {
        var openBtn = new Button
        {
            Content = "Open",
            Tag = app.PackageHint,
            Style = (Style)Application.Current.Resources["HcwGhostButton"],
            MinWidth = 72,
        };
        openBtn.Click += OpenSuiteApp_Click;

        var getBtn = new Button
        {
            Content = "Get",
            Tag = app.Repo,
            Style = (Style)Application.Current.Resources["HcwGhostButton"],
            MinWidth = 56,
        };
        getBtn.Click += GetSuiteApp_Click;

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        actions.Children.Add(openBtn);
        actions.Children.Add(getBtn);

        var body = new StackPanel { Spacing = 6 };
        body.Children.Add(new TextBlock
        {
            Text = app.Title,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 14,
            Foreground = (Brush)Application.Current.Resources["HcwInkBrush"],
            FontFamily = (FontFamily)Application.Current.Resources["HcwFontFamily"],
        });
        body.Children.Add(new TextBlock
        {
            Text = app.Blurb,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["HcwMutedBrush"],
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 180,
        });
        body.Children.Add(actions);

        var face = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = (Brush)Application.Current.Resources["HcwSoftBrush"],
            Padding = new Thickness(14),
            MinWidth = 200,
            Child = body,
        };

        var tile = new Grid { Margin = new Thickness(0, 0, 0, 0) };
        tile.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = (Brush)Application.Current.Resources["HcwNeuDarkBrush"],
            Margin = new Thickness(5, 5, 0, 0),
        });
        tile.Children.Add(new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = (Brush)Application.Current.Resources["HcwNeuLightBrush"],
            Margin = new Thickness(0, 0, 5, 5),
        });
        tile.Children.Add(face);
        return tile;
    }

    void RefreshSession(string? note = null)
    {
        var cfg = _bridge.HubConfigured();
        SessionStatusText.Text =
            $"hub={cfg.HubUrl}  hasSyncToken={cfg.HasSyncToken}  syncReady={cfg.SyncReady}";
        SyncChipText.Text = cfg.SyncReady
            ? $"Sync ready · {ShortHub(cfg.HubUrl)}"
            : cfg.HasSyncToken
                ? "Sync · token present · hub missing"
                : "Sync · not activated";
        if (!string.IsNullOrWhiteSpace(note))
        {
            LogText.Text = note;
            TrayHintText.Text = note.Length > 72 ? note[..72] + "…" : note;
        }
        RefreshLicencePanel();
        RefreshDbConnector();
    }

    static string ShortHub(string hub)
    {
        if (string.IsNullOrWhiteSpace(hub)) return "—";
        try
        {
            var u = new Uri(hub);
            return u.IsDefaultPort ? u.Host : $"{u.Host}:{u.Port}";
        }
        catch
        {
            return hub.Length > 28 ? hub[..28] + "…" : hub;
        }
    }

    void RefreshDbConnector(string? note = null)
    {
        var cfg = _bridge.HubConfigured();
        var snap = _bridge.LicenceSnapshot();
        var box = _bridge.OutboxCounts();
        DbConnectorStatusText.Text =
            $"syncReady={cfg.SyncReady}  hub={cfg.HubUrl}\n" +
            $"firm.db={snap.FirmDbPath}\n" +
            $"outbox pending meta={box.PendingMeta}  artifacts={box.PendingArtifacts}  total={box.TotalPending}";
        if (!string.IsNullOrWhiteSpace(note))
            DbConnectorLogText.Text = note;
    }

    void RefreshOutbox_Click(object sender, RoutedEventArgs e) =>
        RefreshDbConnector("Outbox counts refreshed.");

    void EnqueueTestMeta_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var id = Guid.NewGuid().ToString("N")[..12];
            var rowId = _bridge.EnqueueMeta(
                "connect.ping",
                id,
                new { source = "AORMS-Connect", at = DateTime.UtcNow.ToString("O"), note = "DB connector smoke" });
            RefreshDbConnector($"Enqueued connect.ping/{id} as meta outbox #{rowId}. Flush to push to hub.");
        }
        catch (Exception ex)
        {
            RefreshDbConnector($"Enqueue failed: {ex.Message}");
        }
    }

    async void Flush_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DbConnectorLogText.Text = "Flushing…";
            TrayHintText.Text = "Flushing…";
            var result = await _bridge.FlushAsync();
            if (!string.IsNullOrWhiteSpace(result.SkippedReason))
            {
                RefreshDbConnector($"Flush skipped: {result.SkippedReason} (Activate first if missing_sync_token).");
                RefreshSession($"Flush skipped={result.SkippedReason}");
                return;
            }
            var msg =
                $"Flush OK · metaSent={result.MetaSent}  artifactsSent={result.ArtifactsSent}. Browse on hub /ops-db.";
            RefreshDbConnector(msg);
            RefreshSession(msg);
        }
        catch (Exception ex)
        {
            RefreshDbConnector($"Flush failed: {ex.Message}");
            RefreshSession($"Flush failed: {ex.Message}");
        }
    }

    void RefreshLicencePanel()
    {
        var snap = _bridge.LicenceSnapshot();
        LicenceStatusText.Text =
            $"status={snap.LicenceStatus}  licenseToken={snap.HasLicenseToken}  syncToken={snap.HasSyncToken}\n" +
            $"installId={snap.InstallId}\n" +
            $"hub={snap.HubUrl}  licenseApi={snap.LicenseApiUrl}\n" +
            $"sessionFile={(snap.SessionFilePresent ? "yes" : "no")}  updated={snap.UpdatedAt ?? "—"}\n" +
            $"firm.db={snap.FirmDbPath}";
    }

    void Refresh_Click(object sender, RoutedEventArgs e) => RefreshSession("Status refreshed.");

    void SyncChip_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) =>
        RefreshSession("Status refreshed.");

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
            TrayHintText.Text = "Activating…";
            var grant = await _bridge.ActivateAsync(key);
            WriteSessionFromBridge(grant.SyncToken, grant.LicenseToken);
            RefreshSession(
                $"Activate OK · session.json written · syncToken length={grant.SyncToken.Length}");
        }
        catch (Exception ex)
        {
            RefreshSession($"Activate failed: {ex.Message}");
        }
    }

    void WriteSessionFromBridge(string? syncTokenOverride = null, string? licenseTokenOverride = null)
    {
        var snap = _bridge.LicenceSnapshot();
        var (sync, hub, installId) = _bridge.Db.ReadAuth();
        var (_, licenseToken, _, _, licenseApi, _, _) = _bridge.Db.ReadLicenceRow();
        var token = string.IsNullOrWhiteSpace(syncTokenOverride) ? (sync ?? "") : syncTokenOverride!;
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("No syncToken to export.");
        ConnectSession.Write(new ConnectSessionFile
        {
            SyncToken = token,
            HubUrl = string.IsNullOrWhiteSpace(hub) ? snap.HubUrl : hub!,
            LicenseApiUrl = string.IsNullOrWhiteSpace(licenseApi) ? snap.LicenseApiUrl : licenseApi,
            LicenseToken = string.IsNullOrWhiteSpace(licenseTokenOverride) ? licenseToken : licenseTokenOverride,
            DeviceId = installId,
            WrittenAt = DateTimeOffset.UtcNow.ToString("O"),
        });
    }

    void ExportSession_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            WriteSessionFromBridge();
            RefreshSession("session.json rewritten from firm.db for suite apps.");
        }
        catch (Exception ex)
        {
            RefreshSession($"Export session failed: {ex.Message}");
        }
    }

    void ClearLocalLicence_Click(object sender, RoutedEventArgs e)
    {
        _bridge.ClearLocalLicence();
        RefreshSession("Local licence cleared (firm.db tokens + session.json). Hub revoke is via License Manager.");
    }

    void CopyInstallId_Click(object sender, RoutedEventArgs e)
    {
        var id = _bridge.LicenceSnapshot().InstallId;
        var data = new DataPackage();
        data.SetText(id);
        Clipboard.SetContent(data);
        RefreshSession($"Copied installId ({id.Length} chars).");
    }

    void OpenLicenseManager_Click(object sender, RoutedEventArgs e) =>
        OpenUrl(LicenseManagerUrl);

    void OpenDownloads_Click(object sender, RoutedEventArgs e) =>
        OpenUrl("https://aorms.in/downloads");

    void OpenCanon_Click(object sender, RoutedEventArgs e) => OpenUrl(CanonUrl);

    void Exit_Click(object sender, RoutedEventArgs e) => Close();

    async void About_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new ContentDialog
        {
            Title = "About AORMS Connect",
            Content = "Suite core Manager Hub — sign in · launch · catalog · DB connector.\n" +
                      "Package in.aorms.connect · Human Centric Works, Hospet.\n" +
                      "Not a practice manager (AStudio / AConsulting stay separate).",
            CloseButtonText = "Close",
            XamlRoot = Content.XamlRoot,
            DefaultButton = ContentDialogButton.Close,
        };
        await dlg.ShowAsync();
    }

    void OpenSuiteApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string hint }) return;
        var sessionPath = ConnectSession.DefaultPath();
        foreach (var path in ResolveExeCandidates(hint))
        {
            if (!File.Exists(path)) continue;
            var args = BuildLaunchArgs(path, sessionPath);
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                Arguments = args,
            });
            RefreshSession(
                File.Exists(sessionPath)
                    ? $"Launched {hint} with Connect session"
                    : $"Launched {hint} (no session.json — Activate first)");
            return;
        }
        RefreshSession($"Not installed locally ({hint}). Use Get → downloads / GitHub.");
    }

    /// <summary>
    /// AAD.exe (egui) needs subcommand <c>app</c>; WinUI / managers take --connect-session only.
    /// </summary>
    static string BuildLaunchArgs(string exePath, string sessionPath)
    {
        var name = Path.GetFileName(exePath) ?? "";
        var sessionArg = File.Exists(sessionPath)
            ? $"{ConnectSession.FlagConnectSession} \"{sessionPath}\""
            : "";
        if (name.Equals("AAD.exe", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("aadt.exe", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrEmpty(sessionArg) ? "app" : $"app {sessionArg}";
        }
        return sessionArg;
    }

    /// <summary>Installed Programs paths + sibling repo unpackaged Release builds.</summary>
    static IEnumerable<string> ResolveExeCandidates(string hint)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        // AADT drafting — real installers / WinUI shell names differ from package hint.
        if (hint is "AADT")
        {
            yield return Path.Combine(local, "Programs", "AAD", "AAD.exe");
            yield return Path.Combine(pf, "AAD", "AAD.exe");
            var reposAadt = FindReposRoot();
            if (reposAadt is not null)
            {
                // VS: …\AadWinui\x64\{Config}\AadWinui\AadWinui.exe
                yield return Path.Combine(reposAadt, "AADT", "native", "aad-winui", "winui", "AadWinui",
                    "x64", "Release", "AadWinui", "AadWinui.exe");
                yield return Path.Combine(reposAadt, "AADT", "native", "aad-winui", "winui", "AadWinui",
                    "x64", "Debug", "AadWinui", "AadWinui.exe");
                yield return Path.Combine(reposAadt, "AADT", "native", "aad-winui", "build", "Release",
                    "aad-winui.exe");
                yield return Path.Combine(reposAadt, "AADT", "target", "release", "aadt.exe");
                yield return Path.Combine(reposAadt, "AADT", "target", "release", "AAD.exe");
            }
        }

        yield return Path.Combine(local, "Programs", hint, $"{hint}.exe");
        yield return Path.Combine(pf, hint, $"{hint}.exe");

        // Sibling unpackaged builds (dev: …/Repos/{app}/…)
        var repos = FindReposRoot();
        if (repos is not null)
        {
            if (hint is "AQCCore" or "AQC")
            {
                yield return Path.Combine(repos, "AQC", "BBSDesktop", "BBSApp", "bin", "x64", "Release",
                    "net8.0-windows10.0.19041.0", "AQCCore.exe");
            }

            yield return Path.Combine(repos, hint, "src", $"{hint}.App", "bin", "x64", "Release",
                "net8.0-windows10.0.19041.0", $"{hint}.exe");
            yield return Path.Combine(repos, hint, "bin", "x64", "Release",
                "net8.0-windows10.0.19041.0", $"{hint}.exe");

            if (hint is "AStudio")
                yield return Path.Combine(repos, "AStudio", "src", "AStudio.App", "bin", "x64", "Release",
                    "net8.0-windows10.0.19041.0", "AStudio.exe");
            if (hint is "AConsulting")
                yield return Path.Combine(repos, "AConsulting", "src", "AConsulting.App", "bin", "x64", "Release",
                    "net8.0-windows10.0.19041.0", "AConsulting.exe");
            if (hint is "AQC-Estimation")
                yield return Path.Combine(repos, "AQC-Estimation", "src", "bin", "x64", "Release",
                    "net8.0-windows10.0.19041.0", "AQC-Estimation.exe");
            if (hint is "AQC-BBS")
                yield return Path.Combine(repos, "AQC-BBS", "src", "bin", "x64", "Release",
                    "net8.0-windows10.0.19041.0", "AQC-BBS.exe");
            if (hint is "AQC-PM")
                yield return Path.Combine(repos, "AQC-PM", "src", "bin", "x64", "Release",
                    "net8.0-windows10.0.19041.0", "AQC-PM.exe");
        }

        yield return Path.Combine(local, "Programs", "AQC Core", "AQCCore.exe");
    }

    static string? FindReposRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "AQC")) &&
                Directory.Exists(Path.Combine(dir.FullName, "AORMS-Connect")))
                return dir.FullName;
            if (dir.Name.Equals("AORMS-Connect", StringComparison.OrdinalIgnoreCase) &&
                dir.Parent is not null &&
                Directory.Exists(Path.Combine(dir.Parent.FullName, "AQC")))
                return dir.Parent.FullName;
        }
        return null;
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
        var rows = _catalog.List()
            .Select(r => new CatalogRowVm
            {
                Id = r.Id,
                IdShort = r.Id.Length >= 8 ? r.Id[..8] + "…" : r.Id,
                Ref = r.Ref,
                Title = r.Title,
                Status = r.Status,
            })
            .ToList();
        ProjectListView.ItemsSource = rows;
        ProjectEmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    static void OpenUrl(string url) =>
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
