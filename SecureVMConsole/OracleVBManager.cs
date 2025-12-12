using System.Runtime.InteropServices;

namespace SecureVMConsole
{
    public static class OracleVBManager
    {
        public static void StartVirtualMachine(string vmName, string hyperVHost = "localhost")
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Powershell is necessary to continue.");
            }
            WindowsVMStart(vmName, hyperVHost);
        }

        private static int WindowsVMStart(string vmName, string hyperVHost)
        {
            var script = hyperVHost == "localhost"
                                        ? $"& VBoxManage startvm \"{vmName}\" --type headless"
                                        : $"Invoke-Command -ComputerName \"{hyperVHost}\" -ScriptBlock {{ & VBoxManage startvm \"{vmName}\" --type headless }}";
            var res = PowerShellCommands.RunPSCommand(script);

            int exitCode = res.exitCode switch
            {
                0 => PowerShellCommands.PrintAndReturn($"Virtual machine '{vmName}' started successfully.", 0),
                1 => InstallOracleVB(),
                _ => PowerShellCommands.PrintAndReturn($"Failed to start virtual machine '{vmName}'. Error: {res.error}. Exit Code: {res.exitCode}", res.exitCode),
            };

            if (exitCode != 0)
            {
                Console.WriteLine($"Operation failed with exit code: {exitCode}");
            }
            else
            {
                res = PowerShellCommands.RunPSCommand(script);
                Console.WriteLine($"Failed to start virtual machine '{vmName}'. Error: {res.error}. Exit Code: {res.exitCode}", res.exitCode);
            }

            return exitCode;
        }

        private static int InstallOracleVB()
        {
            string script = "winget install Oracle.VirtualBox";
            var result = PowerShellCommands.RunElevatedPSCommand(script);

            script = "[Environment]::SetEnvironmentVariable(\"Path\", \"$env:Path;C:\\Program Files\\Oracle\\VirtualBox\", [System.EnvironmentVariableTarget]::User)";
            PowerShellCommands.RunPSCommand(script);

            return result;
        }
    }
}
