// SPDX-License-Identifier: AGPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Human Centric Works, Hospet

using Aorms.Bridge;

namespace AormsConnect.Services;

/// <summary>
/// Factory for the AORMS hub bridge (firm.db under LocalAppData\AORMS-Connect).
/// Env: ESTI_LICENSE_API_URL, ESTI_HUB_URL, ESTI_PRODUCT_API_KEY, INSTALL_ID.
/// </summary>
public static class AormsBridgeHost
{
    public static AormsBridge CreateFromEnvironment()
    {
        var opt = new BridgeOptions
        {
            LicenseApiUrl = Environment.GetEnvironmentVariable("ESTI_LICENSE_API_URL") ?? "",
            HubUrl = Environment.GetEnvironmentVariable("ESTI_HUB_URL") ?? "http://127.0.0.1:4000",
            ProductApiKey = Environment.GetEnvironmentVariable("ESTI_PRODUCT_API_KEY") ?? "",
            DeviceId = Environment.GetEnvironmentVariable("INSTALL_ID")
                ?? $"aorms-connect-{Environment.MachineName}".ToLowerInvariant(),
            DeviceName = "AORMS Connect",
        };
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AORMS-Connect",
            "firm.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        return new AormsBridge(opt, dbPath);
    }
}
