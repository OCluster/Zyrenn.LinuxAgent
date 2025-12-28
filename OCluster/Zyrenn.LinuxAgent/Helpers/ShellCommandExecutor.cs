using System.Diagnostics;

namespace Zyrenn.LinuxAgent.Helpers;

public static class ShellCommandExecutor
{
    #region Fields region

    // Cache the shell path to avoid repeated file checks
    private static readonly string? s_shellPath = InitializeShellPath();

    #endregion

    #region Methods region

    private static string? InitializeShellPath() =>
        File.Exists("/bin/bash") ? "/bin/bash" :
        File.Exists("/bin/sh") ? "/bin/sh" : null;

    public static string ExecuteShellCommand(string command)
    {
        if (s_shellPath == null)
            return "Error: No compatible shell found";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = s_shellPath,
                Arguments = string.Intern($"-c \"{command}\""),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        // Use StringBuilder to reduce string allocations
        using var output = new StringWriter();

        // Read the output stream line by line instead of all at once
        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            if (line != null)
            {
                output.WriteLine(line);
            }
        }

        process.WaitForExit();
        return output.ToString();
    }

    #endregion
}