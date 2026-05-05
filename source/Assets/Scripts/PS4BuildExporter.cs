#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Build.Reporting;

public class PS4BuildExporter
{
    private static string BUILD_PATH = "Builds/PS4";
    private static string EXECUTABLE_NAME = "FPKGi";

    [MenuItem("FPKGi/Build/Export to PS4 Package")]
    public static void ExportPS4Package()
    {
        Debug.Log("[PS4 Exporter] بدء تصدير المشروع للـ PS4...");

        // التحقق من مجلد البناء
        if (!Directory.Exists(BUILD_PATH))
        {
            Directory.CreateDirectory(BUILD_PATH);
            Debug.Log($"[PS4 Exporter] تم إنشاء مجلد البناء: {BUILD_PATH}");
        }

        // قائمة المشاهد المراد تضمينها
        string[] scenes = GetScenesForBuild();

        // إعدادات البناء
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(BUILD_PATH, EXECUTABLE_NAME + ".elf"),
            target = BuildTarget.PS4,
            options = BuildOptions.Development
        };

        // البناء
        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[PS4 Exporter] ✓ تم البناء بنجاح!");
            Debug.Log($"[PS4 Exporter] الملف: {buildOptions.locationPathName}");
            Debug.Log($"[PS4 Exporter] الحجم: {report.summary.totalSize / (1024f * 1024f):F2} MB");

            // إنشاء ملفات إضافية
            CreatePS4Metadata();
            CreatePackageManifest();
            
            EditorUtility.RevealInFinder(BUILD_PATH);
        }
        else
        {
            Debug.LogError("[PS4 Exporter] ✗ فشل البناء!");
            Debug.LogError($"[PS4 Exporter] الأخطاء: {report.summary.totalErrors}");
        }
    }

    [MenuItem("FPKGi/Build/Build for Windows (Test)")]
    public static void BuildWindows()
    {
        Debug.Log("[Builder] بدء بناء نسخة Windows للاختبار...");

        string[] scenes = GetScenesForBuild();

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine("Builds/Windows", "FPKGi.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log("[Builder] ✓ تم البناء بنجاح!");
            EditorUtility.RevealInFinder("Builds/Windows");
        }
        else
        {
            Debug.LogError("[Builder] ✗ فشل البناء!");
        }
    }

    private static string[] GetScenesForBuild()
    {
        List<string> scenePaths = new List<string>();

        // البحث عن المشاهد
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("Scene") || path.Contains("Main"))
            {
                scenePaths.Add(path);
                Debug.Log($"[Builder] إضافة مشهد: {path}");
            }
        }

        if (scenePaths.Count == 0)
        {
            Debug.LogWarning("[Builder] لم يتم العثور على مشاهد، سيتم البناء بدون مشاهد");
        }

        return scenePaths.ToArray();
    }

    private static void CreatePS4Metadata()
    {
        string metadataPath = Path.Combine(BUILD_PATH, "ps4_metadata.json");

        string json = @"{
  ""app_info"": {
    ""title"": ""FPKGi v2.0.0"",
    ""title_id"": ""FPKG00001"",
    ""version"": ""02.00"",
    ""publisher"": ""FPKGi Dev"",
    ""category"": ""Game"",
    ""content_rating"": ""MATURE""
  },
  ""system_requirements"": {
    ""min_firmware"": ""5.05"",
    ""required_ram"": ""4096"",
    ""required_storage"": ""2048""
  },
  ""features"": [
    ""Modern UI"",
    ""Search & Filter"",
    ""Download Manager"",
    ""JSON-based Content""
  ],
  ""build_date"": """ + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"""
}";

        File.WriteAllText(metadataPath, json);
        Debug.Log($"[PS4 Exporter] تم إنشاء ملف البيانات الوصفية: {metadataPath}");
    }

    private static void CreatePackageManifest()
    {
        string manifestPath = Path.Combine(BUILD_PATH, "MANIFEST.txt");

        string manifest = @"FPKGi v2.0.0 - PS4 Package Manifest
=====================================

Application: FPKGi
Title ID: FPKG00001
Version: 2.0.0
Platform: PS4
Build Date: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"

Contents:
---------
- eboot.elf (Main Executable)
- ps4_metadata.json (Application Metadata)
- Assets/ (Game Resources & Data)

Installation Instructions:
==========================
1. Copy this entire folder to USB drive
2. Connect USB to PS4 (must be Jailbroken/Modified)
3. Install via Package Manager
4. Launch FPKGi from Applications

Minimum Requirements:
====================
- Firmware: 5.05 or higher
- Storage: 2GB free space
- RAM: 4GB available
- Internet Connection (recommended)

Support:
========
For issues or questions, visit the FPKGi project page.

License: Freeware
";

        File.WriteAllText(manifestPath, manifest);
        Debug.Log($"[PS4 Exporter] تم إنشاء ملف البيان: {manifestPath}");
    }

    [MenuItem("FPKGi/Help/Show Build Instructions")]
    public static void ShowInstructions()
    {
        string instructions = @"
=== FPKGi PS4 Package Building Guide ===

QUICK START:
============
1. Click 'Build > Export to PS4 Package'
2. Wait for build to complete
3. Files will be in 'Builds/PS4' folder

INSTALLATION ON JAILBROKEN PS4:
================================
1. Copy entire 'Builds/PS4' folder to USB drive
2. On PS4, go to: Settings > System Software > Install Package Files
3. Select the folder from USB
4. Wait for installation to complete
5. Launch from Applications > FPKGi

TESTING ON WINDOWS:
====================
1. Click 'Build > Build for Windows (Test)'
2. Run FPKGi.exe from Builds/Windows
3. Test UI and functionality

REQUIREMENTS:
==============
- PS4 with Jailbreak/Modified Firmware (5.05+)
- USB Drive (formatted as exFAT or FAT32)
- ~2GB free space on PS4

TROUBLESHOOTING:
=================
Q: Build fails?
A: Check if PS4 Build Support module is installed in Unity

Q: Installation fails on PS4?
A: Ensure PS4 is properly jailbroken (5.05+ firmware)

Q: App crashes on launch?
A: Check ps4_metadata.json for correct app settings

For more help, visit: https://github.com/FPKGi/FPKGi
";

        EditorUtility.DisplayDialog("FPKGi Build Instructions", instructions, "OK");
        Debug.Log(instructions);
    }
}
#endif
