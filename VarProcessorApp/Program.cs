using System;
using System.Windows.Forms;

namespace VarProcessorApp
{
    internal static class Program
    {
        /// <summary>
        /// 程式的主要入口點。
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