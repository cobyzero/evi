using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace Evi.CLI;

public class EviCli
{
    static readonly string EviRoot = FindEviRoot();
    private static EviConfig? _config;
    private static EviConfig Config => _config ??= LoadConfig();

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

    private static EviConfig LoadConfig()
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), "evi.json");
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<EviConfig>(json, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                }) ?? new EviConfig();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Aviso:[/] No se pudo leer [yellow]evi.json[/] ({ex.Message}).");
            }
        }
        return new EviConfig();
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
            EnsureIosLaunchScreenAssets(testDir);

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
            EnsureIosLaunchScreenAssets(cwd);
            EnsureIosProjectWatchCompatibility(cwd);
            EnsurePlatformTargetFrameworkCompatibility(cwd);

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

            TryBootIosSimulator(selected.Udid);

            RunProcess(
                "dotnet",
                $"watch run -f net9.0-ios {ridArg} {deviceArg} {platformArg}",
                cwd,
                new Dictionary<string, string>
                {
                    ["EviPlatform"] = "ios",
                    ["_DeviceName"] = $":v2:udid={selected.Udid}",
                    ["RuntimeIdentifier"] = "iossimulator-arm64"
                }
            );
        }
        else if (platform == "web")
        {
            AnsiConsole.MarkupLine("[blue]ℹ[/] Iniciando en [yellow]Web[/] (navegador) con Hot Reload...");
            // Compilamos con la constante WEB para activar el WebHost
            RunProcess("dotnet", "watch run -f net9.0 --property:IsEviWeb=true --property:IsEviLib=false", cwd);
        }
        else if (platform == "android")
        {
            var projectName = Path.GetFileName(cwd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var packageId = $"com.evi.{projectName.ToLowerInvariant()}";
            EnsureAndroidNativeScaffold(cwd, packageId, projectName);

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
            
            var sdkPath = FindAndroidSdk();
            if (string.IsNullOrEmpty(sdkPath))
            {
                AnsiConsole.MarkupLine("[yellow]Aviso:[/] No se encontró el Android SDK automáticamente.");
                AnsiConsole.MarkupLine("  [grey]Si el build falla, intenta definir ANDROID_HOME o usar --property:AndroidSdkDirectory en el comando.[/]");
            }

            // _DeviceName permite especificar el serial del dispositivo
            var deviceArg = $"--property:AdbTarget={selected.Serial}";
            var platformArg = "--property:EviPlatform=android";
            var sdkArg = !string.IsNullOrEmpty(sdkPath) ? $"--property:AndroidSdkDirectory=\"{sdkPath}\"" : "";

            var envVars = new Dictionary<string, string> { ["EviPlatform"] = "android" };
            if (!string.IsNullOrEmpty(sdkPath))
            {
                envVars["ANDROID_HOME"] = sdkPath;
                envVars["AndroidSdkDirectory"] = sdkPath;
            }

            RunProcess(
                "dotnet",
                $"watch build -t:Run -f net9.0-android {deviceArg} {platformArg} {sdkArg}",
                cwd,
                envVars
            );
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
    <TargetFramework>net9.0</TargetFramework>
    <TargetFramework Condition=""'$(EviPlatform)' == 'ios'"">net9.0-ios</TargetFramework>
    <TargetFramework Condition=""'$(EviPlatform)' == 'android'"">net9.0-android</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <SelfContained>false</SelfContained>
    <ValidateExecutableReferences>false</ValidateExecutableReferences>
    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">15.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'"">21.0</SupportedOSPlatformVersion>
    <ApplicationId>com.evi.{projectName.ToLower()}</ApplicationId>
    <MtouchSkipXcodeVersionCheck Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">true</MtouchSkipXcodeVersionCheck>
    <_MtouchSkipXcodeVersionCheck Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">true</_MtouchSkipXcodeVersionCheck>
    <_MicrosoftiOSSdkSkipXcodeVersionCheck Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">true</_MicrosoftiOSSdkSkipXcodeVersionCheck>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""{eviLibPath}"">
      <AdditionalProperties>IsEviLib=true;EviPlatform=$(EviPlatform)</AdditionalProperties>
    </ProjectReference>
  </ItemGroup>

  <PropertyGroup Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">
    <IPhoneResourcePrefix>Platforms/iOS</IPhoneResourcePrefix>
    <IsAppBundle>true</IsAppBundle>
    <UseInterpreter>true</UseInterpreter>
    <MtouchLink Condition=""'$(Configuration)' == 'Debug'"">None</MtouchLink>
  </PropertyGroup>

  <ItemGroup Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">
    <None Include=""Platforms\iOS\Info.plist"" LogicalName=""Info.plist"" />
    <PackageReference Include=""SkiaSharp.Views"" Version=""3.119.2"" />
  </ItemGroup>

  <PropertyGroup Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'"">
    <AndroidManifest>Platforms\Android\AndroidManifest.xml</AndroidManifest>
  </PropertyGroup>

  <ItemGroup Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'"">
    <PackageReference Include=""SkiaSharp.Views"" Version=""3.119.2"" />
  </ItemGroup>

  <ItemGroup>
    <Using Include=""Evi"" />
  </ItemGroup>

</Project>";
            File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csproj);
            
            // evi.json
            var eviJson = @"{
  ""name"": """ + projectName + @""",
  ""id"": ""com.evi." + projectName.ToLower() + @""",
  ""android"": {
    ""sdk"": """"
  },
  ""ios"": {
    ""bundleId"": ""com.evi." + projectName.ToLower() + @"""
  }
}";
            File.WriteAllText(Path.Combine(projectDir, "evi.json"), eviJson);

            // Directorios de plataforma
            Directory.CreateDirectory(Path.Combine(projectDir, "Platforms", "Android"));
            Directory.CreateDirectory(Path.Combine(projectDir, "Platforms", "iOS"));

            // AndroidManifest.xml
            var manifest = @"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" package=""com.evi." + projectName.ToLower() + @""">
    <application android:allowBackup=""true"" android:label=""" + projectName + @""" android:supportsRtl=""true"">
    </application>
</manifest>";
            File.WriteAllText(Path.Combine(projectDir, "Platforms", "Android", "AndroidManifest.xml"), manifest);

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
            File.WriteAllText(Path.Combine(projectDir, "Platforms", "iOS", "Info.plist"), plist);

            // LaunchScreen.storyboard mínimo para evitar crash al abrir si Info.plist lo referencia
            var launchStoryboard = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<document type=""com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB"" version=""3.0"" toolsVersion=""24096"" targetRuntime=""iOS.CocoaTouch"" propertyAccessControl=""none"" useAutolayout=""YES"" launchScreen=""YES"" useTraitCollections=""YES"" useSafeAreas=""YES"" initialViewController=""01J-lp-oVM"">
    <device id=""retina6_12"" orientation=""portrait"" appearance=""light""/>
    <scenes>
        <scene sceneID=""EHf-IW-A2E"">
            <objects>
                <viewController id=""01J-lp-oVM"" sceneMemberID=""viewController"">
                    <view key=""view"" contentMode=""scaleToFill"" id=""Ze5-6b-2t3"">
                        <rect key=""frame"" x=""0.0"" y=""0.0"" width=""393"" height=""852""/>
                        <autoresizingMask key=""autoresizingMask"" widthSizable=""YES"" heightSizable=""YES""/>
                        <color key=""backgroundColor"" red=""1"" green=""1"" blue=""1"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier=""IBFirstResponder"" id=""iYj-Kq-Ea1"" userLabel=""First Responder"" sceneMemberID=""firstResponder""/>
            </objects>
            <point key=""canvasLocation"" x=""52.0"" y=""375.0""/>
        </scene>
    </scenes>
</document>
";
            File.WriteAllText(Path.Combine(projectDir, "Platforms", "iOS", "LaunchScreen.storyboard"), launchStoryboard);

            // Program.cs del nuevo proyecto
            var program = @"using Evi;
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(AppHotReloadBridge))]

App.Run(() => new MyFirstApp());

public static class AppHotReloadBridge
{
    public static void ClearCache(Type[]? types)
    {
        Console.WriteLine(""[Evi Hot Reload] ClearCache invoked."");
    }

    public static void UpdateApplication(Type[]? types)
    {
        Console.WriteLine($""[Evi Hot Reload] App bridge invoked. Types: {types?.Length ?? 0}"");
        Evi.HotReloadManager.UpdateApplication(types);
    }
}

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

            // Scaffold Android Studio (android/) estilo proyecto nativo editable
            CreateAndroidStudioScaffold(projectDir, $"com.evi.{projectName.ToLower()}", projectName);
        });

        AnsiConsole.MarkupLine($"[green]✔[/] Proyecto [yellow]{projectName}[/] creado con éxito.");
        AnsiConsole.MarkupLine($"[blue]ℹ[/] Para ejecutarlo: [yellow]cd {projectName} && evi run[/]");
    }

    // ─── evi doctor ────────────────────────────────────────────────────────────
    static void HandleDoctor()
    {
        AnsiConsole.Write(new Rule("[yellow]Evi Doctor[/]"));

        // Mostrar config si existe
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "evi.json")))
        {
            AnsiConsole.MarkupLine($"[blue]ℹ[/] Usando configuración de: [yellow]evi.json[/]");
            if (!string.IsNullOrEmpty(Config.Name)) AnsiConsole.MarkupLine($"  Proyecto: [grey]{Config.Name}[/]");
        }

        CheckTool("dotnet", "--version", ".NET SDK");
        CheckTool("xcodebuild", "-version", "Xcode");
        CheckTool("adb", "version", "Android Debug Bridge (adb)");

        // Verificar Android SDK
        var sdkPath = FindAndroidSdk();
        if (!string.IsNullOrEmpty(sdkPath))
        {
            AnsiConsole.MarkupLine($"[green]✔[/] Android SDK encontrado en: [grey]{sdkPath}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✘[/] Android SDK no encontrado.");
            AnsiConsole.MarkupLine("  [grey]Sugerencia: Define la variable de entorno ANDROID_HOME o instala el SDK.[/]");
        }

        AnsiConsole.MarkupLine("\n[blue]ℹ[/] Si tienes problemas con Android, asegúrate de tener instalado el workload:");
        AnsiConsole.MarkupLine("  [yellow]dotnet workload install android[/]");

        AnsiConsole.MarkupLine("\n[green]Chequeo completado.[/]");
    }

    static string? FindAndroidSdk()
    {
        // 0. Archivo de configuración (evi.json)
        if (!string.IsNullOrEmpty(Config.Android?.Sdk) && Directory.Exists(Config.Android.Sdk))
        {
            return Config.Android.Sdk;
        }

        // 1. Variable de entorno
        var env = Environment.GetEnvironmentVariable("ANDROID_HOME") ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) return env;

        // 2. Ubicaciones comunes en macOS
        var commonPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Android/sdk"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Developer/Xamarin/android-sdk-macosx"),
            "/usr/local/share/android-sdk",
            "/opt/android-sdk",
            "/opt/homebrew/share/android-sdk"
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path)) return path;
        }

        return null;
    }

    static void EnsureIosLaunchScreenAssets(string projectDir)
    {
        try
        {
            var iosDir = Path.Combine(projectDir, "Platforms", "iOS");
            var plistPath = Path.Combine(iosDir, "Info.plist");
            if (!File.Exists(plistPath))
                return;

            var plist = File.ReadAllText(plistPath);
            if (!plist.Contains("<key>UILaunchStoryboardName</key>", StringComparison.Ordinal))
                return;

            Directory.CreateDirectory(iosDir);

            var storyboardPath = Path.Combine(iosDir, "LaunchScreen.storyboard");
            if (!File.Exists(storyboardPath))
            {
                var launchStoryboard = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<document type=""com.apple.InterfaceBuilder3.CocoaTouch.Storyboard.XIB"" version=""3.0"" toolsVersion=""24096"" targetRuntime=""iOS.CocoaTouch"" propertyAccessControl=""none"" useAutolayout=""YES"" launchScreen=""YES"" useTraitCollections=""YES"" useSafeAreas=""YES"" initialViewController=""01J-lp-oVM"">
    <device id=""retina6_12"" orientation=""portrait"" appearance=""light""/>
    <scenes>
        <scene sceneID=""EHf-IW-A2E"">
            <objects>
                <viewController id=""01J-lp-oVM"" sceneMemberID=""viewController"">
                    <view key=""view"" contentMode=""scaleToFill"" id=""Ze5-6b-2t3"">
                        <rect key=""frame"" x=""0.0"" y=""0.0"" width=""393"" height=""852""/>
                        <autoresizingMask key=""autoresizingMask"" widthSizable=""YES"" heightSizable=""YES""/>
                        <color key=""backgroundColor"" red=""1"" green=""1"" blue=""1"" alpha=""1"" colorSpace=""custom"" customColorSpace=""sRGB""/>
                    </view>
                </viewController>
                <placeholder placeholderIdentifier=""IBFirstResponder"" id=""iYj-Kq-Ea1"" userLabel=""First Responder"" sceneMemberID=""firstResponder""/>
            </objects>
            <point key=""canvasLocation"" x=""52.0"" y=""375.0""/>
        </scene>
    </scenes>
</document>
";
                File.WriteAllText(storyboardPath, launchStoryboard);
                AnsiConsole.MarkupLine("[blue]ℹ[/] Se creó [yellow]Platforms/iOS/LaunchScreen.storyboard[/] automáticamente.");
            }

            var csprojPath = Directory.GetFiles(projectDir, "*.csproj").FirstOrDefault();
            if (string.IsNullOrEmpty(csprojPath))
                return;

            var csproj = File.ReadAllText(csprojPath);
            if (!csproj.Contains("LaunchScreen.storyboard", StringComparison.Ordinal))
            {
                var itemGroup = @"
  <ItemGroup Condition=""$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'"">
    <InterfaceDefinition Include=""Platforms\iOS\LaunchScreen.storyboard"" />
  </ItemGroup>
";
                var insertAt = csproj.LastIndexOf("</Project>", StringComparison.Ordinal);
                if (insertAt >= 0)
                {
                    csproj = csproj.Insert(insertAt, itemGroup);
                    File.WriteAllText(csprojPath, csproj);
                    AnsiConsole.MarkupLine("[blue]ℹ[/] Se agregó [yellow]LaunchScreen.storyboard[/] al .csproj automáticamente.");
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Aviso:[/] No se pudo verificar LaunchScreen en iOS ({ex.Message}).");
        }
    }

    static void CreateAndroidStudioScaffold(string projectDir, string packageId, string appName)
    {
        try
        {
            var androidDir = Path.Combine(projectDir, "android");
            var appDir = Path.Combine(androidDir, "app");
            var wrapperDir = Path.Combine(androidDir, "gradle", "wrapper");
            var packagePath = packageId.Replace('.', Path.DirectorySeparatorChar);
            var kotlinDir = Path.Combine(appDir, "src", "main", "java", packagePath);
            var resLayoutDir = Path.Combine(appDir, "src", "main", "res", "layout");
            var resValuesDir = Path.Combine(appDir, "src", "main", "res", "values");

            Directory.CreateDirectory(androidDir);
            Directory.CreateDirectory(appDir);
            Directory.CreateDirectory(wrapperDir);
            Directory.CreateDirectory(kotlinDir);
            Directory.CreateDirectory(resLayoutDir);
            Directory.CreateDirectory(resValuesDir);

            var settingsGradle = $@"pluginManagement {{
    repositories {{
        google()
        mavenCentral()
        gradlePluginPortal()
    }}
}}

dependencyResolutionManagement {{
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {{
        google()
        mavenCentral()
    }}
}}

rootProject.name = ""{appName}""
include("":app"")
";

            var rootBuildGradle = @"plugins {
    id(""com.android.application"") version ""8.6.1"" apply false
    id(""org.jetbrains.kotlin.android"") version ""2.0.21"" apply false
}
";

            var gradleProps = @"org.gradle.jvmargs=-Xmx2g -Dfile.encoding=UTF-8
android.useAndroidX=true
kotlin.code.style=official
";

            var gradleWrapperProps = @"distributionBase=GRADLE_USER_HOME
distributionPath=wrapper/dists
distributionUrl=https\://services.gradle.org/distributions/gradle-8.7-bin.zip
zipStoreBase=GRADLE_USER_HOME
zipStorePath=wrapper/dists
";

            var sdkPath = FindAndroidSdk();
            var localProps = !string.IsNullOrEmpty(sdkPath)
                ? $"sdk.dir={sdkPath.Replace("\\", "\\\\").Replace(":", "\\:")}\n"
                : "# sdk.dir=/absolute/path/to/Android/sdk\n";

            var gradlew = @"#!/bin/sh

DIR=""$(cd ""$(dirname ""$0"")"" && pwd)""
WRAPPER_JAR=""$DIR/gradle/wrapper/gradle-wrapper.jar""

if [ ! -f ""$WRAPPER_JAR"" ]; then
  echo ""Missing gradle-wrapper.jar. Open this folder in Android Studio once to generate/sync wrapper, or run 'gradle wrapper' if Gradle is installed.""
  exit 1
fi

exec java -classpath ""$WRAPPER_JAR"" org.gradle.wrapper.GradleWrapperMain ""$@""
";

            var gradlewBat = @"@ECHO OFF
SET DIR=%~dp0
SET WRAPPER_JAR=%DIR%gradle\wrapper\gradle-wrapper.jar

IF NOT EXIST ""%WRAPPER_JAR%"" (
  ECHO Missing gradle-wrapper.jar. Open this folder in Android Studio once to generate/sync wrapper, or run ""gradle wrapper"" if Gradle is installed.
  EXIT /B 1
)

java -classpath ""%WRAPPER_JAR%"" org.gradle.wrapper.GradleWrapperMain %*
";

            var appBuildGradle = $@"plugins {{
    id(""com.android.application"")
    id(""org.jetbrains.kotlin.android"")
}}

android {{
    namespace = ""{packageId}""
    compileSdk = 35

    defaultConfig {{
        applicationId = ""{packageId}""
        minSdk = 24
        targetSdk = 35
        versionCode = 1
        versionName = ""1.0""
    }}

    buildTypes {{
        release {{
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile(""proguard-android-optimize.txt""),
                ""proguard-rules.pro""
            )
        }}
    }}

    compileOptions {{
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }}
    kotlinOptions {{
        jvmTarget = ""17""
    }}
}}

dependencies {{
    implementation(""androidx.core:core-ktx:1.15.0"")
    implementation(""androidx.appcompat:appcompat:1.7.0"")
    implementation(""com.google.android.material:material:1.12.0"")
}}
";

            var manifest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"">
    <application
        android:allowBackup=""true""
        android:label=""{appName}""
        android:supportsRtl=""true""
        android:theme=""@style/Theme.{appName}"">
        <activity
            android:name="".MainActivity""
            android:exported=""true"">
            <intent-filter>
                <action android:name=""android.intent.action.MAIN"" />
                <category android:name=""android.intent.category.LAUNCHER"" />
            </intent-filter>
        </activity>
    </application>
</manifest>
";

            var kt = $@"package {packageId}

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity

class MainActivity : AppCompatActivity() {{
    override fun onCreate(savedInstanceState: Bundle?) {{
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
    }}
}}
";

            var layout = @"<?xml version=""1.0"" encoding=""utf-8""?>
<LinearLayout xmlns:android=""http://schemas.android.com/apk/res/android""
    android:layout_width=""match_parent""
    android:layout_height=""match_parent""
    android:gravity=""center""
    android:orientation=""vertical"">

    <TextView
        android:layout_width=""wrap_content""
        android:layout_height=""wrap_content""
        android:text=""Android nativo listo (Evi)""
        android:textSize=""22sp"" />
</LinearLayout>
";

            var strings = $@"<resources>
    <string name=""app_name"">{appName}</string>
</resources>
";

            var themes = $@"<resources xmlns:tools=""http://schemas.android.com/tools"">
    <style name=""Theme.{appName}"" parent=""Theme.Material3.DayNight.NoActionBar"">
    </style>
</resources>
";

            var readme = @"# Android nativo (estilo Flutter)

Esta carpeta `android/` es un proyecto Gradle nativo editable en Android Studio.

## Abrir en Android Studio
1. Open
2. Selecciona la carpeta `android/`
3. Sync Gradle

## Nota
Este módulo es nativo Android puro para personalización de plataforma.
Se puede abrir directamente en Android Studio.

## Wrapper
Se incluyen `gradlew`, `gradlew.bat` y `gradle/wrapper/gradle-wrapper.properties`.
Si falta `gradle-wrapper.jar`, Android Studio normalmente lo regenera al sincronizar.
";

            File.WriteAllText(Path.Combine(androidDir, "settings.gradle.kts"), settingsGradle);
            File.WriteAllText(Path.Combine(androidDir, "build.gradle.kts"), rootBuildGradle);
            File.WriteAllText(Path.Combine(androidDir, "gradle.properties"), gradleProps);
            File.WriteAllText(Path.Combine(androidDir, "local.properties"), localProps);
            File.WriteAllText(Path.Combine(wrapperDir, "gradle-wrapper.properties"), gradleWrapperProps);
            File.WriteAllText(Path.Combine(androidDir, "gradlew"), gradlew);
            File.WriteAllText(Path.Combine(androidDir, "gradlew.bat"), gradlewBat);
            File.WriteAllText(Path.Combine(appDir, "build.gradle.kts"), appBuildGradle);
            File.WriteAllText(Path.Combine(appDir, "proguard-rules.pro"), "");
            File.WriteAllText(Path.Combine(appDir, "src", "main", "AndroidManifest.xml"), manifest);
            File.WriteAllText(Path.Combine(kotlinDir, "MainActivity.kt"), kt);
            File.WriteAllText(Path.Combine(resLayoutDir, "activity_main.xml"), layout);
            File.WriteAllText(Path.Combine(resValuesDir, "strings.xml"), strings);
            File.WriteAllText(Path.Combine(resValuesDir, "themes.xml"), themes);
            File.WriteAllText(Path.Combine(androidDir, ".gitignore"), ".gradle/\nlocal.properties\n**/build/\n");
            File.WriteAllText(Path.Combine(androidDir, "README.md"), readme);

            try
            {
                var chmod = Process.Start(new ProcessStartInfo("chmod", $"+x \"{Path.Combine(androidDir, "gradlew")}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                chmod?.WaitForExit();
            }
            catch
            {
                // Ignorar en plataformas sin chmod
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Aviso:[/] No se pudo crear scaffold nativo Android ({ex.Message}).");
        }
    }

    static void EnsureAndroidNativeScaffold(string projectDir, string packageId, string appName)
    {
        var androidDir = Path.Combine(projectDir, "android");
        var manifestPath = Path.Combine(androidDir, "app", "src", "main", "AndroidManifest.xml");
        if (!Directory.Exists(androidDir) || !File.Exists(manifestPath))
        {
            CreateAndroidStudioScaffold(projectDir, packageId, appName);
            AnsiConsole.MarkupLine("[blue]ℹ[/] Se generó la carpeta [yellow]android/[/] nativa para Android Studio.");
        }
    }

    static void EnsureIosProjectWatchCompatibility(string projectDir)
    {
        try
        {
            var csprojPath = Directory.GetFiles(projectDir, "*.csproj").FirstOrDefault();
            if (string.IsNullOrEmpty(csprojPath))
                return;

            var csproj = File.ReadAllText(csprojPath);
            if (!csproj.Contains("Name=\"BootSimulator\"", StringComparison.Ordinal))
                return;

            var cleaned = Regex.Replace(
                csproj,
                @"\s*<Target\s+Name=""BootSimulator""[\s\S]*?</Target>\s*",
                "\n",
                RegexOptions.CultureInvariant);

            if (!string.Equals(cleaned, csproj, StringComparison.Ordinal))
            {
                File.WriteAllText(csprojPath, cleaned);
                AnsiConsole.MarkupLine("[blue]ℹ[/] Se removió [yellow]BootSimulator[/] del .csproj para compatibilidad con Hot Reload en iOS.");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Aviso:[/] No se pudo ajustar compatibilidad de watch en iOS ({ex.Message}).");
        }
    }

    static void EnsurePlatformTargetFrameworkCompatibility(string projectDir)
    {
        try
        {
            var csprojPath = Directory.GetFiles(projectDir, "*.csproj").FirstOrDefault();
            if (string.IsNullOrEmpty(csprojPath))
                return;

            var csproj = File.ReadAllText(csprojPath);
            var updated = csproj
                .Replace("<TargetFrameworks>net9.0</TargetFrameworks>", "<TargetFramework>net9.0</TargetFramework>", StringComparison.Ordinal)
                .Replace("<TargetFrameworks Condition=\"'$(EviPlatform)' == 'ios'\">net9.0-ios</TargetFrameworks>", "<TargetFramework Condition=\"'$(EviPlatform)' == 'ios'\">net9.0-ios</TargetFramework>", StringComparison.Ordinal)
                .Replace("<TargetFrameworks Condition=\"'$(EviPlatform)' == 'android'\">net9.0-android</TargetFrameworks>", "<TargetFramework Condition=\"'$(EviPlatform)' == 'android'\">net9.0-android</TargetFramework>", StringComparison.Ordinal);

            if (!string.Equals(updated, csproj, StringComparison.Ordinal))
            {
                File.WriteAllText(csprojPath, updated);
                AnsiConsole.MarkupLine("[blue]ℹ[/] Se migró [yellow]TargetFrameworks[/] a [yellow]TargetFramework[/] para compatibilidad con dotnet watch en iOS.");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Aviso:[/] No se pudo ajustar TargetFramework para iOS ({ex.Message}).");
        }
    }

    static void TryBootIosSimulator(string udid)
    {
        try
        {
            RunProcessWithOutput("xcrun", $"simctl boot {udid}");
            RunProcessWithOutput("open", "-a Simulator");
        }
        catch
        {
            // Si falla, dotnet/macios intentará igual abrir/usar el simulador.
        }
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

    static void RunProcess(
        string cmd,
        string arguments,
        string workingDir,
        Dictionary<string, string>? environment = null)
    {
        var startInfo = new ProcessStartInfo(cmd, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = false,
            WorkingDirectory = workingDir
        };

        if (environment != null)
        {
            foreach (var kv in environment)
            {
                startInfo.Environment[kv.Key] = kv.Value;
            }
        }

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

    // ─── Config Classes ────────────────────────────────────────────────────────
    public class EviConfig
    {
        public string? Name { get; set; }
        public string? Id { get; set; }
        public AndroidConfig? Android { get; set; }
        public IosConfig? Ios { get; set; }
    }

    public class AndroidConfig
    {
        public string? Sdk { get; set; }
        public string? Package { get; set; }
    }

    public class IosConfig
    {
        public string? BundleId { get; set; }
    }
}
