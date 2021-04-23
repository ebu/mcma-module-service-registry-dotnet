#load "./dotnet.csx"

using System.IO.Compression;

public async Task PackageFunctionAppAsync(string projectFolder, string outputZipFile)
{
    var publishOutput = Path.Combine(projectFolder, "staging");
    try
    {
        await RunDotNetCmdWithOutputAsync("publish", projectFolder, "-o", publishOutput);
        
        Directory.CreateDirectory(Path.GetDirectoryName(outputZipFile));
        
        if (File.Exists(outputZipFile))
            File.Delete(outputZipFile);
            
        ZipFile.CreateFromDirectory(publishOutput, outputZipFile);
    }
    finally
    {
        try
        {
            Directory.Delete(publishOutput, true);
        }
        catch
        {
            // nothing to do at this point...
        }
    }
}