#load "tools/package.csx"
#load "tools/publish.csx"
#load "tools/version.csx"
#load "tools/mcma-refs.csx"

var cmd = Args.FirstOrDefault()?.ToLower();
var args = Args.Skip(1).ToArray();

switch (cmd)
{
    case "package":
        await PackageAllModulesAsync(args);
        break;
    case "publish":
        await PublishAllModulesAsync(args);
        break;
    case "setmcmaversion":
        SetMcmaVersion(args.FirstOrDefault());
        break;
    default:
        Console.WriteLine($"Unrecognized command '{cmd}'");
        break;
}