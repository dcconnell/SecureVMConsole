using System.Diagnostics;

namespace SecureVMConsole
{
    internal static class PowerShellCommands
    {
        internal static (int exitCode, string output, string error) RunPSCommand(string script)
        {
            ProcessStartInfo psi = new()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var process = Process.Start(psi);
            var output = process?.StandardOutput.ReadToEnd() ?? "Process is null";
            var error = process?.StandardError.ReadToEnd() ?? "Process is null";
            process?.WaitForExit();
            var exitCode = process?.ExitCode ?? 999;
            return (exitCode, output, error);
        }

        internal static int RunElevatedPSCommand(string script)
        {
            string errorFile = @"C:\Temp\oraclevb_error.txt";
            // Ensure the temp directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(errorFile)!);

            ProcessStartInfo psi = new()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -Command \"{script} 2> '{errorFile}'\"",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal,

            };
            var process = Process.Start(psi);
            process?.WaitForExit();

            string error = System.IO.File.Exists(errorFile)
                ? System.IO.File.ReadAllText(errorFile)
                : string.Empty;

            PrintAndReturn($"Elevated PS Command Status: {error}\n{process?.ExitCode}", process?.ExitCode ?? 999);

            return process?.ExitCode ?? 999;
        }

        internal static int PrintAndReturn(string message, int code)
        {
            Console.WriteLine(message);
            return code;
        }
    }
}
