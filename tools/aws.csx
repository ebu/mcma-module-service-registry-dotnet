#load "./dotnet.csx"

private static async Task EnsureLambdaToolsAvailableAsync()
{
    (string stdOut, _) = await RunDotNetCmdWithoutOutputAsync("tool", "list");
    
    var areAwsLambdaToolsInstalled =
        stdOut.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
              .Any(x => x.StartsWith("amazon.lambda.tools", StringComparison.OrdinalIgnoreCase));

    if (!areAwsLambdaToolsInstalled)
    {
        Console.WriteLine("Installing dotnet CLI Lambda tools...");
        await RunDotNetCmdWithOutputAsync("tool", "install", "amazon.lambda.tools");
        Console.WriteLine("dotnet CLI Lambda tools installed successfully");
    }
    else
        Console.WriteLine("dotnet CLI Lambda tools already installed");
}

public static async Task PackageLambdaAsync(string projectFolder, string outputZipFile)
{
    await EnsureLambdaToolsAvailableAsync();

    var initialDir = Directory.GetCurrentDirectory();
    try
    {
        Directory.SetCurrentDirectory(projectFolder);
        
        await RunDotNetCmdWithOutputAsync("lambda", "package", Path.Combine(initialDir, outputZipFile));
    }
    finally
    {
        Directory.SetCurrentDirectory(initialDir);
    }
}