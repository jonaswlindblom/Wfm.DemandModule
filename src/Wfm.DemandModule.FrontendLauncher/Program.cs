using System.Diagnostics;

var solutionRoot = FindSolutionRoot(AppContext.BaseDirectory);
var frontendDirectory = Path.Combine(solutionRoot, "frontend.ui");
var packageJsonPath = Path.Combine(frontendDirectory, "package.json");

Console.Title = "Frontend UI Dev Server";

if (!File.Exists(packageJsonPath))
{
    Console.Error.WriteLine("[FEL] Hittar inte frontend.ui\\package.json");
    Console.Error.WriteLine("Kontrollera att launchern startas från solution-roten.");
    return 1;
}

Console.WriteLine($"Startar frontend i {frontendDirectory}");

var startInfo = new ProcessStartInfo
{
    FileName = "cmd.exe",
    Arguments = "/c npm run dev -- --open",
    WorkingDirectory = frontendDirectory,
    UseShellExecute = false
};

using var process = Process.Start(startInfo);

if (process is null)
{
    Console.Error.WriteLine("[FEL] Kunde inte starta frontend.");
    return 1;
}

process.WaitForExit();
return process.ExitCode;

static string FindSolutionRoot(string startDirectory)
{
    var directory = new DirectoryInfo(startDirectory);

    while (directory is not null)
    {
        var frontendPath = Path.Combine(directory.FullName, "frontend.ui", "package.json");
        if (File.Exists(frontendPath))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Kunde inte hitta solution-roten utifrån frontend.ui\\package.json.");
}
