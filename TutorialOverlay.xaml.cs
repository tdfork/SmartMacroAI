using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SmartMacroAI.Core;
using SmartMacroAI.Localization;

namespace SmartMacroAI;

/// <summary>
/// Tutorial overlay that guides new users through SmartMacroAI features.
/// Shows a spotlight on target UI elements with step-by-step instructions.
/// </summary>
public partial class TutorialOverlay : UserControl
{
    private readonly TutorialService _tutorialService = new();

    /// <summary>Fires when the tutorial is completed or skipped.</summary>
    public event Action? TutorialFinished;

    public TutorialOverlay()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows the tutorial overlay if it hasn't been completed yet.
    /// Returns true if the tutorial was shown.
    /// </summary>
    public bool TryShow()
    {
        if (!_tutorialService.ShouldShowTutorial())
            return false;

        Visibility = Visibility.Visible;
        RenderStep();
        return true;
    }

    /// <summary>
    /// Forces the tutorial to show (e.g., from Settings → Restart Tutorial).
    /// </summary>
    public void ForceShow()
    {
        _tutorialService.Reset();
        Visibility = Visibility.Visible;
        RenderStep();
    }

    private void RenderStep()
    {
        var step = _tutorialService.GetCurrentStep();
        if (step is null)
        {
            Close();
            return;
        }

        TxtTitle.Text = step.GetTitle();
        TxtDescription.Text = step.GetDescription();

        // Update buttons
        BtnBack.Visibility = _tutorialService.CurrentStep > 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        bool isLast = _tutorialService.CurrentStep >= _tutorialService.Steps.Count - 1;
        BtnNext.Content = isLast
            ? (LanguageManager.GetString("ui_Tutorial_Finish") is string f && f != "ui_Tutorial_Finish" ? f : "Hoàn tất ✓")
            : (LanguageManager.GetString("ui_Tutorial_Next") is string n && n != "ui_Tutorial_Next" ? n : "Tiếp theo →");

        BtnSkip.Content = LanguageManager.GetString("ui_Tutorial_Skip") is string s && s != "ui_Tutorial_Skip" ? s : "Bỏ qua";

        // Render step dots
        RenderStepDots();
    }

    private void RenderStepDots()
    {
        StepDots.Items.Clear();
        for (int i = 0; i < _tutorialService.Steps.Count; i++)
        {
            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Margin = new Thickness(3, 0, 3, 0),
                Fill = i == _tutorialService.CurrentStep
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#89B4FA"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#45475A")),
            };
            StepDots.Items.Add(dot);
        }
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        if (!_tutorialService.NextStep())
        {
            Close();
        }
        else
        {
            RenderStep();
        }
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        _tutorialService.PreviousStep();
        RenderStep();
    }

    private void BtnSkip_Click(object sender, RoutedEventArgs e)
    {
        _tutorialService.MarkCompleted();
        Close();
    }

    private void Close()
    {
        Visibility = Visibility.Collapsed;
        TutorialFinished?.Invoke();
    }
}
