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

        Directory.CreateDirectory(TemplatesDir);

        // Always seed missing templates (so new templates appear after updates)
        SeedDefaultTemplates();

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

        // Fallback: if no JSON files found (first run or file system issue), return in-memory defaults
        if (templates.Count == 0)
        {
            var defaults = GetDefaultTemplates();
            if (categoryFilter is not null)
                defaults = defaults.Where(t => string.Equals(t.Category, categoryFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            return defaults.OrderBy(t => t.Category).ThenBy(t => t.Name).ToList();
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
            // PoE — Path of Exile Ultimatum Auto
            new MacroTemplate
            {
                Name = "⚔️ Path of Exile — Ultimatum Auto",
                Description = "Auto PoE Ultimatum đầy đủ: Flask + Skill + Vision 2 bước (Icon → Accept). Chỉ cần Snip hình và chạy.",
                Category = "mmorpg",
                Difficulty = "Advanced",
                EstimatedSetupTime = "5 min",
                DefaultTargetWindow = "Path of Exile",
                DefaultRepeatCount = 0,
                Actions =
                [
                    // Init flask timer
                    new SetVariableAction { DisplayName = "⚙️ Init flask_timer", VarName = "flask_timer", Value = "0", Operation = "Set" },

                    new RepeatAction
                    {
                        DisplayName = "🔄 Vòng lặp chính (vô hạn)",
                        RepeatCount = 0,
                        IntervalMs = 300,
                        LoopActions =
                        [
                            // ═══ FLASK — mỗi ~5 giây bấm 1-5 ═══
                            new SetVariableAction { DisplayName = "flask_timer++", VarName = "flask_timer", Value = "1", Operation = "Increment" },
                            new IfVariableAction
                            {
                                DisplayName = "🧪 Flask (mỗi 15 loop ≈ 5s)",
                                VarName = "flask_timer",
                                CompareOp = ">=",
                                Value = "15",
                                ThenActions =
                                [
                                    new KeyPressAction { DisplayName = "Flask 1", KeyName = "D1", VirtualKeyCode = 0x31, HoldDurationMs = 30, InputMode = KeyInputMode.RawInput },
                                    new WaitAction { DisplayName = "delay", DelayMin = 50, DelayMax = 80 },
                                    new KeyPressAction { DisplayName = "Flask 2", KeyName = "D2", VirtualKeyCode = 0x32, HoldDurationMs = 30, InputMode = KeyInputMode.RawInput },
                                    new WaitAction { DisplayName = "delay", DelayMin = 50, DelayMax = 80 },
                                    new KeyPressAction { DisplayName = "Flask 3", KeyName = "D3", VirtualKeyCode = 0x33, HoldDurationMs = 30, InputMode = KeyInputMode.RawInput },
                                    new WaitAction { DisplayName = "delay", DelayMin = 50, DelayMax = 80 },
                                    new KeyPressAction { DisplayName = "Flask 4", KeyName = "D4", VirtualKeyCode = 0x34, HoldDurationMs = 30, InputMode = KeyInputMode.RawInput },
                                    new WaitAction { DisplayName = "delay", DelayMin = 50, DelayMax = 80 },
                                    new KeyPressAction { DisplayName = "Flask 5", KeyName = "D5", VirtualKeyCode = 0x35, HoldDurationMs = 30, InputMode = KeyInputMode.RawInput },
                                    new WaitAction { DisplayName = "delay", DelayMin = 30, DelayMax = 60 },
                                    new SetVariableAction { DisplayName = "Reset flask_timer", VarName = "flask_timer", Value = "0", Operation = "Set" },
                                ],
                                ElseActions = []
                            },

                            // ═══ VISION — Tìm Icon → Click → Đợi → Accept ═══
                            new IfImageAction
                            {
                                DisplayName = "👁️ Bước 1: Tìm Icon Ultimatum (Snip hình vào đây)",
                                ImagePath = "",
                                ImagePaths = [],
                                Threshold = 0.65,
                                TimeoutMs = 1500,
                                RetryUntilFound = false,
                                ClickOnFound = true,
                                ClickMode = ClickMode.Raw,
                                RandomOffset = 3,
                                ThenActions =
                                [
                                    new LogAction { DisplayName = "📝 Log", Message = "Chọn icon: {{foundImageName}} tại ({{image_x}}, {{image_y}})" },
                                    new WaitAction { DisplayName = "⏳ Đợi UI Accept hiện", DelayMin = 1500, DelayMax = 2500 },

                                    // Bước 2: Tìm nút Accept Trial
                                    new IfImageAction
                                    {
                                        DisplayName = "✅ Bước 2: Tìm nút Accept (Snip hình vào đây)",
                                        ImagePath = "",
                                        ImagePaths = [],
                                        Threshold = 0.60,
                                        TimeoutMs = 8000,
                                        RetryUntilFound = true,
                                        RetryIntervalMs = 400,
                                        MaxRetryCount = 20,
                                        ClickOnFound = true,
                                        ClickMode = ClickMode.Raw,
                                        RandomOffset = 3,
                                        ThenActions =
                                        [
                                            new LogAction { DisplayName = "✅ Accepted!", Message = "✅ Accept Trial thành công!" },
                                            new WaitAction { DisplayName = "⏳ Đợi trial bắt đầu", DelayMin = 3000, DelayMax = 5000 },
                                        ],
                                        ElseActions =
                                        [
                                            new LogAction { DisplayName = "⚠️ Timeout", Message = "Không tìm thấy nút Accept sau 8s" },
                                        ]
                                    },
                                ],
                                ElseActions = []
                            },

                            // ═══ SKILL — đánh quái liên tục ═══
                            new ClickAction
                            {
                                DisplayName = "⚔️ Main Skill (Right Click giữa màn hình)",
                                X = 960, Y = 540,
                                Button = MouseButton.Right,
                                Mode = ClickMode.Raw,
                            },
                            new WaitAction { DisplayName = "Skill delay", DelayMin = 100, DelayMax = 200 },

                            new KeyPressAction { DisplayName = "🏃 Move Skill (W)", KeyName = "W", VirtualKeyCode = 0x57, HoldDurationMs = 50, InputMode = KeyInputMode.RawInput },
                            new WaitAction { DisplayName = "Move delay", DelayMin = 200, DelayMax = 400 },
                        ]
                    }
                ]
            },
            // Sunflower Land — Web3 Farming Game (browser-based)
            new MacroTemplate
            {
                Name = "🌻 Sunflower Land — Auto Harvest & Plant",
                Description = "Tự động thu hoạch và trồng lại cây trong Sunflower Land. Hỗ trợ: harvest plot → plant seed → chờ → lặp lại. Dùng cho browser (Playwright/CloakBrowser).",
                Category = "idle",
                Difficulty = "Medium",
                EstimatedSetupTime = "5 min",
                DefaultTargetWindow = "Sunflower Land",
                DefaultRepeatCount = 0,
                Actions =
                [
                    // Mở game (nếu dùng web mode)
                    new WebNavigateAction { DisplayName = "🌐 Mở Sunflower Land", Url = "https://sunflower-land.com/play" },
                    new WaitAction { DisplayName = "⏳ Đợi game load", Milliseconds = 5000, DelayMin = 4000, DelayMax = 6000 },

                    new RepeatAction
                    {
                        DisplayName = "🔄 Vòng lặp Farm (vô hạn)",
                        RepeatCount = 0,
                        IntervalMs = 1000,
                        LoopActions =
                        [
                            // ═══ THU HOẠCH — Click từng ô đất ═══
                            new IfImageAction
                            {
                                DisplayName = "🌾 Tìm cây đã chín (Snip hình cây chín vào đây)",
                                ImagePath = "",
                                ImagePaths = [],
                                Threshold = 0.70,
                                TimeoutMs = 3000,
                                RetryUntilFound = false,
                                ClickOnFound = true,
                                ClickMode = ClickMode.Stealth,
                                RandomOffset = 5,
                                ThenActions =
                                [
                                    new LogAction { DisplayName = "📝 Log harvest", Message = "Thu hoạch tại ({{image_x}}, {{image_y}})" },
                                    new WaitAction { DisplayName = "⏳ Đợi animation harvest", DelayMin = 800, DelayMax = 1200 },

                                    // Click confirm harvest (nếu có popup)
                                    new IfImageAction
                                    {
                                        DisplayName = "✅ Tìm nút Harvest/Confirm (Snip nếu có)",
                                        ImagePath = "",
                                        ImagePaths = [],
                                        Threshold = 0.65,
                                        TimeoutMs = 2000,
                                        RetryUntilFound = false,
                                        ClickOnFound = true,
                                        ClickMode = ClickMode.Stealth,
                                        RandomOffset = 3,
                                        ThenActions =
                                        [
                                            new WaitAction { DisplayName = "Đợi confirm", DelayMin = 500, DelayMax = 800 },
                                        ],
                                        ElseActions = []
                                    },
                                ],
                                ElseActions =
                                [
                                    new LogAction { DisplayName = "💤 Chưa có cây chín", Message = "Không tìm thấy cây chín — đợi..." },
                                ]
                            },

                            // ═══ TRỒNG LẠI — Click ô đất trống → chọn seed ═══
                            new IfImageAction
                            {
                                DisplayName = "🟫 Tìm ô đất trống (Snip hình đất trống vào đây)",
                                ImagePath = "",
                                ImagePaths = [],
                                Threshold = 0.70,
                                TimeoutMs = 3000,
                                RetryUntilFound = false,
                                ClickOnFound = true,
                                ClickMode = ClickMode.Stealth,
                                RandomOffset = 3,
                                ThenActions =
                                [
                                    new WaitAction { DisplayName = "⏳ Đợi menu seed mở", DelayMin = 600, DelayMax = 1000 },

                                    // Click chọn loại seed (Sunflower / Potato / Pumpkin...)
                                    new IfImageAction
                                    {
                                        DisplayName = "🌱 Chọn Seed (Snip hình seed muốn trồng)",
                                        ImagePath = "",
                                        ImagePaths = [],
                                        Threshold = 0.65,
                                        TimeoutMs = 2000,
                                        RetryUntilFound = false,
                                        ClickOnFound = true,
                                        ClickMode = ClickMode.Stealth,
                                        RandomOffset = 3,
                                        ThenActions =
                                        [
                                            new LogAction { DisplayName = "📝 Đã trồng", Message = "Trồng seed tại ({{image_x}}, {{image_y}})" },
                                            new WaitAction { DisplayName = "Đợi plant animation", DelayMin = 500, DelayMax = 800 },
                                        ],
                                        ElseActions =
                                        [
                                            new LogAction { DisplayName = "⚠️ Không tìm thấy seed", Message = "Hết seed hoặc không tìm thấy nút seed" },
                                        ]
                                    },
                                ],
                                ElseActions = []
                            },

                            // ═══ ĐỢI — Chờ cây mọc ═══
                            new WaitAction
                            {
                                DisplayName = "⏰ Đợi trước khi quét lại (30s–60s)",
                                Milliseconds = 45000,
                                DelayMin = 30000,
                                DelayMax = 60000,
                            },
                        ]
                    }
                ]
            },
            // ═══ LEGACY TEMPLATES (từ bản gốc, dùng LanguageManager) ═══
            new MacroTemplate
            {
                Name = "🔐 Auto Login (Web)",
                Description = "Mở browser, điền username/password, click login. Dùng cho web automation.",
                Category = "web",
                Difficulty = "Easy",
                EstimatedSetupTime = "2 min",
                DefaultTargetWindow = "",
                DefaultRepeatCount = 1,
                Actions =
                [
                    new LaunchAndBindAction { DisplayName = "🌐 Mở Browser", Url = "{{url}}", Browser = LaunchBrowserKind.Edge, BindTimeoutMs = 30000, PollIntervalMs = 500 },
                    new WaitAction { DisplayName = "Đợi trang load", DelayMin = 2000, DelayMax = 3000 },
                    new WebClickAction { DisplayName = "Click ô Username", CssSelector = "{{username_selector}}" },
                    new WebTypeAction { DisplayName = "Gõ Username", CssSelector = "{{username_selector}}", TextToType = "{{username}}" },
                    new WebClickAction { DisplayName = "Click ô Password", CssSelector = "{{password_selector}}" },
                    new WebTypeAction { DisplayName = "Gõ Password", CssSelector = "{{password_selector}}", TextToType = "{{password}}" },
                    new WebClickAction { DisplayName = "Click Login", CssSelector = "{{login_button_selector}}" },
                ]
            },
            new MacroTemplate
            {
                Name = "📊 Auto Fill Form (CSV)",
                Description = "Điền form web tự động từ dữ liệu CSV. Mỗi dòng CSV = 1 lần điền.",
                Category = "web",
                Difficulty = "Medium",
                EstimatedSetupTime = "5 min",
                DefaultTargetWindow = "",
                DefaultRepeatCount = 1,
                Actions =
                [
                    new LaunchAndBindAction { DisplayName = "🌐 Mở Form", Url = "{{form_url}}", Browser = LaunchBrowserKind.Edge, BindTimeoutMs = 30000 },
                    new WaitAction { DisplayName = "Đợi form load", DelayMin = 2000, DelayMax = 3000 },
                    new RepeatAction
                    {
                        DisplayName = "🔄 Lặp mỗi dòng CSV",
                        RepeatCount = 0,
                        IntervalMs = 1000,
                        LoopActions =
                        [
                            new WebClickAction { DisplayName = "Click Field 1", CssSelector = "{{field1_selector}}" },
                            new WebTypeAction { DisplayName = "Gõ giá trị 1", CssSelector = "{{field1_selector}}", TextToType = "{{col1}}" },
                            new WebClickAction { DisplayName = "Click Field 2", CssSelector = "{{field2_selector}}" },
                            new WebTypeAction { DisplayName = "Gõ giá trị 2", CssSelector = "{{field2_selector}}", TextToType = "{{col2}}" },
                            new WebClickAction { DisplayName = "Click Submit", CssSelector = "{{submit_selector}}" },
                            new WaitAction { DisplayName = "Đợi xử lý", DelayMin = 1000, DelayMax = 2000 },
                        ]
                    }
                ]
            },
            new MacroTemplate
            {
                Name = "🔄 Auto Repeat Click",
                Description = "Lặp click tại vị trí cố định. Dùng cho farm, auto-click đơn giản.",
                Category = "idle",
                Difficulty = "Easy",
                EstimatedSetupTime = "1 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new RepeatAction
                    {
                        DisplayName = "🔄 Lặp Click",
                        RepeatCount = 10,
                        IntervalMs = 5000,
                        LoopActions =
                        [
                            new ClickAction { DisplayName = "Click vị trí", X = 0, Y = 0 },
                            new WaitAction { DisplayName = "Đợi", DelayMin = 1000, DelayMax = 1500 },
                        ]
                    }
                ]
            },
            new MacroTemplate
            {
                Name = "🔍 Image Detect & Click",
                Description = "Tìm hình ảnh trên cửa sổ, nếu thấy thì click. Lặp vô hạn.",
                Category = "idle",
                Difficulty = "Medium",
                EstimatedSetupTime = "3 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new RepeatAction
                    {
                        DisplayName = "🔄 Quét hình liên tục",
                        RepeatCount = 0,
                        IntervalMs = 2000,
                        LoopActions =
                        [
                            new IfImageAction
                            {
                                DisplayName = "NẾU tìm thấy hình",
                                ImagePath = "{{image_path}}",
                                Threshold = 0.7,
                                TimeoutMs = 5000,
                                ClickOnFound = true,
                                RandomOffset = 5,
                                ThenActions = [ new WaitAction { DisplayName = "Đợi sau click", DelayMin = 500, DelayMax = 1000 } ],
                                ElseActions = [ new WaitAction { DisplayName = "Không thấy — đợi", DelayMin = 1000, DelayMax = 1000 } ],
                            },
                            new WaitAction { DisplayName = "Đợi trước lần quét tiếp", DelayMin = 500, DelayMax = 800 },
                        ]
                    }
                ]
            },
            new MacroTemplate
            {
                Name = "⌨️ Hotkey Automation (Ctrl+S)",
                Description = "Tự động bấm tổ hợp phím theo chu kỳ. Ví dụ: Ctrl+S mỗi 3 giây.",
                Category = "general",
                Difficulty = "Easy",
                EstimatedSetupTime = "1 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new RepeatAction
                    {
                        DisplayName = "🔄 Lặp phím tắt",
                        RepeatCount = 5,
                        IntervalMs = 3000,
                        LoopActions =
                        [
                            new KeyPressAction { DisplayName = "Ctrl+S", KeyName = "S", VirtualKeyCode = 0x53, Modifiers = new KeyModifiers { Ctrl = true }, HoldDurationMs = 100 },
                            new WaitAction { DisplayName = "Đợi", DelayMin = 500, DelayMax = 1000 },
                        ]
                    }
                ]
            },
            new MacroTemplate
            {
                Name = "🎮 Game Skill Rotation",
                Description = "Bấm skill 1-2-3 liên tục cho game. Dùng RawInput để vượt DirectInput.",
                Category = "mmorpg",
                Difficulty = "Easy",
                EstimatedSetupTime = "2 min",
                DefaultTargetWindow = "{{target_window}}",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new RepeatAction
                    {
                        DisplayName = "🔄 Lặp Skill",
                        RepeatCount = 0,
                        IntervalMs = 2000,
                        LoopActions =
                        [
                            new KeyPressAction { DisplayName = "Skill 1", KeyName = "D1", VirtualKeyCode = 0x31, HoldDurationMs = 80, InputMode = KeyInputMode.RawInput },
                            new WaitAction { DisplayName = "Đợi", DelayMin = 400, DelayMax = 600 },
                            new KeyPressAction { DisplayName = "Skill 2", KeyName = "D2", VirtualKeyCode = 0x32, HoldDurationMs = 80, InputMode = KeyInputMode.RawInput },
                            new WaitAction { DisplayName = "Đợi", DelayMin = 400, DelayMax = 600 },
                            new KeyPressAction { DisplayName = "Skill 3", KeyName = "D3", VirtualKeyCode = 0x33, HoldDurationMs = 80, InputMode = KeyInputMode.RawInput },
                            new WaitAction { DisplayName = "Đợi", DelayMin = 800, DelayMax = 1200 },
                        ]
                    }
                ]
            },
            // 🍁 MapleStory Auto Farm
            new MacroTemplate
            {
                Name = "🍁 MapleStory Auto Farm",
                Description = "Auto farm MapleStory: Buff + HP check + Attack + Move + Loot. Dùng Driver Level mode.",
                Category = "mmorpg",
                Difficulty = "Advanced",
                EstimatedSetupTime = "3 min",
                DefaultTargetWindow = "MapleStory",
                DefaultRepeatCount = 0,
                Actions =
                [
                    new SetVariableAction { DisplayName = "Set loop counter", VarName = "loop", Value = "0", Operation = "Set" },
                    new RepeatAction
                    {
                        DisplayName = "🔄 Main Farm Loop (infinite)",
                        RepeatCount = 0,
                        IntervalMs = 500,
                        LoopActions =
                        [
                            new SetVariableAction { DisplayName = "loop++", VarName = "loop", Value = "1", Operation = "Increment" },
                            new IfVariableAction
                            {
                                DisplayName = "IF loop == 30 → Buff",
                                VarName = "loop",
                                CompareOp = "==",
                                Value = "30",
                                ThenActions =
                                [
                                    new KeyPressAction { DisplayName = "🛡️ Buff (Page Up)", KeyName = "PageUp", VirtualKeyCode = 0x21, HoldDurationMs = 100, InputMode = KeyInputMode.DriverLevel },
                                    new WaitAction { DisplayName = "Wait buff", DelayMin = 800, DelayMax = 1200 },
                                    new SetVariableAction { DisplayName = "Reset loop", VarName = "loop", Value = "0", Operation = "Set" },
                                ],
                                ElseActions = []
                            },
                            new IfPixelColorAction
                            {
                                DisplayName = "⚠️ IF HP low",
                                X = 100, Y = 580,
                                ExpectedColor = "#1A1A1A",
                                Tolerance = 40,
                                ThenActions =
                                [
                                    new KeyPressAction { DisplayName = "💊 HP Pot (Insert)", KeyName = "Insert", VirtualKeyCode = 0x2D, HoldDurationMs = 50, InputMode = KeyInputMode.DriverLevel },
                                    new WaitAction { DisplayName = "Pot cooldown", DelayMin = 200, DelayMax = 400 },
                                ],
                                ElseActions = []
                            },
                            new KeyPressAction { DisplayName = "⚔️ Attack (Ctrl)", KeyName = "LControlKey", VirtualKeyCode = 0xA2, HoldDurationMs = 80, InputMode = KeyInputMode.DriverLevel },
                            new WaitAction { DisplayName = "Attack delay", DelayMin = 300, DelayMax = 500 },
                            new KeyPressAction { DisplayName = "⚔️ Skill 1 (A)", KeyName = "A", VirtualKeyCode = 0x41, HoldDurationMs = 80, InputMode = KeyInputMode.DriverLevel },
                            new WaitAction { DisplayName = "Skill delay", DelayMin = 400, DelayMax = 700 },
                            new KeyPressAction { DisplayName = "➡️ Move Right", KeyName = "Right", VirtualKeyCode = 0x27, HoldDurationMs = 600, InputMode = KeyInputMode.DriverLevel },
                            new WaitAction { DisplayName = "Move delay", DelayMin = 200, DelayMax = 400 },
                            new KeyPressAction { DisplayName = "💰 Loot (Z)", KeyName = "Z", VirtualKeyCode = 0x5A, HoldDurationMs = 50, InputMode = KeyInputMode.DriverLevel },
                            new WaitAction { DisplayName = "Loot delay", DelayMin = 100, DelayMax = 300 },
                        ]
                    }
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
            // Blank Macro
            new MacroTemplate
            {
                Name = "📋 Blank Macro",
                Description = "Macro trống — bắt đầu từ đầu, tự thêm action.",
                Category = "general",
                Difficulty = "Easy",
                EstimatedSetupTime = "0 min",
                DefaultTargetWindow = "",
                DefaultRepeatCount = 1,
                Actions = []
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
