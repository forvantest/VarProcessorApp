using System;
using System.Windows.Forms;

// �{���J�f�I
namespace VarProcessor
{
    internal static class Program
    {
        /// <summary>
        /// ���ε{�����D�n�i�J�I�C
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());  // �ҰʥD���
        }
    }
}