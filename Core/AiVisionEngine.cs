using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace SmartMacroAI.Core;

/// <summary>
/// AI-powered image recognition using ONNX Runtime for object detection.
/// Falls back to template matching (VisionEngine) when no model is loaded
/// or when confidence is below threshold.
/// </summary>
public sealed class AiVisionEngine : IDisposable
{
    private bool _disposed;
    private string? _modelPath;
    private bool _modelLoaded;

    // ONNX Runtime session will be loaded dynamically when available
    private object? _session; // Microsoft.ML.OnnxRuntime.InferenceSession

    /// <summary>Whether a model is currently loaded and ready for inference.</summary>
    public bool IsModelLoaded => _modelLoaded;

    /// <summary>Path to the currently loaded ONNX model.</summary>
    public string? ModelPath => _modelPath;

    /// <summary>
    /// Attempts to load an ONNX model file for inference.
    /// Returns true if successful, false if ONNX Runtime is not available.
    /// </summary>
    public bool LoadModel(string onnxModelPath)
    {
        if (!File.Exists(onnxModelPath)) return false;

        try
        {
            // Try to load ONNX Runtime dynamically
            var onnxType = Type.GetType("Microsoft.ML.OnnxRuntime.InferenceSession, Microsoft.ML.OnnxRuntime");
            if (onnxType is null)
            {
                // ONNX Runtime not installed — will use fallback
                _modelPath = onnxModelPath;
                _modelLoaded = false;
                return false;
            }

            _session = Activator.CreateInstance(onnxType, onnxModelPath);
            _modelPath = onnxModelPath;
            _modelLoaded = true;
            return true;
        }
        catch
        {
            _modelLoaded = false;
            return false;
        }
    }

    /// <summary>
    /// Unloads the current model and frees resources.
    /// </summary>
    public void UnloadModel()
    {
        if (_session is IDisposable disposable)
            disposable.Dispose();
        _session = null;
        _modelLoaded = false;
        _modelPath = null;
    }

    /// <summary>
    /// Detects objects in the target window using AI model.
    /// Falls back to template matching if AI is not available.
    /// </summary>
    /// <param name="hwnd">Target window handle</param>
    /// <param name="templatePath">Template image path (used for fallback)</param>
    /// <param name="threshold">Confidence threshold (0.0 - 1.0)</param>
    /// <param name="useAi">Whether to attempt AI detection first</param>
    /// <param name="searchRegion">Optional ROI</param>
    /// <returns>Detection result with location and confidence, or null if not found</returns>
    public AiDetectionResult? Detect(IntPtr hwnd, string templatePath, double threshold,
        bool useAi = true, Rectangle? searchRegion = null)
    {
        // Try AI detection first if model is loaded
        if (useAi && _modelLoaded && _session is not null)
        {
            try
            {
                var aiResult = RunAiInference(hwnd, threshold, searchRegion);
                if (aiResult is not null)
                    return aiResult;
            }
            catch
            {
                // AI failed — fall through to template matching
            }
        }

        // Fallback: use existing template matching (VisionEngine)
        try
        {
            var detailed = VisionEngine.FindImageOnWindowDetailed(hwnd, templatePath, searchRegion);
            if (detailed.HasValue && detailed.Value.Confidence >= threshold)
            {
                return new AiDetectionResult
                {
                    Location = detailed.Value.Location,
                    Confidence = detailed.Value.Confidence,
                    DetectionMethod = "TemplateMatch",
                    Label = Path.GetFileNameWithoutExtension(templatePath),
                };
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Runs AI inference on the captured window image.
    /// This is a placeholder that will be fully implemented when ONNX Runtime is added.
    /// </summary>
    private AiDetectionResult? RunAiInference(IntPtr hwnd, double threshold, Rectangle? searchRegion)
    {
        // Capture window
        var screenshot = Win32Api.CaptureHiddenWindow(hwnd);
        if (screenshot is null) return null;

        using (screenshot)
        {
            // TODO: When ONNX Runtime NuGet is added, implement:
            // 1. Resize image to model input size (e.g., 640x640 for YOLOv8)
            // 2. Normalize pixel values
            // 3. Run inference session
            // 4. Parse output tensors for bounding boxes + confidence
            // 5. Apply NMS (Non-Maximum Suppression)
            // 6. Return best detection above threshold

            // For now, return null to trigger fallback
            return null;
        }
    }

    /// <summary>
    /// Gets available ONNX models from the models directory.
    /// </summary>
    public static List<string> GetAvailableModels()
    {
        string modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");
        if (!Directory.Exists(modelsDir))
        {
            Directory.CreateDirectory(modelsDir);
            return [];
        }

        return Directory.GetFiles(modelsDir, "*.onnx")
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .Cast<string>()
            .ToList();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnloadModel();
    }
}

/// <summary>
/// Result of an AI detection operation.
/// </summary>
public class AiDetectionResult
{
    /// <summary>Center point of the detected object (client coordinates).</summary>
    public Point Location { get; set; }

    /// <summary>Confidence score (0.0 - 1.0).</summary>
    public double Confidence { get; set; }

    /// <summary>Bounding box of the detection (if available).</summary>
    public Rectangle? BoundingBox { get; set; }

    /// <summary>Detection method used: "AI" or "TemplateMatch".</summary>
    public string DetectionMethod { get; set; } = "TemplateMatch";

    /// <summary>Label/class name of the detected object.</summary>
    public string? Label { get; set; }
}
