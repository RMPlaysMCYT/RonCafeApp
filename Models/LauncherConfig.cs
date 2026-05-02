using System.Collections.Generic;

namespace RonCafeApp.Models;

public class LauncherConfig
{
    public bool UseCoverArtReview { get; set; } = false;
    public List<AppItem> Apps { get; set; } = new();
    public string BackgroundColor { get; set; } = "#1E1E2E";
    public string SidebarColor { get; set; } = "#181825";
    public string AccentColor { get; set; } = "#89B4FA";
    public bool UseCoverArtView { get; set; } = false;
}