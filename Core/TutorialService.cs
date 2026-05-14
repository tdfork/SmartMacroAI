using System.IO;
using System.Text.Json;
using SmartMacroAI.Localization;

namespace SmartMacroAI.Core;

/// <summary>
/// Manages the first-run tutorial wizard state and step definitions.
/// Persists completion state to a local JSON file.
/// </summary>
public sealed class TutorialService
{
    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "tutorial_state.json");

    public bool IsCompleted { get; private set; }
    public int CurrentStep { get; private set; }
    public List<TutorialStep> Steps { get; } = [];

    public TutorialService()
    {
        LoadState();
        InitializeSteps();
    }

    private void InitializeSteps()
    {
        Steps.Clear();
        Steps.AddRange(new[]
        {
            new TutorialStep
            {
                Id = "welcome",
                TitleKey = "ui_Tutorial_Welcome_Title",
                DescriptionKey = "ui_Tutorial_Welcome_Desc",
                TargetElement = null, // No highlight, just a welcome dialog
                FallbackTitle = "Chào mừng đến SmartMacroAI!",
                FallbackDescription = "Phần mềm tự động hóa thông minh. Hãy cùng tìm hiểu các tính năng chính.",
            },
            new TutorialStep
            {
                Id = "select_window",
                TitleKey = "ui_Tutorial_SelectWindow_Title",
                DescriptionKey = "ui_Tutorial_SelectWindow_Desc",
                TargetElement = "CmbTargetWindow",
                FallbackTitle = "Chọn cửa sổ mục tiêu",
                FallbackDescription = "Chọn cửa sổ game/ứng dụng mà bạn muốn tự động hóa từ danh sách dropdown.",
            },
            new TutorialStep
            {
                Id = "add_action",
                TitleKey = "ui_Tutorial_AddAction_Title",
                DescriptionKey = "ui_Tutorial_AddAction_Desc",
                TargetElement = "ActionsPanel",
                FallbackTitle = "Thêm thao tác",
                FallbackDescription = "Kéo thả các thao tác (Click, Wait, If Image...) vào canvas để tạo macro.",
            },
            new TutorialStep
            {
                Id = "run_macro",
                TitleKey = "ui_Tutorial_RunMacro_Title",
                DescriptionKey = "ui_Tutorial_RunMacro_Desc",
                TargetElement = "BtnRunMacro",
                FallbackTitle = "Chạy Macro",
                FallbackDescription = "Nhấn nút ▶ Chạy để bắt đầu. Dùng phím tắt Ctrl+F6 để Run/Stop nhanh.",
            },
            new TutorialStep
            {
                Id = "templates",
                TitleKey = "ui_Tutorial_Templates_Title",
                DescriptionKey = "ui_Tutorial_Templates_Desc",
                TargetElement = "BtnTemplates",
                FallbackTitle = "Template có sẵn",
                FallbackDescription = "Dùng template macro có sẵn cho game phổ biến để bắt đầu nhanh hơn.",
            },
            new TutorialStep
            {
                Id = "hotkeys",
                TitleKey = "ui_Tutorial_Hotkeys_Title",
                DescriptionKey = "ui_Tutorial_Hotkeys_Desc",
                TargetElement = null,
                FallbackTitle = "Phím tắt quan trọng",
                FallbackDescription = "Ctrl+F5: Ẩn/Hiện app\nCtrl+F6: Chạy/Dừng macro\nCtrl+F7: Ẩn/Hiện cửa sổ game",
            },
            new TutorialStep
            {
                Id = "done",
                TitleKey = "ui_Tutorial_Done_Title",
                DescriptionKey = "ui_Tutorial_Done_Desc",
                TargetElement = null,
                FallbackTitle = "Hoàn tất!",
                FallbackDescription = "Bạn đã sẵn sàng sử dụng SmartMacroAI. Chúc bạn tự động hóa thành công!",
            },
        });
    }

    /// <summary>
    /// Returns true if the tutorial should be shown (first run).
    /// </summary>
    public bool ShouldShowTutorial() => !IsCompleted;

    /// <summary>
    /// Advances to the next step. Returns false if already at the last step.
    /// </summary>
    public bool NextStep()
    {
        if (CurrentStep >= Steps.Count - 1)
        {
            MarkCompleted();
            return false;
        }
        CurrentStep++;
        return true;
    }

    /// <summary>
    /// Goes back to the previous step.
    /// </summary>
    public bool PreviousStep()
    {
        if (CurrentStep <= 0) return false;
        CurrentStep--;
        return true;
    }

    /// <summary>
    /// Gets the current tutorial step.
    /// </summary>
    public TutorialStep? GetCurrentStep() =>
        CurrentStep >= 0 && CurrentStep < Steps.Count ? Steps[CurrentStep] : null;

    /// <summary>
    /// Marks the tutorial as completed and persists state.
    /// </summary>
    public void MarkCompleted()
    {
        IsCompleted = true;
        CurrentStep = 0;
        SaveState();
    }

    /// <summary>
    /// Resets the tutorial so it shows again on next launch.
    /// </summary>
    public void Reset()
    {
        IsCompleted = false;
        CurrentStep = 0;
        SaveState();
    }

    private void LoadState()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var state = JsonSerializer.Deserialize<TutorialState>(json);
                if (state is not null)
                {
                    IsCompleted = state.IsCompleted;
                    CurrentStep = 0;
                }
            }
        }
        catch { }
    }

    private void SaveState()
    {
        try
        {
            var state = new TutorialState { IsCompleted = IsCompleted };
            string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }

    private class TutorialState
    {
        public bool IsCompleted { get; set; }
    }
}

/// <summary>
/// Represents a single step in the tutorial wizard.
/// </summary>
public class TutorialStep
{
    public string Id { get; set; } = "";
    public string TitleKey { get; set; } = "";
    public string DescriptionKey { get; set; } = "";
    /// <summary>x:Name of the WPF element to highlight. Null = no spotlight.</summary>
    public string? TargetElement { get; set; }
    public string FallbackTitle { get; set; } = "";
    public string FallbackDescription { get; set; } = "";

    /// <summary>Gets the localized title, falling back to Vietnamese default.</summary>
    public string GetTitle()
    {
        string? localized = LanguageManager.GetString(TitleKey);
        return string.IsNullOrEmpty(localized) || localized == TitleKey ? FallbackTitle : localized;
    }

    /// <summary>Gets the localized description, falling back to Vietnamese default.</summary>
    public string GetDescription()
    {
        string? localized = LanguageManager.GetString(DescriptionKey);
        return string.IsNullOrEmpty(localized) || localized == DescriptionKey ? FallbackDescription : localized;
    }
}
