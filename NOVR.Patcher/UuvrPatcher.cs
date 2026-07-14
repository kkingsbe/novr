using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;


public class Patcher
{
    private static readonly List<string> PluginsToDeleteBeforePatch =
        new()
        {
            "openvr_api", "openxr_loader", "UnityOpenXR", "ucrtbased.dll", "XRSDKOpenVR"
        };

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll", "Unity.XR.OpenXR.dll" };
    
    public static void Patch(AssemblyDefinition assembly)
    {
        if (assembly.Name.Name == "Unity.XR.OpenXR")
        {
            PatchOpenXrSettings(assembly);
            PatchOpenXrHapticControlLayouts(assembly);
        }
    }

#if MONO
    private static void PatchOpenXrSettings(AssemblyDefinition assembly)
    {
        var openXrSettingsType = assembly.MainModule.GetType("UnityEngine.XR.OpenXR.OpenXRSettings");
        if (openXrSettingsType == null)
        {
            Console.WriteLine("[NOVR.Patcher] Failed to find UnityEngine.XR.OpenXR.OpenXRSettings.");
            return;
        }

        var renderModeField = openXrSettingsType.Fields.FirstOrDefault(field => field.Name == "m_renderMode");
        if (renderModeField == null)
        {
            Console.WriteLine("[NOVR.Patcher] Failed to find OpenXRSettings.m_renderMode.");
            return;
        }

        var applySettingsMethod = openXrSettingsType.Methods.FirstOrDefault(method => method.Name == "ApplySettings");
        if (applySettingsMethod != null)
        {
            ForceMultiPass(applySettingsMethod, renderModeField, openXrSettingsType);
        }

        var awakeMethod = openXrSettingsType.Methods.FirstOrDefault(method => method.Name == "Awake");
        if (awakeMethod != null)
        {
            ForceMultiPass(awakeMethod, renderModeField, openXrSettingsType);
        }

        Console.WriteLine("[NOVR.Patcher] Patched OpenXRSettings to force MultiPass.");
    }

    private static void PatchOpenXrHapticControlLayouts(AssemblyDefinition assembly)
    {
        var patchedCount = 0;

        foreach (var type in AllTypes(assembly.MainModule.Types))
        {
            foreach (var property in type.Properties)
            {
                if (property.PropertyType.FullName != "UnityEngine.XR.OpenXR.Input.HapticControl")
                {
                    continue;
                }

                patchedCount += PatchInputControlHapticLayout(property.CustomAttributes, assembly.MainModule.TypeSystem.String);
            }

            foreach (var field in type.Fields)
            {
                if (field.FieldType.FullName != "UnityEngine.XR.OpenXR.Input.HapticControl")
                {
                    continue;
                }

                patchedCount += PatchInputControlHapticLayout(field.CustomAttributes, assembly.MainModule.TypeSystem.String);
            }
        }

        Console.WriteLine($"[NOVR.Patcher] Patched {patchedCount} OpenXR haptic InputControl layout attribute(s).");
    }

    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;

            foreach (var nestedType in AllTypes(type.NestedTypes))
            {
                yield return nestedType;
            }
        }
    }

    private static int PatchInputControlHapticLayout(
        Mono.Collections.Generic.Collection<CustomAttribute> customAttributes,
        TypeReference stringType)
    {
        var patchedCount = 0;

        foreach (var attribute in customAttributes.Where(attribute =>
                     attribute.AttributeType.FullName == "UnityEngine.InputSystem.Layouts.InputControlAttribute"))
        {
            if (SetStringAttributeProperty(attribute, "layout", "Haptic", stringType))
            {
                patchedCount++;
            }
        }

        return patchedCount;
    }

    private static bool SetStringAttributeProperty(
        CustomAttribute attribute,
        string propertyName,
        string propertyValue,
        TypeReference stringType)
    {
        var argument = new CustomAttributeArgument(stringType, propertyValue);

        for (var propertyIndex = 0; propertyIndex < attribute.Properties.Count; propertyIndex++)
        {
            if (attribute.Properties[propertyIndex].Name != propertyName)
            {
                continue;
            }

            if (attribute.Properties[propertyIndex].Argument.Value as string == propertyValue)
            {
                return false;
            }

            attribute.Properties[propertyIndex] = new Mono.Cecil.CustomAttributeNamedArgument(propertyName, argument);
            return true;
        }

        attribute.Properties.Add(new Mono.Cecil.CustomAttributeNamedArgument(propertyName, argument));
        return true;
    }

    private static void ForceMultiPass(MethodDefinition method, FieldDefinition renderModeField, TypeDefinition openXrSettingsType)
    {
        if (!method.HasBody)
        {
            Console.WriteLine($"[NOVR.Patcher] Skipping {method.FullName} because it has no body.");
            return;
        }

        var singlePassValue = openXrSettingsType.NestedTypes
            .First(type => type.Name == "RenderMode")
            .Fields
            .First(field => field.Name == "MultiPass");

        var il = method.Body.GetILProcessor();
        var firstInstruction = method.Body.Instructions.First();

        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldarg_0));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Ldc_I4, singlePassValue.Constant is int value ? value : 1));
        il.InsertBefore(firstInstruction, il.Create(OpCodes.Stfld, renderModeField));
    }
#endif


    public static void Initialize()
    {
        
        
        
        
        
        Console.WriteLine("Patching NOVR...");
        
        
        
        

        var installerPath = Assembly.GetExecutingAssembly().Location;

        var gameExePath = Process.GetCurrentProcess().MainModule.FileName;

        var gamePath = Path.GetDirectoryName(gameExePath);
        var gameName = Path.GetFileNameWithoutExtension(gameExePath);
        var dataPath = Path.Combine(gamePath, $"{gameName}_Data/");
        var patcherPath = Path.GetDirectoryName(installerPath);
        
        CopyFilesToGame(patcherPath, dataPath);
        

        Console.WriteLine("");
        Console.WriteLine("Installed successfully, probably.");
    }

    private static void CopyFilesToGame(string patcherPath, string dataPath)
    {
        var copyToGameFolderPath = Path.Combine(patcherPath, "CopyToGame");

        Console.WriteLine($"Copying mod files to game... These files get overwritten every time the game starts. If you want to change them manually, replace them in the mod folder instead: {copyToGameFolderPath}");

        CopyDirectory(Path.Combine(copyToGameFolderPath, "Data"), dataPath);

        var gamePluginsPath = Path.Combine(dataPath, "Plugins");
        Directory.CreateDirectory(gamePluginsPath);

        var uuvrPluginsPath = Path.Combine(copyToGameFolderPath, "Plugins");

        DeleteExistingVrPlugins(gamePluginsPath);

        // IntPtr size is 4 on x86, 8 on x64.
        var is64Bit = IntPtr.Size == 8;
        Console.WriteLine($"Detected game as being {(is64Bit ? "x64" : "x86")}");

        // Unity plugins are often in a subfolder of the Plugins folder, but they also get detected from the root folder,
        // so we don't need to worry about the subfolders.
        CopyDirectory(is64Bit ? Path.Combine(uuvrPluginsPath, "x64") : Path.Combine(uuvrPluginsPath, "x86"), gamePluginsPath);
    }

    // There might be leftover stuff from previous UUVR versions, or from other filthy VR mods,
    // and they might be in different subfolders, which could cause conflicts.
    // So we should make sure to nuke them all before replacing with our own.
    private static void DeleteExistingVrPlugins(string gamePluginsPath)
    {
        var pluginPaths = Directory
            .GetFiles(gamePluginsPath, "*.dll", SearchOption.AllDirectories)
            .Where(pluginPath => PluginsToDeleteBeforePatch
                .Select(pluginToDelete => $"{pluginToDelete.ToLower()}.dll")
                .Contains(Path.GetFileName(pluginPath).ToLower()));

        Console.WriteLine($"### Found {pluginPaths.Count()} plugins");

        foreach (var pluginPath in pluginPaths)
        {
            try
            {
                Console.WriteLine($"Deleting plugin `{pluginPath}`");
                File.Delete(pluginPath);
            } catch (Exception exception)
            {
                Console.WriteLine($"Failed to delete plugin before patching. Path: `{pluginPath}`. Exception: `{exception}`");
            }
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        DirectoryInfo dir = new(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        var dirs = dir.GetDirectories();

        Directory.CreateDirectory(destinationDir);

        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(destinationDir, file.Name);
            CopyFileIfNeeded(file, targetFilePath);
        }

        foreach (var subDir in dirs)
        {
            var newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }

        Console.WriteLine($"Copied files from:\n> {sourceDir}\nto:\n> {destinationDir}");
    }

    private static void CopyFileIfNeeded(FileInfo sourceFile, string targetFilePath)
    {
        if (File.Exists(targetFilePath))
        {
            var targetFile = new FileInfo(targetFilePath);
            if (targetFile.Length == sourceFile.Length &&
                targetFile.LastWriteTimeUtc == sourceFile.LastWriteTimeUtc)
            {
                Console.WriteLine($"Skipping unchanged file `{targetFilePath}`");
                return;
            }
        }

        try
        {
            sourceFile.CopyTo(targetFilePath, true);
        }
        catch (IOException exception)
        {
            Console.WriteLine($"Failed to copy `{sourceFile.FullName}` to `{targetFilePath}`. The file may already be loaded by the game; keeping the existing file. Exception: `{exception}`");
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.WriteLine($"Failed to copy `{sourceFile.FullName}` to `{targetFilePath}`. The file may be locked or read-only; keeping the existing file. Exception: `{exception}`");
        }
    }

#if CPP
    public override void Finalizer() { }
#endif
}
