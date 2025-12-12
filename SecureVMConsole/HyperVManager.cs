using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SecureVMConsole
{
    public static class HyperVManager
    {
        public static void StartVirtualMachine(string vmName, string hyperVHost = "localhost")
        {
            try
            {
                var script = hyperVHost == "localhost"
                                ? $"Start-VM -Name \"{vmName}\""
                                : $"Invoke-Command -ComputerName \"{hyperVHost}\" -ScriptBlock {{ Start-VM -Name \"{vmName}\" }}";
                var exitCode = ExecuteVMPSCommand(script, out Process process, out string error);

                int result = exitCode switch
                {
                    0 => PrintAndReturn($"Virtual machine '{vmName}' started successfully.", 0),
                    1 => InstallHyperV(),
                    _ => PrintAndReturn($"Failed to start virtual machine '{vmName}'. Error: {error}. Exit Code: {process?.ExitCode}", process?.ExitCode ?? 999),
                };
                
                if (result == 1)
                {
                    exitCode = ExecuteVMPSCommand(script, out process, out error);
                    Console.WriteLine($"Operation completed with exit code: {exitCode}, ProcessId: {process.Id}, Error: {error}");
                }
                else
                {
                    Console.WriteLine($"Operation completed with exit code: {result}");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static int ExecuteVMPSCommand(string script, out Process process, out string error)
        {
            ProcessStartInfo psi = new()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{script}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process = Process.Start(psi);
            error = process?.StandardError.ReadToEnd() ?? "Process is null";
            process?.WaitForExit();
            return process?.ExitCode ?? 999;
        }

        /// <summary>
        /// Will not work with Windows Home edition, stopping for now doing more research.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static int InstallHyperV()
        {
            // For Windows Server, may need include this later
            //ProcessStartInfo installPsi = new()
            //{
            //    FileName = "powershell.exe",
            //    Arguments = "-NoProfile -NonInteractive -Command \"Install-WindowsFeature -Name Hyper-V-PowerShell\"",
            //    Verb = "runas",
            //    UseShellExecute = false,
            //    RedirectStandardError = true,
            //    WindowStyle = ProcessWindowStyle.Hidden,
            //};
            //var installProcess = Process.Start(installPsi);
            //installProcess?.WaitForExit();
            //if (installProcess?.ExitCode != 0)
            //{
            //    PrintAndReturn($"Failed to install Hyper-V PowerShell module. {installProcess?.StandardError.ReadToEnd()} \n{installProcess?.ExitCode ?? 999}", installProcess?.ExitCode ?? 999);
            //    throw new Exception("Hyper-V installation failed.");
            //}
            string errorFile = @"C:\Temp\hyperv_error.txt";
            // Ensure the temp directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(errorFile)!);

            ProcessStartInfo enablePsi = new()
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All -NoRestart; Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-Management-PowerShell -All 2> '{errorFile}'\"",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            
            var enableProcess = Process.Start(enablePsi);
            enableProcess?.WaitForExit();

            string error = System.IO.File.Exists(errorFile)
                ? System.IO.File.ReadAllText(errorFile)
                : string.Empty;

            if (enableProcess?.ExitCode != 0)
            {
                PrintAndReturn($"Failed to enable Hyper-V PowerShell module. {error}\n{enableProcess?.ExitCode}", enableProcess?.ExitCode ?? 999);
                throw new Exception("Hyper-V enable failed.");
            }
            return enableProcess?.ExitCode ?? 999;
        }

        private static int PrintAndReturn(string message, int code)
        {
            Console.WriteLine(message);
            return code;
        }
    }
}
