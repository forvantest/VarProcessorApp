using System;
using System.Windows.Forms;

namespace VarProcessorApp
{
    internal static class Program
    {
        /// <summary>
        /// �{�����D�n�J�f�I�C
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}