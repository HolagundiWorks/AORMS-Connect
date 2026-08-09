// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Human Centric Works, Hospet

using System.Diagnostics;
using Aorms.Bridge;
using AormsConnect.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace AormsConnect;

public sealed partial class MainWindow : Window
{
    readonly AormsBridge _bridge;
    readonly ProjectCatalogStore _catalog = new();

    const string LicenseManagerUrl = "https://admin.aorms.in";

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
        RefreshDbConnector();
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
        RefreshLicencePanel();
        RefreshDbConnector();
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
            var result = await _bridge.FlushAsync();
            if (!string.IsNullOrWhiteSpace(result.SkippedReason))
            {
                RefreshDbConnector($"Flush skipped: {result.SkippedReason} (Activate first if missing_sync_token).");
                return;
            }
            RefreshDbConnector(
                $"Flush OK · metaSent={result.MetaSent}  artifactsSent={result.ArtifactsSent}. Browse on hub /ops-db.");
        }
        catch (Exception ex)
        {
            RefreshDbConnector($"Flush failed: {ex.Message}");
        }
    }

    void RefreshLicencePanel()
    {
        var snap = _bridge.LicenceSnapshot();
        LicenceStatusText.Text =
            $"status={snap.LicenceStatus}  licenseToken={snap.HasLicenseToken}  syncToken={snap.HasSyncToken}\n" +
            $"installId={snap.InstallId}\n" +
            $"hub={snap.HubUrl}\n" +
            $"licenseApi={snap.LicenseApiUrl}\n" +
            $"sessionFile={(snap.SessionFilePresent ? "yes" : "no")}  updated={snap.UpdatedAt ?? "—"}\n" +
            $"firm.db={snap.FirmDbPath}\n" +
            $"session={snap.SessionPath}";
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
