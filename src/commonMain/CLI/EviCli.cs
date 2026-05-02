using System.Diagnostics;
using System.Text.Json;
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

        if (platform == "ios")
        {
            var devices = GetIosDevices();
            if (devices.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No se encontraron simuladores de iOS disponibles.");
                return;
            }

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<IosDevice>()
                    .Title("Selecciona un simulador para [yellow]Test[/]:")
                    .AddChoices(devices)
                    .UseConverter(d =>
                    {
                        var state = d.State == "Booted" ? "[green]Encendido[/]" : "[grey]Apagado[/]";
                        return $"{d.Name} [blue]({d.Runtime})[/] - {state}";
                    })
            );

            AnsiConsole.MarkupLine($"[blue]ℹ[/] Ejecutando Test en [yellow]{selected.Name}[/] con Hot Reload...");
            var deviceArg = $"--property:_DeviceName=:v2:udid={selected.Udid}";
            var ridArg = "--property:RuntimeIdentifier=iossimulator-arm64";
            RunProcess("dotnet", $"watch run -f net9.0-ios {ridArg} {deviceArg}", testDir);
        }
        else
        {
            AnsiConsole.MarkupLine($"[blue]ℹ[/] Ejecutando proyecto Test en [yellow]macOS[/] con Hot Reload...");
            RunProcess("dotnet", "watch run -f net9.0", testDir);
        }
    }

    // ─── evi run ───────────────────────────────────────────────────────────────
    static void HandleRun(string[] args)
    {
        var platform = args.Length > 0 ? args[0].ToLower() : "macos";
        var cwd = Directory.GetCurrentDirectory();

        if (platform == "ios")
        {
            var devices = GetIosDevices();
            if (devices.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No se encontraron simuladores de iOS disponibles.");
                return;
            }

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<IosDevice>()
                    .Title("Selecciona un simulador de [yellow]iOS[/]:")
                    .PageSize(10)
                    .AddChoices(devices)
                    .UseConverter(d =>
                    {
                        var state = d.State == "Booted" ? "[green]Encendido[/]" : "[grey]Apagado[/]";
                        return $"{d.Name} [blue]({d.Runtime})[/] - {state}";
                    })
            );

            AnsiConsole.MarkupLine($"[blue]ℹ[/] Iniciando en [yellow]{selected.Name}[/] con Hot Reload...");

            // Usamos --property para evitar conflictos con los flags de dotnet watch
            // _DeviceName permite especificar el simulador exacto por UDID
            var deviceArg = $"--property:_DeviceName=:v2:udid={selected.Udid}";
            var ridArg = "--property:RuntimeIdentifier=iossimulator-arm64";
            var platformArg = "--property:EviPlatform=ios";

            RunProcess("dotnet", $"watch run -f net9.0-ios {ridArg} {deviceArg} {platformArg}", cwd);
        }
        else if (platform == "web")
        {
            AnsiConsole.MarkupLine("[blue]ℹ[/] Iniciando en [yellow]Web[/] (navegador) con Hot Reload...");
            // Compilamos con la constante WEB para activar el WebHost
            RunProcess("dotnet", "watch run -f net9.0 --property:IsEviWeb=true --property:IsEviLib=false", cwd);
        }
        else if (platform == "android")
        {
            var devices = GetAndroidDevices();
            if (devices.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No se encontraron dispositivos o emuladores de Android.");
                return;
            }

            var selected = AnsiConsole.Prompt(
                new SelectionPrompt<AndroidDevice>()
                    .Title("Selecciona un dispositivo [yellow]Android[/]:")
                    .PageSize(10)
                    .AddChoices(devices)
                    .UseConverter(d => $"{d.Name} [blue]({d.Serial})[/]")
            );

            AnsiConsole.MarkupLine($"[blue]ℹ[/] Iniciando en [yellow]{selected.Name}[/] con Hot Reload...");
            
            // _DeviceName permite especificar el serial del dispositivo
            var deviceArg = $"--property:AdbTarget={selected.Serial}";
            var platformArg = "--property:EviPlatform=android";
            RunProcess("dotnet", $"watch run -f net9.0-android {deviceArg} {platformArg}", cwd);
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

        string rid = platform switch {
            "ios" => "iossimulator-arm64",
            "android" => "android-arm64",
            _ => "osx-arm64"
        };
        
        string framework = platform switch {
            "ios" => "net9.0-ios",
            "android" => "net9.0-android",
            _ => "net9.0"
        };

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
    <TargetFrameworks>net9.0;net9.0-ios;net9.0-android</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <SelfContained>false</SelfContained>
    <ValidateExecutableReferences>false</ValidateExecutableReferences>
    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">15.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'"">21.0</SupportedOSPlatformVersion>
    <ApplicationId>com.evi.{projectName.ToLower()}</ApplicationId>
    <!-- Saltar verificación de versión de Xcode para compatibilidad entre desarrolladores -->
    <MtouchSkipXcodeVersionCheck Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">true</MtouchSkipXcodeVersionCheck>
    <_MtouchSkipXcodeVersionCheck Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">true</_MtouchSkipXcodeVersionCheck>
    <_MicrosoftiOSSdkSkipXcodeVersionCheck Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">true</_MicrosoftiOSSdkSkipXcodeVersionCheck>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""{eviLibPath}"">
      <AdditionalProperties>IsEviLib=true</AdditionalProperties>
    </ProjectReference>
  </ItemGroup>

  <!-- Inclusión dinámica de componentes nativos del framework -->
  <ItemGroup Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">
    <Compile Include=""{Path.Combine(EviRoot, "src/iosMain/**/*.cs")}"" />
    <PackageReference Include=""SkiaSharp.Views"" Version=""3.119.2"" />
  </ItemGroup>

  <ItemGroup Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'"">
    <Compile Include=""{Path.Combine(EviRoot, "src/androidMain/**/*.cs")}"" />
    <PackageReference Include=""SkiaSharp.Views"" Version=""3.119.2"" />
  </ItemGroup>

  <ItemGroup>
    <Using Include=""Evi"" />
  </ItemGroup>
</Project>";
            File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csproj);

            // AndroidManifest.xml
            var manifest = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" package=""com.evi." + projectName.ToLower() + @""">
    <application android:allowBackup=""true"" android:icon=""@mipmap/ic_launcher"" android:label=""" + projectName + @""" android:roundIcon=""@mipmap/ic_launcher_round"" android:supportsRtl=""true"">
    </application>
</manifest>";
            File.WriteAllText(Path.Combine(projectDir, "AndroidManifest.xml"), manifest);

            // Info.plist para iOS
            var plist = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>CFBundleDisplayName</key>
    <string>" + projectName + @"</string>
    <key>CFBundleIdentifier</key>
    <string>com.evi." + projectName.ToLower() + @"</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>LSRequiresIPhoneOS</key>
    <true/>
    <key>UIDeviceFamily</key>
    <array>
        <integer>1</integer>
        <integer>2</integer>
    </array>
    <key>UILaunchStoryboardName</key>
    <string>LaunchScreen</string>
    <key>UIRequiredDeviceCapabilities</key>
    <array>
        <string>arm64</string>
    </array>
    <key>UISupportedInterfaceOrientations</key>
    <array>
        <string>UIInterfaceOrientationPortrait</string>
        <string>UIInterfaceOrientationLandscapeLeft</string>
        <string>UIInterfaceOrientationLandscapeRight</string>
    </array>
</dict>
</plist>";
            File.WriteAllText(Path.Combine(projectDir, "Info.plist"), plist);

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
    static List<IosDevice> GetIosDevices()
    {
        var devices = new List<IosDevice>();
        try
        {
            var process = Process.Start(new ProcessStartInfo("xcrun", "simctl list devices --json")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            var output = process?.StandardOutput.ReadToEnd();
            process?.WaitForExit();

            if (string.IsNullOrEmpty(output)) return devices;

            using var doc = JsonDocument.Parse(output);
            var devicesJson = doc.RootElement.GetProperty("devices");
            foreach (var runtime in devicesJson.EnumerateObject())
            {
                // Solo queremos runtimes de iOS
                if (!runtime.Name.Contains("iOS")) continue;

                var runtimeName = runtime.Name.Split('.').Last().Replace("-", " ");

                foreach (var device in runtime.Value.EnumerateArray())
                {
                    if (device.GetProperty("isAvailable").GetBoolean())
                    {
                        devices.Add(new IosDevice(
                            device.GetProperty("name").GetString() ?? "Unknown",
                            device.GetProperty("udid").GetString() ?? "",
                            device.GetProperty("state").GetString() ?? "",
                            runtimeName
                        ));
                    }
                }
            }
        }
        catch { }
        return devices.OrderByDescending(d => d.State == "Booted").ThenBy(d => d.Name).ToList();
    }

    record IosDevice(string Name, string Udid, string State, string Runtime);

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

    // ─── Helpers Android ───────────────────────────────────────────────────────
    class AndroidDevice
    {
        public string Serial { get; set; } = "";
        public string Name { get; set; } = "";
    }

    static List<AndroidDevice> GetAndroidDevices()
    {
        var devices = new List<AndroidDevice>();
        try
        {
            var output = RunProcessWithOutput("adb", "devices -l");
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("List") || string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                var serial = parts[0];
                var model = "Unknown Device";
                foreach (var part in parts)
                {
                    if (part.StartsWith("model:")) model = part.Substring(6);
                }

                devices.Add(new AndroidDevice { Serial = serial, Name = model });
            }
        }
        catch { }
        return devices;
    }

    static string RunProcessWithOutput(string fileName, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = args,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = Process.Start(psi);
        return process?.StandardOutput.ReadToEnd() ?? "";
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
        table.AddRow("test [grey][macos|ios|web|android][/]", "Ejecuta el proyecto en la carpeta Test/");
        table.AddRow("run [grey][macos|ios|web|android][/]", "Ejecuta el proyecto actual");
        table.AddRow("build [grey][macos|ios|android][/]", "Compila en la carpeta build/");
        table.AddRow("doctor", "Verifica que el entorno esté listo");

        AnsiConsole.Write(new FigletText("Evi Framework").Color(Spectre.Console.Color.Blue));
        AnsiConsole.Write(table);
    }
}
