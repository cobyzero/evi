using System.Diagnostics;
using Spectre.Console;

namespace Evi.CLI;

public class EviCli
{
    static readonly string EviRoot = FindEviRoot();

    private static string FindEviRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "evi.csproj")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        // Fallback para cuando se ejecuta via dotnet run desde el root
        return Directory.GetCurrentDirectory();
    }

    public static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var command = args[0].ToLower();
        switch (command)
        {
            case "test":
                HandleTest(args.Skip(1).ToArray());
                break;
            case "run":
                HandleRun(args.Skip(1).ToArray());
                break;
            case "build":
                HandleBuild(args.Skip(1).ToArray());
                break;
            case "create":
                HandleCreate(args.Skip(1).ToArray());
                break;
            case "doctor":
                HandleDoctor();
                break;
            default:
                AnsiConsole.MarkupLine("[red]Error:[/] Comando desconocido. Usa [yellow]evi[/] para ver los comandos disponibles.");
                break;
        }
    }

    // ─── evi test ──────────────────────────────────────────────────────────────
    static void HandleTest(string[] args)
    {
        var platform = args.Length > 0 ? args[0].ToLower() : "macos";
        var testDir = Path.Combine(EviRoot, "Test");

        if (!Directory.Exists(testDir))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No se encontró la carpeta [yellow]Test/[/]. ¿Ya la creaste?");
            return;
        }

        var framework = platform == "ios" ? "net9.0-ios" : "net9.0";
        AnsiConsole.MarkupLine($"[blue]ℹ[/] Ejecutando proyecto Test en [yellow]{platform}[/] con Hot Reload...");
        RunProcess("dotnet", $"watch run -f {framework}", testDir);
    }

    // ─── evi run ───────────────────────────────────────────────────────────────
    static void HandleRun(string[] args)
    {
        var platform = args.Length > 0 ? args[0].ToLower() : "macos";
        var cwd = Directory.GetCurrentDirectory();

        if (platform == "ios")
        {
            AnsiConsole.Status()
                .Start("Compilando y preparando simulador de iOS con Hot Reload...", ctx =>
                {
                    RunProcess("dotnet", "watch run -f net9.0-ios", cwd);
                });
        }
        else if (platform == "macos")
        {
            AnsiConsole.MarkupLine("[blue]ℹ[/] Iniciando en macOS con Hot Reload...");
            RunProcess("dotnet", "watch run -f net9.0", cwd);
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Plataforma no soportada: " + platform);
        }
    }

    // ─── evi build ─────────────────────────────────────────────────────────────
    static void HandleBuild(string[] args)
    {
        var platform = args.Length > 0 ? args[0].ToLower() : "macos";
        var cwd = Directory.GetCurrentDirectory();

        AnsiConsole.MarkupLine($"[green]✔[/] Generando binario para [yellow]{platform}[/]...");

        string rid = platform == "ios" ? "iossimulator-arm64" : "osx-arm64";
        string framework = platform == "ios" ? "net9.0-ios" : "net9.0";

        RunProcess("dotnet", $"publish -f {framework} -c Release -p:RuntimeIdentifier={rid} -o build/{platform}", cwd);
    }

    // ─── evi create ────────────────────────────────────────────────────────────
    static void HandleCreate(string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Especifica un nombre. Ejemplo: [yellow]evi create mi_app[/]");
            return;
        }

        var projectName = args[0];
        var projectDir = Path.Combine(Directory.GetCurrentDirectory(), projectName);

        if (Directory.Exists(projectDir))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] El directorio '{projectName}' ya existe.");
            return;
        }

        AnsiConsole.Status().Start($"Creando proyecto [yellow]{projectName}[/]...", ctx =>
        {
            Directory.CreateDirectory(projectDir);

            // .csproj del nuevo proyecto de usuario
            var eviLibPath = Path.Combine(EviRoot, "evi.csproj");
            var csproj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net9.0;net9.0-ios</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">15.0</SupportedOSPlatformVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""{eviLibPath}"" />
  </ItemGroup>

  <ItemGroup>
    <Using Include=""Evi"" />
  </ItemGroup>
</Project>";
            File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csproj);

            // Program.cs del nuevo proyecto
            var program = @"using Evi;

App.Run(() => new MyFirstApp());

public class MyFirstApp : Component
{
    public override RenderNode Build()
    {
        return new Scaffold
        {
            AppBar = new AppBar { Title = ""Mi Primera App Evi"" },
            Body = new Center
            {
                Child = new Text
                {
                    Value = ""¡Hola desde Evi!"",
                    FontSize = 32,
                    Color = Color.White
                }
            },
            FloatingActionButton = new FloatingActionButton
            {
                Child = new Text { Value = ""+"", Color = Color.White, FontSize = 24 },
                OnPressed = () => Console.WriteLine(""Click!"")
            }
        }.Build();
    }
}
";
            File.WriteAllText(Path.Combine(projectDir, "Program.cs"), program);
        });

        AnsiConsole.MarkupLine($"[green]✔[/] Proyecto [yellow]{projectName}[/] creado con éxito.");
        AnsiConsole.MarkupLine($"[blue]ℹ[/] Para ejecutarlo: [yellow]cd {projectName} && evi run[/]");
    }

    // ─── evi doctor ────────────────────────────────────────────────────────────
    static void HandleDoctor()
    {
        AnsiConsole.Write(new Rule("[yellow]Evi Doctor[/]"));

        CheckTool("dotnet", "--version", ".NET SDK");
        CheckTool("xcodebuild", "-version", "Xcode");

        AnsiConsole.MarkupLine("\n[green]Todo parece estar en orden. ¡Feliz codificación![/]");
    }

    // ─── helpers ───────────────────────────────────────────────────────────────
    static void CheckTool(string cmd, string arguments, string name)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo(cmd, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            process?.WaitForExit();
            if (process?.ExitCode == 0)
                AnsiConsole.MarkupLine($"[green]✔[/] {name} instalado.");
            else
                AnsiConsole.MarkupLine($"[red]✘[/] {name} no funciona correctamente.");
        }
        catch
        {
            AnsiConsole.MarkupLine($"[red]✘[/] {name} no encontrado.");
        }
    }

    static void RunProcess(string cmd, string arguments, string workingDir)
    {
        var startInfo = new ProcessStartInfo(cmd, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            WorkingDirectory = workingDir
        };
        var process = Process.Start(startInfo);
        process?.WaitForExit();
    }

    static void ShowHelp()
    {
        var table = new Table();
        table.AddColumn("[yellow]Comando[/]");
        table.AddColumn("[yellow]Descripción[/]");

        table.AddRow("create [grey]<nombre>[/]", "Crea un nuevo proyecto Evi");
        table.AddRow("test [grey][macos|ios][/]", "Ejecuta el proyecto en la carpeta Test/");
        table.AddRow("run [grey][macos|ios][/]", "Ejecuta el proyecto actual");
        table.AddRow("build [grey][macos|ios][/]", "Compila en la carpeta build/");
        table.AddRow("doctor", "Verifica que el entorno esté listo");

        AnsiConsole.Write(new FigletText("Evi Framework").Color(Spectre.Console.Color.Blue));
        AnsiConsole.Write(table);
    }
}
