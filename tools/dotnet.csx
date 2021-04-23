public static Task<(string stdOut, string stdErr)> RunDotNetCmdWithOutputAsync(string cmd, params string[] args)
    => RunDotNetCmdAsync(cmd, args, true);

public static Task<(string stdOut, string stdErr)> RunDotNetCmdWithoutOutputAsync(string cmd, params string[] args)
    => RunDotNetCmdAsync(cmd, args, false);

public static Task<(string stdOut, string stdErr)> RunDotNetCmdAsync(string cmd, string[] args, bool showOutput)
{
    var taskCompletionSource = new TaskCompletionSource<(string stdOut, string stdErr)>();

    var startInfo = new ProcessStartInfo("dotnet", cmd + " " + string.Join(" ", args));
    if (!showOutput)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
    }
    
    var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

    process.Exited += async (sender, args) =>
    {
        try
        {
            var exitCode = process.ExitCode; 
            var stdOut = !showOutput ? await process.StandardOutput.ReadToEndAsync() : string.Empty;
            var stdErr = !showOutput ? await process.StandardError.ReadToEndAsync() : string.Empty;
            
            if (exitCode != 0)
                taskCompletionSource.SetException(new Exception($"Process exited with code {exitCode}.{Environment.NewLine}StdErr:{Environment.NewLine}{stdErr}"));
            else
                taskCompletionSource.SetResult((stdOut, stdErr));
        }
        catch (Exception ex)
        {
            taskCompletionSource.SetException(ex);
        }
        finally
        {
            process.Dispose();
        }
    };
    
    process.Start();
    
    return taskCompletionSource.Task;
}