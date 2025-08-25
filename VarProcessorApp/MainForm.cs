using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VarProcessorApp
{
public partial class MainForm : Form
{
public MainForm()
{
InitializeComponent();
Core.Logger.SetLogWindow(logWindow);  // 設定日誌輸出到 TextBox
}

// 開始按鈕事件：觸發處理 .var 檔案
private async void startButton_Click(object sender, EventArgs e)
{
Core.Logger.Log("[INFO] 開始處理 .var 檔案...");
await Task.Run(async () => await Core.Processor.ProcessVarsAsync());
}

// 重新組裝按鈕事件：觸發重新組裝 .var 檔案
private async void reassembleButton_Click(object sender, EventArgs e)
{
Core.Logger.Log("[INFO] 開始重新組裝 .var 檔案...");
await Task.Run(() => Core.Processor.ReassembleAll());
}

// 清除日誌按鈕事件：清除 TextBox 內容
private void clearLogButton_Click(object sender, EventArgs e)
{
logWindow.Clear();
Core.Logger.Log("[INFO] 日誌已清除");
}
}
}