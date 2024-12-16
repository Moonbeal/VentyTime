using MudBlazor;

namespace VentyTime.Client.Theme
{
    public static class CustomTheme
    {
        public static MudTheme DefaultTheme => new MudTheme()
        {
            Palette = new PaletteLight()
            {
                Primary = "#8B5CF6",
                PrimaryDarken = "#7C3AED",
                PrimaryLighten = "#A78BFA",
                Secondary = "#EC4899",
                SecondaryDarken = "#DB2777",
                SecondaryLighten = "#F472B6",
                AppbarBackground = "#8B5CF6",
                Background = "#F8F9FA",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "rgba(0,0,0,0.7)",
                Success = "#10B981",
                SuccessDarken = "#059669",
                SuccessLighten = "#34D399",
                Warning = "#F59E0B",
                WarningDarken = "#D97706",
                WarningLighten = "#FBBF24",
                Error = "#EF4444",
                ErrorDarken = "#DC2626",
                ErrorLighten = "#F87171",
                TextPrimary = "rgba(0,0,0,0.87)",
                TextSecondary = "rgba(0,0,0,0.6)",
                TextDisabled = "rgba(0,0,0,0.38)",
                ActionDefault = "#424242",
                ActionDisabled = "rgba(0,0,0,0.26)",
                ActionDisabledBackground = "rgba(0,0,0,0.12)",
                DrawerIcon = "rgba(0,0,0,0.54)"
            },
            Typography = new Typography()
            {
                Default = new Default()
                {
                    FontFamily = new[] { "Inter", "Helvetica", "Arial", "sans-serif" },
                    FontSize = "0.875rem",
                    FontWeight = 400,
                    LineHeight = 1.43,
                    LetterSpacing = ".01071em"
                },
                H1 = new H1()
                {
                    FontSize = "3.75rem",
                    FontWeight = 300,
                    LineHeight = 1.167,
                    LetterSpacing = "-.01562em"
                },
                H2 = new H2()
                {
                    FontSize = "3rem",
                    FontWeight = 300,
                    LineHeight = 1.2,
                    LetterSpacing = "-.00833em"
                },
                H3 = new H3()
                {
                    FontSize = "2.125rem",
                    FontWeight = 400,
                    LineHeight = 1.167,
                    LetterSpacing = "0"
                },
                H4 = new H4()
                {
                    FontSize = "1.5rem",
                    FontWeight = 400,
                    LineHeight = 1.235,
                    LetterSpacing = ".00735em"
                },
                H5 = new H5()
                {
                    FontSize = "1.25rem",
                    FontWeight = 400,
                    LineHeight = 1.334,
                    LetterSpacing = "0"
                },
                H6 = new H6()
                {
                    FontSize = "1rem",
                    FontWeight = 500,
                    LineHeight = 1.6,
                    LetterSpacing = ".0075em"
                },
                Subtitle1 = new Subtitle1()
                {
                    FontSize = "1rem",
                    FontWeight = 400,
                    LineHeight = 1.75,
                    LetterSpacing = ".00938em"
                },
                Subtitle2 = new Subtitle2()
                {
                    FontSize = "0.875rem",
                    FontWeight = 500,
                    LineHeight = 1.57,
                    LetterSpacing = ".00714em"
                },
                Body1 = new Body1()
                {
                    FontSize = "1rem",
                    FontWeight = 400,
                    LineHeight = 1.5,
                    LetterSpacing = ".00938em"
                },
                Body2 = new Body2()
                {
                    FontSize = "0.875rem",
                    FontWeight = 400,
                    LineHeight = 1.43,
                    LetterSpacing = ".01071em"
                },
                Button = new Button()
                {
                    FontSize = "0.875rem",
                    FontWeight = 500,
                    LineHeight = 1.75,
                    LetterSpacing = ".02857em",
                    TextTransform = "uppercase"
                },
                Caption = new Caption()
                {
                    FontSize = "0.75rem",
                    FontWeight = 400,
                    LineHeight = 1.66,
                    LetterSpacing = ".03333em"
                },
                Overline = new Overline()
                {
                    FontSize = "0.75rem",
                    FontWeight = 400,
                    LineHeight = 2.66,
                    LetterSpacing = ".08333em",
                    TextTransform = "uppercase"
                }
            },
            Shadows = new Shadow(),
            ZIndex = new ZIndex(),
            LayoutProperties = new LayoutProperties()
            {
                DefaultBorderRadius = "8px"
            }
        };
    }
}
