using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using CompositionMaterial.Avalonia.Materials;

namespace CompositionMaterial.Avalonia.Demo;

public sealed class MainWindow : Window
{
    private readonly CompositionMaterialControl _animatedCard;
    private readonly TextBlock _status;
    private readonly AcrylicMaterial _acrylic;
    private readonly MicaMaterial _mica;
    private readonly LiquidGlassMaterial _liquidGlass;
    private readonly AcrylicMaterial _revealBase;
    private readonly RevealMaterial _reveal;
    private DispatcherTimer? _opacityTimer;
    private DispatcherTimer? _smokeTimer;
    private TabControl? _parameterTabs;
    private readonly List<CompositionMaterialControl> _cards = [];

    public MainWindow()
    {
        Title = "CompositionMaterial.Avalonia";
        Width = 1320;
        Height = 760;
        Background = Brushes.Transparent;
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        ExtendClientAreaToDecorationsHint = true;
        ExtendClientAreaTitleBarHeightHint = 48;

        _status = new TextBlock { Foreground = Brushes.White, FontSize = 13 };
        _acrylic = new AcrylicMaterial
        {
            TintColor = Color.FromRgb(28, 32, 40),
            TintOpacity = 0.68,
        };
        _mica = new MicaMaterial();
        _liquidGlass = new LiquidGlassMaterial();
        _revealBase = new AcrylicMaterial { TintOpacity = 0.62 };
        _reveal = new RevealMaterial { BaseMaterial = _revealBase };
        _animatedCard = CreateCard("Acrylic · direct Composition animation", _acrylic);

        var custom = new CustomCompositionMaterial
        {
            RootBrush = new TintBrushNode
            {
                Color = Color.FromRgb(40, 90, 160),
                Opacity = 0.45,
                Source = new GaussianBlurBrushNode
                {
                    Amount = 28,
                    Source = new BackdropBrushNode(),
                },
            },
        };

        var gallery = new StackPanel
        {
            Margin = new Thickness(40, 72, 20, 40),
            Spacing = 20,
            Children =
            {
                new TextBlock
                {
                    Text = "WinUI Composition materials",
                    FontSize = 30,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brushes.White,
                },
                _status,
                new WrapPanel
                {
                    ItemWidth = 270,
                    ItemHeight = 150,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Children =
                    {
                        _animatedCard,
                        CreateCard("Mica", _mica),
                        CreateCard("Liquid glass approximation", _liquidGlass, new CornerRadius(32)),
                        CreateCard("Fluent 1 Reveal", _reveal),
                        CreateCard("Custom brush graph", custom),
                    },
                },
            },
        };

        var root = new Grid { Background = Brushes.Transparent };
        root.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        root.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(370)));
        var titleBar = CreateTitleBar();
        Grid.SetColumnSpan(titleBar, 2);
        root.Children.Add(titleBar);
        root.Children.Add(gallery);
        var parameterPanel = CreateParameterPanel();
        Grid.SetColumn(parameterPanel, 1);
        root.Children.Add(parameterPanel);
        Content = root;

        Opened += (_, _) =>
        {
            StartCompositionAnimations();
            if (Program.IsSmokeTest)
                StartSmokeTest();
        };
        Closed += (_, _) =>
        {
            _opacityTimer?.Stop();
            _smokeTimer?.Stop();
        };
    }

    private Control CreateTitleBar()
    {
        // Leave the right side to the native caption buttons.
        var titleBar = new Border
        {
            Height = 48,
            Margin = new Thickness(0, 0, 150, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
        };
        titleBar.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(titleBar).Properties.IsLeftButtonPressed)
                return;
            BeginMoveDrag(args);
            args.Handled = true;
        };
        return titleBar;
    }

    private Control CreateParameterPanel()
    {
        _parameterTabs = new TabControl
        {
            ItemsSource = new object[]
            {
                CreateParameterTab("Acrylic", CreateAcrylicParameters(_acrylic)),
                CreateParameterTab("Mica", CreateMicaParameters()),
                CreateParameterTab("Liquid", CreateLiquidGlassParameters()),
                CreateParameterTab("Reveal", CreateRevealParameters()),
            },
        };

        return new Border
        {
            Margin = new Thickness(10, 64, 20, 22),
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.FromArgb(225, 20, 23, 29)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Child = _parameterTabs,
        };
    }

    private static TabItem CreateParameterTab(string header, Control content)
    {
        return new TabItem
        {
            Header = header,
            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                Content = content,
            },
        };
    }

    private StackPanel CreateAcrylicParameters(AcrylicMaterial material)
    {
        return ParameterStack(
            SectionTitle("Acrylic"),
            CreateColorRow("TintColor", () => material.TintColor, value => material.TintColor = value),
            CreateSliderRow("TintOpacity", 0, 1, material.TintOpacity, value => material.TintOpacity = value),
            CreateSliderRow("TintLuminosityOpacity", 0, 1, material.TintLuminosityOpacity,
                value => material.TintLuminosityOpacity = value),
            CreateSliderRow("BlurAmount · system recipe", 0, 80, material.BlurAmount,
                value => material.BlurAmount = value, "0.0"),
            CreateSliderRow("Saturation · system recipe", 0, 3, material.Saturation,
                value => material.Saturation = value),
            CreateSliderRow("NoiseOpacity", 0, 0.15, material.NoiseOpacity,
                value => material.NoiseOpacity = value, "0.000"));
    }

    private StackPanel CreateMicaParameters()
    {
        var themes = new ComboBox
        {
            ItemsSource = Enum.GetValues<MicaThemeMode>(),
            SelectedItem = _mica.ThemeMode,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        themes.SelectionChanged += (_, _) =>
        {
            if (themes.SelectedItem is MicaThemeMode value)
                _mica.ThemeMode = value;
        };

        return ParameterStack(
            SectionTitle("Mica"),
            LabeledControl("ThemeMode", themes),
            CreateColorRow("TintColor", () => _mica.TintColor, value => _mica.TintColor = value),
            CreateSliderRow("TintOpacity", 0, 1, _mica.TintOpacity, value => _mica.TintOpacity = value));
    }

    private StackPanel CreateLiquidGlassParameters()
    {
        return ParameterStack(
            SectionTitle("LiquidGlass"),
            CreateColorRow("TintColor", () => _liquidGlass.TintColor, value => _liquidGlass.TintColor = value),
            CreateSliderRow("SurfaceOpacity", 0, 1, _liquidGlass.SurfaceOpacity,
                value => _liquidGlass.SurfaceOpacity = value),
            CreateSliderRow("FrostedOpacity", 0, 0.6, _liquidGlass.FrostedOpacity,
                value => _liquidGlass.FrostedOpacity = value),
            CreateSliderRow("BlurAmount · system recipe", 0, 80, _liquidGlass.BlurAmount,
                value => _liquidGlass.BlurAmount = value, "0.0"),
            CreateSliderRow("Saturation · system recipe", 0, 3, _liquidGlass.Saturation,
                value => _liquidGlass.Saturation = value),
            CreateColorRow("HighlightColor", () => _liquidGlass.HighlightColor,
                value => _liquidGlass.HighlightColor = value),
            CreateSliderRow("HighlightIntensity", 0, 1, _liquidGlass.HighlightIntensity,
                value => _liquidGlass.HighlightIntensity = value),
            CreateSliderRow("BorderThickness", 0, 5, _liquidGlass.BorderThickness,
                value => _liquidGlass.BorderThickness = value),
            CreateSliderRow("EdgeIntensity", 0, 1, _liquidGlass.EdgeIntensity,
                value => _liquidGlass.EdgeIntensity = value),
            CreateSliderRow("EdgeDepth", 0, 12, _liquidGlass.EdgeDepth,
                value => _liquidGlass.EdgeDepth = value),
            CreateSliderRow("ChromaticAberration", 0, 1, _liquidGlass.ChromaticAberration,
                value => _liquidGlass.ChromaticAberration = value),
            CreateSliderRow("PointerGlowRadius", 20, 300, _liquidGlass.PointerGlowRadius,
                value => _liquidGlass.PointerGlowRadius = value, "0"));
    }

    private StackPanel CreateRevealParameters()
    {
        var stack = ParameterStack(
            SectionTitle("Reveal"),
            CreateColorRow("HighlightColor", () => _reveal.HighlightColor, value => _reveal.HighlightColor = value),
            CreateSliderRow("Radius", 20, 300, _reveal.Radius, value => _reveal.Radius = value, "0"),
            CreateSliderRow("FillIntensity", 0, 1, _reveal.FillIntensity, value => _reveal.FillIntensity = value),
            CreateSliderRow("BorderIntensity", 0, 1, _reveal.BorderIntensity,
                value => _reveal.BorderIntensity = value),
            CreateSliderRow("BorderThickness", 0, 5, _reveal.BorderThickness,
                value => _reveal.BorderThickness = value),
            SectionTitle("Base Acrylic"));

        var baseParameters = CreateAcrylicParameters(_revealBase);
        while (baseParameters.Children.Count > 1)
        {
            var control = baseParameters.Children[1];
            baseParameters.Children.RemoveAt(1);
            stack.Children.Add(control);
        }
        return stack;
    }

    private static StackPanel ParameterStack(params Control[] children)
    {
        var result = new StackPanel { Spacing = 10, Margin = new Thickness(4, 12, 8, 16) };
        result.Children.AddRange(children);
        return result;
    }

    private static TextBlock SectionTitle(string text) => new()
    {
        Text = text,
        FontSize = 17,
        FontWeight = FontWeight.SemiBold,
        Foreground = Brushes.White,
        Margin = new Thickness(0, 4, 0, 2),
    };

    private static Control CreateSliderRow(string label, double minimum, double maximum, double initial,
        Action<double> setter, string format = "0.00")
    {
        var valueText = new TextBlock
        {
            Text = initial.ToString(format),
            Foreground = new SolidColorBrush(Color.FromRgb(160, 205, 255)),
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.FromRgb(220, 224, 232)) });
        Grid.SetColumn(valueText, 1);
        header.Children.Add(valueText);

        var slider = new Slider
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = initial,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        slider.ValueChanged += (_, _) =>
        {
            setter(slider.Value);
            valueText.Text = slider.Value.ToString(format);
        };
        return new StackPanel { Spacing = 3, Children = { header, slider } };
    }

    private static Control CreateColorRow(string label, Func<Color> getter, Action<Color> setter)
    {
        var swatch = new Border
        {
            Width = 24,
            Height = 24,
            CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(getter()),
        };
        var editor = new TextBox
        {
            Text = getter().ToString(),
            PlaceholderText = "#AARRGGBB",
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        void Commit()
        {
            if (Color.TryParse(editor.Text, out var value))
            {
                setter(value);
                swatch.Background = new SolidColorBrush(value);
                editor.Text = value.ToString();
            }
            else
            {
                editor.Text = getter().ToString();
            }
        }
        editor.LostFocus += (_, _) => Commit();
        editor.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter)
                return;
            Commit();
            args.Handled = true;
        };

        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(132)));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(Color.FromRgb(220, 224, 232)),
            VerticalAlignment = VerticalAlignment.Center,
        });
        Grid.SetColumn(editor, 1);
        row.Children.Add(editor);
        Grid.SetColumn(swatch, 2);
        row.Children.Add(swatch);
        return row;
    }

    private static Control LabeledControl(string label, Control control)
    {
        return new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.FromRgb(220, 224, 232)) },
                control,
            },
        };
    }

    private CompositionMaterialControl CreateCard(string title, Materials.CompositionMaterial material,
        CornerRadius? cornerRadius = null)
    {
        var control = new CompositionMaterialControl
        {
            Width = 250,
            Height = 130,
            Margin = new Thickness(8),
            Padding = new Thickness(18),
            CornerRadius = cornerRadius ?? new CornerRadius(24, 10, 24, 10),
            Material = material,
            FallbackBrush = new SolidColorBrush(Color.FromRgb(40, 44, 52)),
            Child = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };
        control.PropertyChanged += (_, args) =>
        {
            if (args.Property == CompositionMaterialControl.ActualRenderingModeProperty)
                _status.Text = $"Backend: {control.ActualRenderingMode}";
        };
        _cards.Add(control);
        control.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(control).Properties.IsLeftButtonPressed || _parameterTabs is null)
                return;
            if (ReferenceEquals(material, _acrylic)) _parameterTabs.SelectedIndex = 0;
            else if (ReferenceEquals(material, _mica)) _parameterTabs.SelectedIndex = 1;
            else if (ReferenceEquals(material, _liquidGlass)) _parameterTabs.SelectedIndex = 2;
            else if (ReferenceEquals(material, _reveal)) _parameterTabs.SelectedIndex = 3;
        };
        return control;
    }

    private void StartCompositionAnimations()
    {
        var visual = ElementComposition.GetElementVisual(_animatedCard);
        if (visual is null)
            return;

        visual.CenterPoint = new Vector3D(_animatedCard.Bounds.Width / 2, _animatedCard.Bounds.Height / 2, 0);
        var scale = visual.Compositor.CreateVector3DKeyFrameAnimation();
        scale.Duration = TimeSpan.FromSeconds(1.8);
        scale.IterationBehavior = AnimationIterationBehavior.Forever;
        scale.Direction = PlaybackDirection.Alternate;
        scale.InsertKeyFrame(0, new Vector3D(0.94, 0.94, 1), new SineEaseInOut());
        scale.InsertKeyFrame(1, new Vector3D(1.04, 1.04, 1), new SineEaseInOut());
        visual.StartAnimation("Scale", scale);

        var implicitAnimations = visual.Compositor.CreateImplicitAnimationCollection();
        var opacity = visual.Compositor.CreateScalarKeyFrameAnimation();
        opacity.Target = "Opacity";
        opacity.Duration = TimeSpan.FromMilliseconds(450);
        opacity.InsertExpressionKeyFrame(1, "this.FinalValue", new CubicEaseOut());
        implicitAnimations["Opacity"] = opacity;
        visual.ImplicitAnimations = implicitAnimations;

        var dimmed = false;
        _opacityTimer = new DispatcherTimer(TimeSpan.FromSeconds(2.2), DispatcherPriority.Normal, (_, _) =>
        {
            dimmed = !dimmed;
            visual.Opacity = dimmed ? 0.58f : 1f;
        });
        _opacityTimer.Start();
    }

    private void StartSmokeTest()
    {
        // Exercise both overlay-only updates and the native parameter/rebuild paths used by the panel.
        _acrylic.TintOpacity = 0.55;
        _mica.ThemeMode = MicaThemeMode.Dark;
        _mica.TintOpacity = 0.08;
        _liquidGlass.FrostedOpacity = 0.14;
        _liquidGlass.EdgeDepth = 6;
        _reveal.Radius = 132;
        _reveal.FillIntensity = 0.18;

        _smokeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _smokeTimer.Tick += (_, _) =>
        {
            _smokeTimer.Stop();
            var success = _cards.Count == 5 && _cards.All(card => card.ActualRenderingMode == MaterialRenderingMode.WinUIComposition);
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown(success ? 0 : 3);
        };
        _smokeTimer.Start();
    }
}
