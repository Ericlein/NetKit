using System.Diagnostics;
using System.Text;

namespace NetKit;

public class TerminalService : IDisposable
{
    private Process? _shellProcess;
    private readonly StringBuilder _outputBuffer = new();
    private readonly object _outputLock = new();
    private bool _disposed;
    private string _currentShellType = "powershell";

    public event EventHandler<string>? OutputReceived;
    public event EventHandler<string>? ErrorReceived;
    public event EventHandler? ProcessExited;

    public bool IsRunning => _shellProcess?.HasExited == false;
    public string CurrentDirectory { get; private set; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public async Task<bool> StartShellAsync(string shellType = "powershell")
    {
        if (IsRunning)
            return true;

        _currentShellType = shellType.ToLower();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = CurrentDirectory
            };

            // Determine shell executable
            switch (shellType.ToLower())
            {
                case "powershell":
                case "pwsh":
                    startInfo.FileName = "powershell.exe";
                    startInfo.Arguments = "-NoExit -NoProfile";
                    break;
                case "cmd":
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/k";
                    break;
                case "wsl":
                    startInfo.FileName = "wsl.exe";
                    startInfo.Arguments = "";
                    break;
                default:
                    startInfo.FileName = "powershell.exe";
                    startInfo.Arguments = "-NoExit -NoProfile";
                    break;
            }

            _shellProcess = new Process { StartInfo = startInfo };

            // Setup event handlers
            _shellProcess.OutputDataReceived += OnOutputDataReceived;
            _shellProcess.ErrorDataReceived += OnErrorDataReceived;
            _shellProcess.Exited += OnProcessExited;
            _shellProcess.EnableRaisingEvents = true;

            // Start the process
            if (!_shellProcess.Start())
                return false;

            // Begin reading streams
            _shellProcess.BeginOutputReadLine();
            _shellProcess.BeginErrorReadLine();

            // Give the shell a moment to initialize
            await Task.Delay(1000);

            return true;
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(this, $"Failed to start shell: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExecuteCommandAsync(string command)
    {
        if (!IsRunning || _shellProcess?.StandardInput == null)
            return false;

        try
        {
            // Clean the command of any problematic characters
            command = command.Trim().Replace("\r", "").Replace("\n", "");

            // Handle different shell types with appropriate line endings
            if (_currentShellType == "wsl")
            {
                // For WSL/bash, use Unix line endings only
                await _shellProcess.StandardInput.WriteAsync(command + "\n");
            }
            else
            {
                // For PowerShell and CMD, use WriteLineAsync
                await _shellProcess.StandardInput.WriteLineAsync(command);
            }

            await _shellProcess.StandardInput.FlushAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorReceived?.Invoke(this, $"Failed to execute command: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        try
        {
            if (_shellProcess != null && !_shellProcess.HasExited)
            {
                // Try to gracefully close by sending exit command
                try
                {
                    if (_shellProcess.StandardInput != null)
                    {
                        // Use proper exit command and line endings based on shell type
                        if (_currentShellType == "wsl")
                        {
                            _shellProcess.StandardInput.Write("exit\n");
                        }
                        else
                        {
                            _shellProcess.StandardInput.WriteLine("exit");
                        }
                        _shellProcess.StandardInput.Flush();
                    }

                    // Wait a bit for graceful shutdown
                    if (!_shellProcess.WaitForExit(2000))
                    {
                        _shellProcess.Kill();
                    }
                }
                catch
                {
                    try
                    {
                        _shellProcess.Kill();
                    }
                    catch
                    {
                        // Ignore kill errors
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        finally
        {
            _shellProcess?.Dispose();
            _shellProcess = null;
        }
    }

    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            var cleanedData = StripAnsiEscapeSequences(e.Data);
            lock (_outputLock)
            {
                _outputBuffer.AppendLine(cleanedData);
            }
            OutputReceived?.Invoke(this, cleanedData);
        }
    }

    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data != null)
        {
            // Filter out curl progress information (these aren't real errors)
            if (!IsCurlProgressOutput(e.Data))
            {
                ErrorReceived?.Invoke(this, e.Data);
            }
            // Otherwise ignore curl progress output completely
        }
    }

    private static bool IsCurlProgressOutput(string output)
    {
        // Curl progress indicators that aren't actual errors
        return output.Contains("% Total") ||
               output.Contains("Dload  Upload") ||
               output.Contains("--:--:--") ||
               output.Trim().All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '%' || c == '-' || c == ':') ||
               string.IsNullOrWhiteSpace(output.Trim());
    }

    private static string StripAnsiEscapeSequences(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Remove ANSI escape sequences using regex pattern
        // This pattern matches ESC [ followed by any number of digits, semicolons, and letters
        return System.Text.RegularExpressions.Regex.Replace(input, @"\x1B\[[0-9;]*[A-Za-z]", "");
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        ProcessExited?.Invoke(this, EventArgs.Empty);
    }

    public string GetFullOutput()
    {
        lock (_outputLock)
        {
            return _outputBuffer.ToString();
        }
    }

    public void ClearOutput()
    {
        lock (_outputLock)
        {
            _outputBuffer.Clear();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }
}