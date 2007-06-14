using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace ProfilerUi
{
	public partial class Form1 : Form
	{
		public Form1() { InitializeComponent(); }

		void ProfileProcess(string processName)
		{
			using (new ComServerRegistration("pcomimpl.dll"))
			{
				ProcessStartInfo info = new ProcessStartInfo(processName);
				info.WorkingDirectory = Path.GetDirectoryName(processName);
				info.UseShellExecute = false;
				info.EnvironmentVariables["Cor_Enable_Profiling"] = "1";
				info.EnvironmentVariables["COR_PROFILER"] = "{C1E9FE1F-F517-45c0-BB0E-EFAECC9401FC}";

				Process process = Process.Start(info);

				while (!process.WaitForExit(50))
					Application.DoEvents();
			}
		}

		void DoIt(object sender, EventArgs e)
		{
			OpenFileDialog d = new OpenFileDialog();
			d.Filter = "Application|*.exe";
			d.RestoreDirectory = true;

			if (DialogResult.OK != d.ShowDialog())
				return;

			ProfileProcess(d.FileName);

			FunctionNameProvider names = new FunctionNameProvider("c:\\profile.txt");

			CallTree tree = new CallTree("c:\\profile.bin", names);
			foreach (Thread thread in tree.threads.Values)
				callView.Nodes.Add(thread.CreateView());
		}

		Brush selected = new SolidBrush(Color.FromArgb(0xee, 0xee, 0xff));

		void OnDrawNode(object sender, DrawTreeNodeEventArgs e)
		{
			if (e.Node == callView.SelectedNode)
				e.Graphics.FillRectangle(selected, e.Bounds);

			e.DrawDefault = true;
		}
	}
}