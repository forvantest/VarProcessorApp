using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static VarProcessorApp.Core;

namespace VarProcessorApp
{
    public static class Core
    {
        public static class Configuration
        {
            public const string InputDir = @"D:\VAM\AllPackages\___VarTidied___2";  // 輸入目錄
            public const string OutputDir = @"D:\VAM\output";  // 輸出目錄
            public const string ReassembleDir = @"D:\VAM\Virt A Mate 1.22.0.12\___addonpacksswitch ___\default\___VarsLink___";  // 重新組裝目錄
            public const string VamDir = @"D:\VAM\Virt A Mate 1.22.0.12\";  // VAM 根目錄，用於符號連結
            public const string RealDir = @"D:\VAM\Virt A Mate 1.22.0.12\";  // 同 VamDir
            public const string MainPackagesFile = @"D:\VAM\AllPackages\___VarTidied___2\main_packages.txt";  // 主要包清單
            public const string FileMappingsPath = @"D:\VAM\output\file_mappings.json";  // 映射檔案

            // 從檔案路徑獲取 var 名稱
            public static string GetVarName(string filePath) => Path.GetFileNameWithoutExtension(filePath);

            // 獲取分類（例如 custom, dependencies 等）
            public static string GetCategory(string relPath)
            {
                if (relPath.StartsWith("Custom/")) return "";
                if (relPath.StartsWith("Saves/")) return "saves";
                // 其他分類邏輯...
                return "dependencies";
            }

            // 獲取子相對路徑
            public static string GetSubRelPath(string relPath) => relPath.Replace("\\", "/");  // 統一為 /
        }

        public class FileMapping
        {
            public string MD5 { get; set; }  // MD5 值
            public string OutputPath { get; set; }  // 輸出路徑
            public string OriginalRelPath { get; set; }  // 原始相對路徑
            public int ReferenceCount { get; set; } = 0;  // 引用計數
            public string SourceVar { get; set; }  // 來源 var
        }

        public static class FileMappingManager
        {
            public static Dictionary<string, FileMapping> FileMappings { get; set; } = new Dictionary<string, FileMapping>();  // key: originalRelPath (統一 /)

            // 載入映射
            public static void LoadFileMappings()
            {
                if (File.Exists(Configuration.FileMappingsPath))
                {
                    var json = File.ReadAllText(Configuration.FileMappingsPath, Encoding.UTF8);
                    FileMappings = JsonSerializer.Deserialize<Dictionary<string, FileMapping>>(json) ?? new Dictionary<string, FileMapping>();
                }
            }

            // 保存映射
            public static void SaveFileMappings()
            {
                var json = JsonSerializer.Serialize(FileMappings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Configuration.FileMappingsPath, json, new UTF8Encoding(false));  // 無 BOM
            }

            // 更新映射，處理 MD5 比較和重命名
            public static string UpdateMapping(string sourceVar, string originalRelPath, string tempFilePath, string category, string subRelPath)
            {
                originalRelPath = originalRelPath.Replace("\\", "/");  // 統一路徑
                var key = $"{sourceVar}:{originalRelPath}";  // 唯一 key

                System.Diagnostics.Trace.WriteLine("key: "+key);

                var md5 = ComputeMD5(tempFilePath);
                var outputPath = Path.Combine(Configuration.OutputDir, category, subRelPath).Replace("\\", "/");

                if (File.Exists(outputPath))
                {
                    var existingMD5 = ComputeMD5(outputPath);
                    if (existingMD5 == md5)
                    {
                        File.Delete(tempFilePath);  // 相同，刪除新檔
                        return outputPath;  // 使用現有
                    }
                    else
                    {
                        // 不同，重命名新檔
                        var fileName = Path.GetFileNameWithoutExtension(subRelPath);
                        var ext = Path.GetExtension(subRelPath);
                        var newFileName = $"{fileName}_{md5}{ext}";
                        outputPath = Path.Combine(Configuration.OutputDir, category, Path.GetDirectoryName(subRelPath) ?? "", newFileName).Replace("\\", "/");
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? "");
                File.Move(tempFilePath, outputPath, true);  // 移動並覆蓋

                var mapping = new FileMapping
                {
                    MD5 = md5,
                    OutputPath = outputPath,
                    OriginalRelPath = originalRelPath,
                    SourceVar = sourceVar
                };
                FileMappings[key] = mapping;
                return outputPath;
            }
        }

        public static class JsonProcessor
        {
            // 更新 JSON 路徑，處理 SELF:/ 和 MD5 重命名
           /* public static void UpdateJsonPaths(string jsonPath, Dictionary<string, string> renamedFiles)
            {
                var jsonText = File.ReadAllText(jsonPath, Encoding.UTF8);
                // 假設 renamedFiles 包含 oldName -> newName (with MD5)
                foreach (var kv in renamedFiles)
                {
                    jsonText = jsonText.Replace(kv.Key, kv.Value);  // 替換引用
                }
                File.WriteAllText(jsonPath, jsonText, new UTF8Encoding(false));
            }*/

            // 提取引用，若 JSON 失效用 regex
            public static List<(string depVar, string relPath)> ExtractReferences(string jsonText)
            {
                List<(string, string)> references = new List<(string, string)>();
                bool isJsonUse = true;
                try
                {
                    var jsonDoc = JsonDocument.Parse(jsonText);
                    // 遞迴提取引用（簡化，假設特定結構）
                    Logger.Log($"[DEBUG] JSON 檔案內容（前100字元）：{jsonText.Substring(0, Math.Min(100, jsonText.Length))}");

                    // 示例：提取所有字符串值匹配路徑
                    // 實際需遞迴 JsonElement
                }
                catch (Exception ex)
                {
                    Logger.Log($"[ERROR] JSON 解析失敗，使用正則表達式提取引用： | 錯誤訊息：{ex.Message}");
                    isJsonUse = false;
                }

                if(isJsonUse ==false || references.Count == 0)
                {
                    var matches = Regex.Matches(jsonText, @"(?<depVar>[^:/""]+):/(?<relPath>[^""]+)|SELF:/(?<relPath>[^""]+)|Custom/(?<relPath>[^""]+)");
                    foreach (Match match in matches)
                    {
                        var depVar = match.Groups["depVar"].Value ?? "SELF";
                        var relPath = match.Groups["relPath"].Value.Replace("\\", "/");
                        references.Add((depVar, relPath));
                    }
                }
                return references;
            }
        }

        public static class VarFileProcessor
        {
            // 處理依賴 var (async)
            public static async Task ProcessDependencyVarAsync(string varFile, string varName)
            {
                await Task.Run(() =>
                {
                    string tempZip = Path.ChangeExtension(varFile, ".zip");
                    File.Copy(varFile, tempZip, true);
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);

                    try
                    {
                        ZipFile.ExtractToDirectory(tempZip, tempDir);
                        Logger.Log($"[INFO] 成功解壓檔案：{varFile} | {tempDir}");

                        foreach (var file in Directory.EnumerateFiles(tempDir, "*", SearchOption.AllDirectories))
                        {
                            var relPath = Path.GetRelativePath(tempDir, file).Replace("\\", "/");
                            if (relPath.StartsWith("Saves/scene/") || relPath.EndsWith("meta.json") )
                            {
                                File.Delete(file);  // 跳過並刪除
                                continue;
                            }else if (relPath.EndsWith("diffuse.png"))
                            {
                                System.Diagnostics.Trace.WriteLine("relPath: " + relPath);
                            }
                            var category = Configuration.GetCategory(relPath);
                            var subRelPath = Configuration.GetSubRelPath(relPath);
                            FileMappingManager.UpdateMapping(varName, relPath, file, category, subRelPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[ERROR] 無法解壓檔案：{varFile} | 錯誤訊息：{ex.Message}");
                    }
                    finally
                    {
                        Directory.Delete(tempDir, true);
                        File.Delete(tempZip);
                    }
                });
            }

            // 處理主要 var (async)
            public static async Task ProcessMainVarAsync(string varFile, string varName)
            {
                await Task.Run(() =>
                {
                    string tempZip = Path.ChangeExtension(varFile, ".zip");
                    File.Copy(varFile, tempZip, true);
                    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);

                    try
                    {
                        ZipFile.ExtractToDirectory(tempZip, tempDir);
                        Logger.Log($"[INFO] 成功解壓檔案：{varFile} | {tempDir}");

                        // 第一階段：移動 meta.json 和 Saves/scene/*
                        var rootDir = Path.Combine(Configuration.OutputDir, "Root", varName);
                        Directory.CreateDirectory(rootDir);
                        var sceneDir = Path.Combine(rootDir, "Saves", "scene");
                        Directory.CreateDirectory(sceneDir);

                        foreach (var file in Directory.EnumerateFiles(tempDir, "*", SearchOption.AllDirectories))
                        {
                            var relPath = Path.GetRelativePath(tempDir, file).Replace("\\", "/");
                            if (relPath == "meta.json")
                            {
                                File.Move(file, Path.Combine(rootDir, "meta.json"), true);
                            }
                            else if (relPath.StartsWith("Saves/scene/") || relPath.StartsWith("Saves\\scene\\"))
                            {
                                var targetPath = Path.Combine(sceneDir, Path.GetFileName(file));
                                File.Move(file, targetPath, true);
                            }
                            else
                            {
                                var category = Configuration.GetCategory(relPath);
                                var subRelPath = Configuration.GetSubRelPath(relPath);
                                FileMappingManager.UpdateMapping(varName, relPath, file, category, subRelPath);
                            }
                        }

                        // 額外階段：重命名 .json 和 .jpg 為 varName.*
                        foreach (var file in Directory.EnumerateFiles(sceneDir))
                        {
                            if (file.EndsWith(".json"))
                            {
                                File.Move(file, Path.Combine(sceneDir, $"{varName}.json"), true);
                            }
                            else if (file.EndsWith(".jpg"))
                            {
                                File.Move(file, Path.Combine(sceneDir, $"{varName}.jpg"), true);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[ERROR] 無法解壓檔案：{varFile} | 錯誤訊息：{ex.Message}");
                    }
                    finally
                    {
                        Directory.Delete(tempDir, true);
                        File.Delete(tempZip);
                    }
                });
            }
        }

        public static class VarReassembler
        {
            // 重新組裝單個 var
            public static void ReassembleVar(string varName)
            {
                FileMappingManager.LoadFileMappings();  // 載入現有映射

                var rootDir = Path.Combine(Configuration.OutputDir, "Root", varName);
                if (!Directory.Exists(rootDir)) return;

                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                try
                {
                    // 複製 meta.json
                    File.Copy(Path.Combine(rootDir, "meta.json"), Path.Combine(tempDir, "meta.json"), true);

                    // 複製 Saves/scene/*
                    var sceneDir = Path.Combine(rootDir, "Saves", "scene");
                    var targetSceneDir = Path.Combine(tempDir, "Saves", "scene");
                    Directory.CreateDirectory(targetSceneDir);
                    foreach (var file in Directory.EnumerateFiles(sceneDir))
                    {
                        File.Copy(file, Path.Combine(targetSceneDir, Path.GetFileName(file)), true);
                    }

                    // 解析 JSON 並創建符號連結
                    foreach (var jsonFile in Directory.EnumerateFiles(targetSceneDir, "*.json"))
                    {
                        var jsonText = File.ReadAllText(jsonFile, Encoding.UTF8);
                        var references = JsonProcessor.ExtractReferences(jsonText);

                        foreach (var (depVar, relPath) in references)
                        {
                            var mapping = ComputeMD5FromMapping(FileMappingManager.FileMappings, relPath);
                            if (mapping == null)
                            {
                                Logger.Log($"[ERROR] 找不到引用檔案：{depVar}:/{relPath}");
                                continue;
                            }

                            var outputPath = mapping.OutputPath;
                            if (!File.Exists(outputPath))
                            {
                                Logger.Log($"[ERROR] 引用檔案不存在：{outputPath}");
                                continue;
                            }

                            // 創建符號連結
                            var linkPath = Path.Combine(Configuration.VamDir, "", relPath.Replace("/", "\\"));
                            Directory.CreateDirectory(Path.GetDirectoryName(linkPath) ?? "");
                            try
                            {
                                if (File.Exists(linkPath)) File.Delete(linkPath);
                                File.CreateSymbolicLink(linkPath, outputPath);
                                mapping.ReferenceCount++;
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                Logger.Log($"[ERROR] 符號連結創建失敗：{linkPath} | 錯誤訊息：{ex.Message}。請以管理員身分運行或啟用開發者模式");
                            }

                            // 額外連結：.vmi -> .vmb
                            if (relPath.EndsWith(".vmi"))
                            {
                                var vmbPath = Path.ChangeExtension(linkPath, ".vmb");
                                var vmbOutput = Path.ChangeExtension(outputPath, ".vmb");
                                if (!File.Exists(vmbOutput))
                                {
                                    Logger.Log($"[ERROR] 缺少 .vmb 檔案：{vmbOutput}");
                                    continue;
                                }
                                if (File.Exists(vmbPath)) File.Delete(vmbPath);
                                File.CreateSymbolicLink(vmbPath, vmbOutput);
                            }

                            // .vam -> .vaj 和 .vab (.jpg or .png)
                            if (relPath.EndsWith(".vam"))
                            {
                                var vajPath = Path.ChangeExtension(linkPath, ".vaj");
                                var vajOutput = Path.ChangeExtension(outputPath, ".vaj");
                                if (!File.Exists(vajOutput))
                                {
                                    Logger.Log($"[ERROR] 缺少 .vaj 檔案：{vajOutput}");
                                    continue;
                                }
                                if (File.Exists(vajPath)) File.Delete(vajPath);
                                File.CreateSymbolicLink(vajPath, vajOutput);

                                var vabPath = Path.ChangeExtension(linkPath, ".vab");
                                var vabOutput = Path.ChangeExtension(outputPath, ".vab");
                                if (!File.Exists(vabOutput))
                                {
                                    Logger.Log($"[ERROR] 缺少 .vab 檔案：{vabOutput}");
                                    continue;
                                }
                                if (File.Exists(vabPath)) File.Delete(vabPath);
                                File.CreateSymbolicLink(vabPath, vabOutput);

                                var vabJpg = Path.ChangeExtension(outputPath, ".jpg");
                                var vabPng = Path.ChangeExtension(outputPath, ".png");
                                string jpgOutputPath = File.Exists(vabJpg) ? vabJpg : (File.Exists(vabPng) ? vabPng : null);
                                if (jpgOutputPath == null)
                                {
                                    Logger.Log($"[ERROR] 缺少 檔案（.jpg 或 .png）：{Path.ChangeExtension(outputPath, "")}");
                                    continue;
                                }
                                else
                                {
                                    var jpgPath = Path.ChangeExtension(linkPath, ".jpg");
                                    //var vabPath = Path.ChangeExtension(linkPath, Path.GetExtension(vabOutput));
                                    if (File.Exists(jpgPath)) File.Delete(jpgPath);
                                    File.CreateSymbolicLink(jpgPath, jpgOutputPath);
                                }
                            }
                        }
                    }

                    // 壓縮為 .zip 並重命名為 .var
                    var tempZip = Path.Combine(Configuration.ReassembleDir, $"{varName}.zip");
                    Directory.CreateDirectory(Configuration.ReassembleDir);
                    ZipFile.CreateFromDirectory(tempDir, tempZip);
                    var targetVar = Path.Combine(Configuration.ReassembleDir, $"{varName}.var");
                    File.Move(tempZip, targetVar, true);  // 強行覆蓋
                    Logger.Log($"[INFO] 重新組裝完成：{targetVar}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"[ERROR] 重新組裝失敗：{varName} | 錯誤訊息：{ex.Message}");
                }
                finally
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        public static class Processor
        {
            private static HashSet<string> mainPackages = new HashSet<string>();

            // 載入主要包清單
            public static void LoadMainPackages()
            {
                if (File.Exists(Configuration.MainPackagesFile))
                {
                    mainPackages = new HashSet<string>(File.ReadAllLines(Configuration.MainPackagesFile));
                }
            }

            // 收集 .var 檔案
            public static List<string> CollectVarFiles()
            {
                return Directory.EnumerateFiles(Configuration.InputDir, "*.var", SearchOption.AllDirectories).ToList();
            }

            // 處理所有 var (async)
            public static async Task ProcessVarsAsync()
            {
                LoadMainPackages();
                FileMappingManager.LoadFileMappings();  // 載入現有映射

                // 測試符號連結權限
                TestSymbolicLinkPermission();

                var varFiles = CollectVarFiles();
                // 先處理依賴 .var
                foreach (var varFile in varFiles)
                {
                    string varName = Configuration.GetVarName(varFile);
                    if (!mainPackages.Contains(varName))
                    {
                        await VarFileProcessor.ProcessDependencyVarAsync(varFile, varName);
                    }
                }
                // 再處理主要 .var
                foreach (var varFile in varFiles)
                {
                    string varName = Configuration.GetVarName(varFile);
                    if (mainPackages.Contains(varName))
                    {
                        await VarFileProcessor.ProcessMainVarAsync(varFile, varName);
                    }
                }

                FileMappingManager.SaveFileMappings();  // 保存映射
                Logger.Log("[INFO] 所有 .var 處理完成。");

                // 記錄未使用元件
                foreach (var mapping in FileMappingManager.FileMappings.Values.Where(m => m.ReferenceCount == 0))
                {
                   // Logger.Log($"[INFO] 未使用元件：{mapping.SourceVar}:/{mapping.OriginalRelPath}");
                }
            }

            // 重新組裝所有 var
            public static void ReassembleAll()
            {
                var rootDir = Path.Combine(Configuration.OutputDir, "Root");
                if (!Directory.Exists(rootDir)) return;

                foreach (var varDir in Directory.EnumerateDirectories(rootDir))
                {
                    var varName = Path.GetFileName(varDir);
                    VarReassembler.ReassembleVar(varName);
                }
                FileMappingManager.SaveFileMappings();  // 更新映射
            }

            // 測試符號連結權限
            private static void TestSymbolicLinkPermission()
            {
                string testDir = Path.Combine(Path.GetTempPath(), "test_link");
                string testFile = Path.Combine(testDir, "test.txt");
                string testLink = Path.Combine(testDir, "test_link.txt");

                try
                {
                    Directory.CreateDirectory(testDir);
                    File.WriteAllText(testFile, "test");
                    File.CreateSymbolicLink(testLink, testFile);
                    File.Delete(testLink);
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.Log("[ERROR] 符號連結創建測試失敗。請以管理員身分運行或在 Windows 設定中啟用開發者模式");
                }
                finally
                {
                    Directory.Delete(testDir, true);
                }
            }
        }

        public static class Logger
        {
            private static TextBox _logWindow;

            public static void SetLogWindow(TextBox logWindow)
            {
                _logWindow = logWindow;
            }

            public static void Log(string message)
            {
                Console.WriteLine(message);
                if (_logWindow != null)
                {
                    _logWindow.Invoke((MethodInvoker)delegate
                    {
                        _logWindow.AppendText(message + Environment.NewLine);
                    });
                }
            }
        }

        // 計算 MD5
        private static string ComputeMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }

        // 從映射查找 mapping
        private static FileMapping ComputeMD5FromMapping(Dictionary<string, FileMapping> fileMappings, string relPath)
        {
            relPath = relPath.Replace("\\", "/");
            foreach (var mapping in fileMappings.Values)
            {
                if (mapping.OriginalRelPath == relPath || mapping.OriginalRelPath == relPath.Replace("/", "\\"))
                {
                    return mapping;
                }
            }
            Logger.Log("[ERROR] 缺乏依賴元件檔案: " + Path.Combine(Configuration.OutputDir, relPath));
            return null;
        }
    }
}