using System.Diagnostics;
using System.IO;
using Godot;

namespace UniteBlocksRe.Logging;

public static class Log
{
    [Conditional("DEBUG")]
    public static void Debug(string message, int skipFrames = 0)
    {
        skipFrames++;
        var colorCord = "#CE45CE";
        GD.PrintRich(FormatRich("DEBUG", message, colorCord, skipFrames));
    }

    [Conditional("DEBUG")]
    public static void Info(string message, int skipFrames = 0)
    {
        skipFrames++;
        var colorCord = "#39D239";
        GD.PrintRich(FormatRich("INFO", message, colorCord, skipFrames));
    }

    public static void Warn(string message, int skipFrames = 0)
    {
        skipFrames++;
        var colorCord = "#E5E545";
        GD.PushWarning(Format("WARN", message, skipFrames));
        GD.PrintRich(FormatRich("WARN", message, colorCord, skipFrames));
    }

    public static void Error(string message, int skipFrames = 0)
    {
        skipFrames++;
        var colorCord = "#E54545";
        GD.PushError(Format("ERROR", message, skipFrames));
        GD.PrintRich(FormatRich("ERROR", message, colorCord, skipFrames));
    }

    private static string FormatRich(string level, string message, string colorCord, int skipFrames)
    {
        skipFrames++;
        var locationInfo = GetLocationInfo(skipFrames);

        return $"[b][color={colorCord}][{level}][/color][/b] {message} [color=#595959]{locationInfo}[/color]";
    }

    private static string Format(string level, string message, int skipFrames)
    {
        skipFrames++;
        var locationInfo = GetLocationInfo(skipFrames);

        return $"[{level}] {message} {locationInfo}";
    }

    private static string GetLocationInfo(int skipFrames)
    {
        skipFrames++; // このメソッド自体のフレームをスキップ

        var frame = new StackFrame(skipFrames, true);
        var fileName = Path.GetFileName(frame.GetFileName());
        var line = frame.GetFileLineNumber();
        var methodName = frame.GetMethod()?.Name ?? "Unknown";
        return $"line {line}:{methodName}() in {fileName}";
    }
}
