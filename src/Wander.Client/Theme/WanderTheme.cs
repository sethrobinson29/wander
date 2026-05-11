using MudBlazor;

namespace Wander.Client.Theme;

public static class WanderTheme
{
    public static MudTheme Create() => new()
    {
        PaletteDark = new PaletteDark
        {
            // Surfaces
            Black                    = "#100c18",
            Background               = "#100c18",
            BackgroundGray           = "#14101e",
            Surface                  = "#1c1726",
            DrawerBackground         = "#14101e",
            AppbarBackground         = "#100c18",
            DrawerText               = "#ebe4d0",
            AppbarText               = "#ebe4d0",

            // Text
            TextPrimary              = "#ebe4d0",
            TextSecondary            = "#c8c0a8",
            TextDisabled             = "#645a76",
            ActionDefault            = "#c8c0a8",
            ActionDisabled           = "#645a76",
            ActionDisabledBackground = "#251f31",

            // Lines
            LinesDefault             = "#352d47",
            LinesInputs              = "#352d47",
            TableLines               = "#352d47",
            TableStriped             = "rgba(255,255,255,0.02)",
            TableHover               = "#251f31",
            Divider                  = "#352d47",
            DividerLight             = "#251f31",

            // Brass primary, violet secondary
            Primary                  = "#c4a35f",
            PrimaryContrastText      = "#1f1808",
            PrimaryDarken            = "#a88848",
            PrimaryLighten           = "#d8b96f",

            Secondary                = "#9d7fd1",
            SecondaryContrastText    = "#15101e",
            SecondaryDarken          = "#7e62b3",
            SecondaryLighten         = "#b89ee0",

            Tertiary                 = "#7ba87e",
            TertiaryContrastText     = "#0f2418",

            Info                     = "#5d8fd6",
            Success                  = "#7ba87e",
            Warning                  = "#d8b96f",
            Error                    = "#d97a5a",
            Dark                     = "#100c18",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily    = ["Inter", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif"],
                FontSize      = ".875rem",
                FontWeight    = "400",
                LineHeight    = "1.5",
                LetterSpacing = "normal",
            },
            H1 = new H1Typography { FontFamily = ["Cinzel", "serif"], FontSize = "3rem",    FontWeight = "500", LetterSpacing = "0.06em" },
            H2 = new H2Typography { FontFamily = ["Cinzel", "serif"], FontSize = "2.25rem", FontWeight = "500", LetterSpacing = "0.06em" },
            H3 = new H3Typography { FontFamily = ["Cinzel", "serif"], FontSize = "1.75rem", FontWeight = "500", LetterSpacing = "0.06em" },
            H4 = new H4Typography { FontFamily = ["Inter", "sans-serif"], FontSize = "1.5rem",  FontWeight = "600" },
            H5 = new H5Typography { FontFamily = ["Inter", "sans-serif"], FontSize = "1.25rem", FontWeight = "600" },
            H6 = new H6Typography { FontFamily = ["Inter", "sans-serif"], FontSize = "1rem",    FontWeight = "600" },
            Button = new ButtonTypography
            {
                FontFamily    = ["Inter", "sans-serif"],
                FontWeight    = "500",
                LetterSpacing = "0.02em",
                TextTransform = "none",
            },
            Caption = new CaptionTypography
            {
                FontFamily    = ["Inter", "sans-serif"],
                FontSize      = "0.6875rem",
                LetterSpacing = "0.16em",
                TextTransform = "uppercase",
            },
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft     = "260px",
            DrawerWidthRight    = "320px",
            AppbarHeight        = "64px",
        },
    };
}
