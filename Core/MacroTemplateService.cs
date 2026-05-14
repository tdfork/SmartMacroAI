using System.IO;
using System.Text.Json;
using SmartMacroAI.Models;

namespace SmartMacroAI.Core;

/// <summary>
/// Manages pre-built macro templates organized by category.
/// Templates are stored as JSON files in the templates/ directory.
/// </summary>
public sealed class MacroTemplateService
{
    private static readonly string TemplatesDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "templates");

    // ── Static convenience methods (used by TemplatePickerDialog) ──

    private static readonly MacroTemplateService _instance = new();

    /// <summary>Gets category names as strings (static, for dialog use).</summary>
    public static List<string> GetCategories()
    {
        return _instance.GetCategoryList().Select(c => c.Name).ToList();
    }

    /// <summary>Gets all templates (static, for dialog use).</summary>
    public static List<MacroTemplate> GetTemplates()
    {
        return _instance.GetAllTemplates();
    }

    /// <summary>Gets templates filtered by category name (static, for dialog use).</summary>
    public static List<MacroTemplate> GetTemplatesByCategory(string category)
    {
        return _instance.GetAllTemplates(category);
    }

    // ── Instance methods ──

    /// <summary>
    /// Gets all available template categories.
    /// </summary>
    public List<TemplateCategory> GetCategoryList()
    {
        var categories = new List<TemplateCategory>
        {
            new() { Id = "mmorpg", Name = "MMORPG", Icon = "⚔️" },
            new() { Id = "moba", Name = "MOBA", Icon = "🎮" },
            new() { Id = "fps", Name = "FPS", Icon = "🔫" },
            new() { Id = "idle", Name = "Idle / Clicker", Icon = "🖱️" },
            new() { Id = "web", Name = "Web Automation", Icon = "🌐" },
            new() { Id = "general", Name = "General", Icon = "📋" },
        };
        return categories;
    }

    /// <summary>
    /// Gets all templates, optionally filtered by category.
    /// </summary>
    public List<MacroTemplate> GetAllTemplates(string? categoryFilter = null)
    {
        var templates = new List<MacroTemplate>();

        if (!Directory.Exists(TemplatesDir))
        {
            Directory.CreateDirectory(TemplatesDir);
            SeedDefaultTemplates();
        }

        foreach (string file in Directory.GetFiles(TemplatesDir, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                string json = File.ReadAllText(file);
                var template = JsonSerializer.Deserialize<MacroTemplate>(json);
                if (template is null) continue;
                template.FilePath = file;

                if (categoryFilter is null || string.Equals(template.Category, categoryFilter, StringComparison.OrdinalIgnoreCase))
                    templates.Add(template);
            }
            catch { }
        }

        return templates.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
    }

    /// <summary>
    /// Loads a template as a MacroScript ready for editing.
    /// </summary>
    public MacroScript? LoadTemplate(string templateFilePath)
    {
        try
        {
            string json = File.ReadAllText(templateFilePath);
            var template = JsonSerializer.Deserialize<MacroTemplate>(json);
            if (template is null) return null;

            // Convert template to MacroScript
            var script = new MacroScript
            {
                Name = $"{template.Name} (from template)",
                TargetWindowTitle = template.DefaultTargetWindow ?? "",
                Actions = template.Actions ?? [],
                RepeatCount = template.DefaultRepeatCount,
            };

            return script;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Seeds default templates on first run.
    /// </summary>
    private void SeedDefaultTemplates()
    {
        var defaults = GetDefaultTemplates();
        foreach (var template in defaults)
        {
            string categoryDir = Path.Combine(TemplatesDir, template.Category ?? "general");
            Directory.CreateDirectory(categoryDir);

            string fileName = SanitizeFileName(template.Name) + ".json";
            string filePath = Path.Combine(categoryDir, fileName);

            if (File.Exists(filePath)) continue;

            string json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }

    private static List<MacroTemplate> GetDefaultTemplates()
    {
        return
        [
            // MMORPG Templates
            new MacroTemplate
            {
                Name = "Auto HP Potion",
                Description = "Automatically uses HP potion when health bar image is detected below threshold",
                Category = "mmorpg",
                Difficulty = "Easy",
                EstimatedSetupTime = "2 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new IfImageAction
                    {
                        DisplayName = "IF HP Low detected",
                        ImagePath = "{{hp_low_image}}",
                        Threshold = 0.8,
                        ClickOnFound = false,
                        TimeoutMs = 1000,
                        ThenActions =
                        [
                            new KeyPressAction { DisplayName = "Press HP Potion Key", VirtualKeyCode = 0x31, KeyName = "1" }
                        ],
                    },
                    new WaitAction { DisplayName = "Wait 2s", Milliseconds = 2000 },
                ]
            },
            new MacroTemplate
            {
                Name = "Auto Attack Loop",
                Description = "Clicks attack target repeatedly with configurable delay",
                Category = "mmorpg",
                Difficulty = "Easy",
                EstimatedSetupTime = "1 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new ClickAction { DisplayName = "Click Target", X = 400, Y = 300, Mode = ClickMode.Stealth },
                    new WaitAction { DisplayName = "Attack Delay", Milliseconds = 1500, DelayMin = 1200, DelayMax = 1800 },
                ]
            },
            new MacroTemplate
            {
                Name = "Auto Quest Accept/Complete",
                Description = "Detects quest NPC dialog and clicks accept/complete buttons",
                Category = "mmorpg",
                Difficulty = "Medium",
                EstimatedSetupTime = "5 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new IfImageAction
                    {
                        DisplayName = "IF Quest Accept Button",
                        ImagePath = "{{quest_accept_image}}",
                        Threshold = 0.75,
                        ClickOnFound = true,
                        TimeoutMs = 3000,
                    },
                    new WaitAction { DisplayName = "Wait", Milliseconds = 1000 },
                    new IfImageAction
                    {
                        DisplayName = "IF Quest Complete Button",
                        ImagePath = "{{quest_complete_image}}",
                        Threshold = 0.75,
                        ClickOnFound = true,
                        TimeoutMs = 3000,
                    },
                    new WaitAction { DisplayName = "Wait", Milliseconds = 1000 },
                ]
            },
            // Idle/Clicker Templates
            new MacroTemplate
            {
                Name = "Auto Clicker (Fixed Position)",
                Description = "Clicks at a fixed position with random delay to avoid detection",
                Category = "idle",
                Difficulty = "Easy",
                EstimatedSetupTime = "1 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new ClickAction { DisplayName = "Click", X = 500, Y = 400, Mode = ClickMode.Stealth },
                    new WaitAction { DisplayName = "Random Delay", Milliseconds = 500, DelayMin = 300, DelayMax = 700 },
                ]
            },
            new MacroTemplate
            {
                Name = "Multi-Point Clicker",
                Description = "Clicks multiple positions in sequence (farming pattern)",
                Category = "idle",
                Difficulty = "Easy",
                EstimatedSetupTime = "3 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new ClickAction { DisplayName = "Click Point 1", X = 200, Y = 300, Mode = ClickMode.Stealth },
                    new WaitAction { DisplayName = "Wait", Milliseconds = 500 },
                    new ClickAction { DisplayName = "Click Point 2", X = 400, Y = 300, Mode = ClickMode.Stealth },
                    new WaitAction { DisplayName = "Wait", Milliseconds = 500 },
                    new ClickAction { DisplayName = "Click Point 3", X = 600, Y = 300, Mode = ClickMode.Stealth },
                    new WaitAction { DisplayName = "Wait", Milliseconds = 1000 },
                ]
            },
            // Web Automation Templates
            new MacroTemplate
            {
                Name = "Web Login Automation",
                Description = "Navigates to a URL, fills username/password, and clicks login",
                Category = "web",
                Difficulty = "Easy",
                EstimatedSetupTime = "2 min",
                DefaultTargetWindow = "",
                DefaultRepeatCount = 1,
                Actions =
                [
                    new WebNavigateAction { DisplayName = "Navigate to Login", Url = "{{login_url}}" },
                    new WaitAction { DisplayName = "Wait for page", Milliseconds = 2000 },
                    new WebTypeAction { DisplayName = "Type Username", CssSelector = "{{username_selector}}", TextToType = "{{username}}" },
                    new WebTypeAction { DisplayName = "Type Password", CssSelector = "{{password_selector}}", TextToType = "{{password}}" },
                    new WebClickAction { DisplayName = "Click Login", CssSelector = "{{login_button_selector}}" },
                ]
            },
            new MacroTemplate
            {
                Name = "Web Form Filler (CSV)",
                Description = "Fills a web form using data from CSV rows",
                Category = "web",
                Difficulty = "Medium",
                EstimatedSetupTime = "5 min",
                DefaultTargetWindow = "",
                DefaultRepeatCount = 1,
                Actions =
                [
                    new WebNavigateAction { DisplayName = "Navigate to Form", Url = "{{form_url}}" },
                    new WaitAction { DisplayName = "Wait for page", Milliseconds = 2000 },
                    new WebTypeAction { DisplayName = "Fill Field 1", CssSelector = "{{field1_selector}}", TextToType = "{{field1_value}}" },
                    new WebTypeAction { DisplayName = "Fill Field 2", CssSelector = "{{field2_selector}}", TextToType = "{{field2_value}}" },
                    new WebClickAction { DisplayName = "Submit", CssSelector = "{{submit_selector}}" },
                    new WaitAction { DisplayName = "Wait for submit", Milliseconds = 3000 },
                ]
            },
            // FPS Templates
            new MacroTemplate
            {
                Name = "Anti-AFK (Random Movement)",
                Description = "Presses random movement keys to prevent AFK kick",
                Category = "fps",
                Difficulty = "Easy",
                EstimatedSetupTime = "1 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new KeyPressAction { DisplayName = "Press W", VirtualKeyCode = 0x57, KeyName = "W", HoldDurationMs = 500 },
                    new WaitAction { DisplayName = "Wait", Milliseconds = 3000, DelayMin = 2000, DelayMax = 5000 },
                    new KeyPressAction { DisplayName = "Press A", VirtualKeyCode = 0x41, KeyName = "A", HoldDurationMs = 300 },
                    new WaitAction { DisplayName = "Wait", Milliseconds = 4000, DelayMin = 3000, DelayMax = 6000 },
                    new KeyPressAction { DisplayName = "Press D", VirtualKeyCode = 0x44, KeyName = "D", HoldDurationMs = 400 },
                    new WaitAction { DisplayName = "Wait", Milliseconds = 5000, DelayMin = 4000, DelayMax = 8000 },
                ]
            },
            // MOBA Templates
            new MacroTemplate
            {
                Name = "Auto Skill Combo",
                Description = "Executes a skill combo sequence (Q → W → E → R) with timing",
                Category = "moba",
                Difficulty = "Medium",
                EstimatedSetupTime = "2 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 1,
                Actions =
                [
                    new KeyPressAction { DisplayName = "Cast Q", VirtualKeyCode = 0x51, KeyName = "Q", HoldDurationMs = 50 },
                    new WaitAction { DisplayName = "Q Animation", Milliseconds = 300 },
                    new KeyPressAction { DisplayName = "Cast W", VirtualKeyCode = 0x57, KeyName = "W", HoldDurationMs = 50 },
                    new WaitAction { DisplayName = "W Animation", Milliseconds = 400 },
                    new KeyPressAction { DisplayName = "Cast E", VirtualKeyCode = 0x45, KeyName = "E", HoldDurationMs = 50 },
                    new WaitAction { DisplayName = "E Animation", Milliseconds = 500 },
                    new KeyPressAction { DisplayName = "Cast R (Ultimate)", VirtualKeyCode = 0x52, KeyName = "R", HoldDurationMs = 50 },
                ]
            },
            // General Templates
            new MacroTemplate
            {
                Name = "Screenshot Timer",
                Description = "Takes a screenshot of the target window at regular intervals",
                Category = "general",
                Difficulty = "Easy",
                EstimatedSetupTime = "1 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new WaitAction { DisplayName = "Wait Interval", Milliseconds = 30000 },
                    new LogAction { DisplayName = "Log timestamp", Message = "Screenshot at {{timestamp}}" },
                ]
            },
        ];
    }

    private static string SanitizeFileName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        return new string(name.Where(c => !invalid.Contains(c)).ToArray());
    }
}

/// <summary>
/// Represents a template category for UI display.
/// </summary>
public class TemplateCategory
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}

/// <summary>
/// A macro template with metadata for the template library.
/// </summary>
public class MacroTemplate
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Category { get; set; } = "general";
    public string? Difficulty { get; set; } = "Easy";
    public string? EstimatedSetupTime { get; set; } = "1 min";
    public string? DefaultTargetWindow { get; set; }
    public int DefaultRepeatCount { get; set; } = 1;
    public List<MacroAction> Actions { get; set; } = [];

    /// <summary>Alias for DefaultTargetWindow — used by TemplatePickerDialog.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string TargetWindowTitle => DefaultTargetWindow ?? "";

    [System.Text.Json.Serialization.JsonIgnore]
    public string? FilePath { get; set; }
}
