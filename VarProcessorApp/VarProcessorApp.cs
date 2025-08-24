using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// 核心邏輯類別，處理 .var 檔案的解壓、分類、重組等
namespace VarProcessor
{
    public static class Core
    {
        // 常數定義
        public const string InputDir = @"D:\VAM\AllPackages\___VarTidied___2";  // 輸入目錄
        public const string MainPackagesFile = @"D:\VAM\AllPackages\___VarTidied___2\main_packages.txt";  // 主要套件清單
        public const string OutputDir = @"D:\VAM\output";  // 輸出目錄
        public const string RealDir = @"D:\VAM\Virt A Mate 1.22.0.12\";
        public const string ReassembleOutputDir = @"D:\VAM\Virt A Mate 1.22.0.12\___addonpacksswitch ___\default\___VarsLink___";  // 重組輸出目錄
        public const string FileMappingsPath = OutputDir+ @"file_mappings.json";  // 檔案映射檔

        private static HashSet<string> mainPackages = new HashSet<string>();  // 主要套件集合
        private static Dictionary<string, FileMapping> fileMappings = new Dictionary<string, FileMapping>();  // 檔案映射字典，key: MD5

        // 日誌委託
        public static Action<string> LogAction { get; set; }

        // 載入主要套件清單
        public static void LoadMainPackages()
        {
            if (File.Exists(MainPackagesFile))
            {
                mainPackages = new HashSet<string>(File.ReadAllLines(MainPackagesFile));
                Log("[INFO] 已載入主要套件清單。");
            }
            else
            {
                Log("[ERROR] 主要套件清單檔案不存在: " + MainPackagesFile);
            }
        }

        // 收集 .var 檔案
        public static List<string> CollectVarFiles()
        {
            return new List<string>(Directory.EnumerateFiles(InputDir, "*.var", SearchOption.AllDirectories));
        }

        // 處理所有 .var 檔案
        public static async Task ProcessVarsAsync()
        {
            LoadMainPackages();
            LoadFileMappings();  // 載入現有映射

            var varFiles = CollectVarFiles();
            // 先處理依賴 .var
            foreach (var varFile in varFiles)
            {
                string varName = Path.GetFileNameWithoutExtension(varFile);
                if (!mainPackages.Contains(varName))
                {
                    await ProcessDependencyVarAsync(varFile, varName);
                }
            }
            // 再處理主要 .var
            foreach (var varFile in varFiles)
            {
                string varName = Path.GetFileNameWithoutExtension(varFile);
                if (mainPackages.Contains(varName))
                {
                    await ProcessMainVarAsync(varFile, varName);
                }
            }

            SaveFileMappings();  // 保存映射
            Log("[INFO] 所有 .var 處理完成。");
        }

        // 處理依賴 .var
        private static async Task ProcessDependencyVarAsync(string varFile, string varName)
        {
            string tempZip = Path.ChangeExtension(varFile, ".zip");
            File.Copy(varFile, tempZip, true);
            string extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(tempZip, extractDir);
            File.Delete(tempZip);

            // 處理檔案分類，跳過特定檔案
            foreach (var file in Directory.EnumerateFiles(extractDir, "*.*", SearchOption.AllDirectories))
            {
                string relPath = file.Substring(extractDir.Length + 1).Replace("\\", "/");  // 統一路徑為 /
                if (relPath.StartsWith("meta.json") || relPath.StartsWith("Saves/scene/") && (relPath.EndsWith(".json") || relPath.EndsWith(".jpg")))
                {
                    File.Delete(file);  // 刪除跳過檔案
                    continue;
                }

                string md5 = ComputeMD5(file);
                string category = GetCategory(relPath);
                string outputPath = Path.Combine(OutputDir, category, relPath.Replace("/", "\\"));  // 處理 \\ 或 /
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                // 處理重複
                if (File.Exists(outputPath))
                {
                    string existingMd5 = ComputeMD5(outputPath);
                    if (md5 == existingMd5)
                    {
                        File.Delete(file);
                        Log("[INFO] 刪除重複檔案: " + relPath);
                        continue;
                    }
                    else
                    {
                        outputPath = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + "_" + md5 + Path.GetExtension(outputPath));
                    }
                }

                File.Move(file, outputPath);
                UpdateFileMapping(md5, outputPath, relPath, varName);
                Log("[INFO] 移動檔案: " + file + " | " + outputPath);
            }

            // 移動 .var 到 dependencies
            string depVarDir = Path.Combine(OutputDir, "dependencies", varName);
            Directory.CreateDirectory(depVarDir);
            //File.Move(varFile, Path.Combine(depVarDir, Path.GetFileName(varFile)));

            Directory.Delete(extractDir, true);
        }

        // 處理主要 .var
        private static async Task ProcessMainVarAsync(string varFile, string varName)
        {
            string tempZip = Path.ChangeExtension(varFile, ".zip");
            File.Copy(varFile, tempZip, true);
            string extractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            ZipFile.ExtractToDirectory(tempZip, extractDir);
            File.Delete(tempZip);

            string rootVarDir = Path.Combine(OutputDir, "Root", varName);
            Directory.CreateDirectory(rootVarDir);

            // 處理特定檔案
            foreach (var file in Directory.EnumerateFiles(extractDir, "*.*", SearchOption.AllDirectories))
            {
                string relPath = file.Substring(extractDir.Length + 1).Replace("\\", "/");
                string outputPath;

                if (relPath == "meta.json")
                {
                    outputPath = Path.Combine(rootVarDir, "meta.json");
                }
                else if (relPath.StartsWith("Saves/scene/") && relPath.EndsWith(".json"))
                {
                    outputPath = Path.Combine(rootVarDir, "Saves", "scene", varName + ".json");
                    UpdateJsonPaths(file);  // 更新 JSON 路徑
                }
                else if (relPath.StartsWith("Saves/scene/") && relPath.EndsWith(".jpg"))
                {
                    outputPath = Path.Combine(rootVarDir, "Saves", "scene", varName + ".jpg");
                }
                else
                {
                    string category = GetCategory(relPath);
                    outputPath = Path.Combine(OutputDir, category, relPath.Replace("/", "\\"));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                // 重複處理
                string md5 = ComputeMD5(file);
                if (File.Exists(outputPath))
                {
                    string existingMd5 = ComputeMD5(outputPath);
                    if (md5 == existingMd5)
                    {
                        File.Delete(file);
                        continue;
                    }
                    else
                    {
                        outputPath = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + "_" + md5 + Path.GetExtension(outputPath));
                    }
                }

                File.Move(file, outputPath);
                UpdateFileMapping(md5, outputPath, relPath, varName);
                Log("[INFO] 移動檔案: " + file + " | " + outputPath);
            }

            Directory.Delete(extractDir, true);
        }

        // 更新 JSON 路徑
        private static void UpdateJsonPaths(string jsonFile)
        {
            string jsonText = File.ReadAllText(jsonFile, Encoding.UTF8);
            try
            {
                // 嘗試解析 JSON
                var jsonDoc = JsonDocument.Parse(jsonText);
                // 遞迴替換 SELF:/
                jsonText = ReplaceSelfPaths(jsonDoc.RootElement).ToString();
            }
            catch
            {
                // 無效 JSON，使用正則備援
                jsonText = Regex.Replace(jsonText, @"SELF:/", "", RegexOptions.IgnoreCase);
                jsonText = Regex.Replace(jsonText, @"SELF:\\", "", RegexOptions.IgnoreCase);  // 考慮 \\ 路徑
            }
            File.WriteAllText(jsonFile, jsonText, new UTF8Encoding(false));  // 無 BOM
        }

        // 遞迴替換 JSON 中的 SELF:/ (簡化實現，需根據實際 JSON 結構調整)
        private static JsonElement ReplaceSelfPaths(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var obj = new Dictionary<string, JsonElement>();
                foreach (var prop in element.EnumerateObject())
                {
                    string value = prop.Value.GetString();
                    if (value != null && (value.StartsWith("SELF:/") || value.StartsWith("SELF:\\")))
                    {
                        value = value.Replace("SELF:/", "").Replace("SELF:\\", "");
                        obj[prop.Name] = JsonDocument.Parse($"\"{value}\"").RootElement;
                    }
                    else
                    {
                        obj[prop.Name] = ReplaceSelfPaths(prop.Value);
                    }
                }
                return JsonDocument.Parse(JsonSerializer.Serialize(obj)).RootElement;
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                var array = new List<JsonElement>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(ReplaceSelfPaths(item));
                }
                return JsonDocument.Parse(JsonSerializer.Serialize(array)).RootElement;
            }
            return element;
        }

        // 重組 .var
        public static async Task ReassembleVarsAsync()
        {
            LoadFileMappings();

            var rootDirs = Directory.GetDirectories(Path.Combine(OutputDir, "Root"));
            foreach (var rootDir in rootDirs)
            {
                string varName = Path.GetFileName(rootDir);
                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                // 複製 meta.json, Saves/scene/*.json, *.jpg
                string metaPath = Path.Combine(rootDir, "meta.json");
                if (File.Exists(metaPath)) File.Copy(metaPath, Path.Combine(tempDir, "meta.json"));

                string sceneDir = Path.Combine(rootDir, "Saves", "scene");
                if (Directory.Exists(sceneDir))
                {
                    string jsonPath = Path.Combine(sceneDir, varName + ".json");
                    string jpgPath = Path.Combine(sceneDir, varName + ".jpg");
                    if (File.Exists(jsonPath))
                    {
                        var vv1 = Path.Combine(tempDir, "Saves\\scene\\" );
                        Directory.CreateDirectory(vv1);
                        File.Copy(jsonPath,vv1 + varName + ".json");  // 考慮 \\
                    }
                    if (File.Exists(jpgPath))
                    {
                        var vv2 = Path.Combine(tempDir, "Saves\\scene\\");
                        Directory.CreateDirectory(vv2);
                        File.Copy(jpgPath, vv2 + varName + ".jpg");
                    }
                }

                // 從 JSON 提取依賴並建立符號連結
                try
                {
                    string jsonText = File.ReadAllText(Path.Combine(tempDir, "Saves\\scene\\" + varName + ".json"));
                    // 提取依賴路徑 (簡化，使用正則)
                    var matches = Regex.Matches(jsonText, @"(?<depVar>[^:/""]+):/(?<relPath>[^""]+)|Custom/(?<relPath>[^""]+)");
                    foreach (Match match in matches)
                    {
                        string depVar = match.Groups["depVar"].Value;
                        string relPath = match.Groups["relPath"].Value;//.Replace("/", "\\");  // 處理路徑
                        if(String.IsNullOrEmpty(depVar))
                            relPath= match.Groups["0"].Value;
                        FileMapping mapping = ComputeMD5FromMapping(fileMappings, relPath);
                        if (mapping!=null)  // 假設 MD5 從 mapping 取得
                        {
                            string linkPath = Path.Combine(RealDir, relPath);
                            Directory.CreateDirectory(Path.GetDirectoryName(linkPath));
                            if (File.Exists(linkPath)) File.Delete(linkPath);
                            File.CreateSymbolicLink(linkPath, mapping.OutputPath);
                            mapping.ReferenceCount++;
                            Log("[INFO] 建立符號連結: " + linkPath + " -> " + mapping.OutputPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("[ERROR] 建立符號連結失敗: " + ex.Message + " 請以管理員執行。");
                }

                // 壓縮為 .var
                string tempZip = Path.Combine(ReassembleOutputDir, varName + ".zip");
                if (File.Exists(tempZip)) File.Delete(tempZip);
                ZipFile.CreateFromDirectory(tempDir, tempZip);
                string varPath = Path.ChangeExtension(tempZip, ".var");
                if (File.Exists(varPath)) File.Delete(varPath);  // 覆寫
                File.Move(tempZip, varPath);

                Directory.Delete(tempDir, true);
            }

            // 記錄未使用組件
            foreach (var mapping in fileMappings.Values)
            {
                if (mapping.ReferenceCount == 0)
                {
                  //  Log("[DEBUG] 未使用組件: " + mapping.OutputPath);
                }
            }

            SaveFileMappings();
            Log("[INFO] .var 重組完成。");
        }

        // 計算 MD5
        private static string ComputeMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }
        }

        // 假設從 mapping 取得 MD5 (需根據實際調整)
        private static FileMapping ComputeMD5FromMapping(Dictionary<string, FileMapping> fileMappings, string relPath)
        {
            // 簡化實現，返回假 MD5 或實際計算
            foreach (var mapping in fileMappings.Values) {
                if(mapping.OriginalRelPath==relPath)
                    { return mapping; }
                if (mapping.OriginalRelPath == relPath.Replace("\\","/") )
                    { return mapping; }
            }
            Log("[ERROR] 缺乏依賴元件檔案: " + OutputDir + "/" + relPath);
            return null;
        }

        // 取得分類
        private static string GetCategory(string relPath)
        {
            if (relPath.StartsWith("Custom")) return "";
            if (relPath.StartsWith("dependencies/models/")) return "dependencies/models";
            return "other";  // 預設
        }

        // 更新檔案映射
        private static void UpdateFileMapping(string md5, string outputPath, string originalRelPath, string sourceVar)
        {
            if (!fileMappings.ContainsKey(md5))
            {
                fileMappings[md5] = new FileMapping
                {
                    MD5 = md5,
                    OutputPath = outputPath,
                    OriginalRelPath = originalRelPath,
                    ReferenceCount = 0,
                    SourceVar = sourceVar
                };
            }
        }

        // 載入檔案映射
        private static void LoadFileMappings()
        {
            if (File.Exists(FileMappingsPath))
            {
                string json = File.ReadAllText(FileMappingsPath);
                if (String.IsNullOrEmpty(json))
                    return;
                fileMappings = JsonSerializer.Deserialize<Dictionary<string, FileMapping>>(json) ?? new Dictionary<string, FileMapping>();
            }
        }

        // 保存檔案映射
        private static void SaveFileMappings()
        {
            string json = JsonSerializer.Serialize(fileMappings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FileMappingsPath, json, new UTF8Encoding(false));
        }

        // 日誌記錄
        public static void Log(string message)
        {
            Console.WriteLine(message);
            LogAction?.Invoke(message + Environment.NewLine);
        }
    }

    // 檔案映射類別
    public class FileMapping
    {
        public string MD5 { get; set; }
        public string OutputPath { get; set; }
        public string OriginalRelPath { get; set; }
        public int ReferenceCount { get; set; }
        public string SourceVar { get; set; }
    }
}