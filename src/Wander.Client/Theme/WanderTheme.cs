using MudBlazor;

namespace Wander.Client.Theme;

public static class WanderTheme
{
    public static MudTheme Create() => new()
    {
        PaletteDark = new PaletteDark
        {
            Primary = "#c9a84c",
            PrimaryContrastText = "#0f0a1a",
            Secondary = "#7c6bc4",
            SecondaryContrastText = "#ffffff",
            Background = "#0f0a1a",
            BackgroundGray = "#1c1228",
            Surface = "#241a35",
            DrawerBackground = "#160d24",
            AppbarBackground = "#0f0a1a",
            AppbarText = "#e8e0ff",
            TextPrimary = "#e8e0ff",
            TextSecondary = "#9e94c8",
            Error = "#cf4a4a",
            ErrorContrastText = "#ffffff",
            Success = "#4a8c5c",
            OverlayDark = "rgba(15, 10, 26, 0.85)",
            Divider = "#3d2d5e",
            DividerLight = "#2a1d40",
            ActionDefault = "#9e94c8",
            ActionDisabled = "#4a3d6e",
            ActionDisabledBackground = "#1c1228",
            TableLines = "#3d2d5e",
            LinesInputs = "#5a4a8a",
        },
        Typography = new Typography
        {
            H1 = new H1Typography { FontFamily = ["Cinzel", "serif"] },
            H2 = new H2Typography { FontFamily = ["Cinzel", "serif"] },
            H3 = new H3Typography { FontFamily = ["Cinzel", "serif"] },
            H4 = new H4Typography { FontFamily = ["Cinzel", "serif"] },
            H5 = new H5Typography { FontFamily = ["Cinzel", "serif"] },
            H6 = new H6Typography { FontFamily = ["Cinzel", "serif"] },
            Default = new DefaultTypography { FontFamily = ["Inter", "sans-serif"] },
            Body1 = new Body1Typography { FontFamily = ["Inter", "sans-serif"] },
            Body2 = new Body2Typography { FontFamily = ["Inter", "sans-serif"] },
        },
        LayoutProperties = new LayoutProperties
        {
            DrawerWidthLeft = "240px",
            AppbarHeight = "56px",
        },
    };
}