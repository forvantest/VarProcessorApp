using System;
using System.Threading.Tasks;
using System.Windows.Forms;

// 主表單，處理 GUI 事件
namespace VarProcessor
{
public partial class MainForm : Form
{
public MainForm()
{
InitializeComponent();
Core.LogAction = AppendLog;  // 設定日誌委託
}

// 開始按鈕事件
private async void btnStart_Click(object sender, EventArgs e)
{
btnStart.Enabled = false;
await Task.Run(() => Core.ProcessVarsAsync());
btnStart.Enabled = true;
}

// 重組按鈕事件
private async void btnReassemble_Click(object sender, EventArgs e)
{
btnReassemble.Enabled = false;
await Task.Run(() => Core.ReassembleVarsAsync());
btnReassemble.Enabled = true;
}

// 清除日誌按鈕事件
private void btnClearLog_Click(object sender, EventArgs e)
{
txtLog.Clear();
}

// 附加日誌（執行緒安全）
private void AppendLog(string message)
{
if (txtLog.InvokeRequired)
{
txtLog.Invoke(new Action(() => txtLog.AppendText(message)));
}
else
{
txtLog.AppendText(message);
}
}
}
}