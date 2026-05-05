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
        var plain = Format("DEBUG", message, skipFrames);
        if (IsGodotEnvironment)
        {
            GD.PrintRich(FormatRich("DEBUG", message, colorCord, skipFrames));
        }
        else
        {
            System.Console.WriteLine(plain);
        }
    }

    [Conditional("DEBUG")]
    public static void Info(string message, int skipFrames = 0)
    {
        skipFrames++;
        var colorCord = "#39D239";
        var plain = Format("INFO", message, skipFrames);
        if (IsGodotEnvironment)
        {
            GD.PrintRich(FormatRich("INFO", message, colorCord, skipFrames));
        }
        else
        {
            System.Console.WriteLine(plain);
        }
    }

    public static void Warn(string message, int skipFrames = 0)
    {
        skipFrames++;
        var colorCord = "#E5E545";
        var plain = Format("WARN", message, skipFrames);
        if (IsGodotEnvironment)
        {
            GD.PushWarning(plain);
            GD.PrintRich(FormatRich("WARN", message, colorCord, skipFrames));
        }
        else
        {
            System.Console.WriteLine(plain);
        }
    }

    public static void Error(string message, int skipFrames = 0)
    {
        skipFrames++;
        var colorCord = "#E54545";
        var plain = Format("ERROR", message, skipFrames);
        if (IsGodotEnvironment)
        {
            GD.PushError(Format("ERROR", message, skipFrames));
            GD.PrintRich(FormatRich("ERROR", message, colorCord, skipFrames));
        }
        else
        {
            throw new System.Exception(plain);
        }
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

    private static bool IsGodotEnvironment => Engine.GetMainLoop() != null;
}
