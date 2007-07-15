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
		public Form1() 
		{ 
			InitializeComponent();
			Text = "IJW Profiler 0.2.7";
			ImageList images = new ImageList();
			images.TransparentColor = Color.Fuchsia;
			images.Images.Add("collapsed", Image.FromFile("res/collapsed.bmp"));
			images.Images.Add("expanded", Image.FromFile("res/expanded.bmp"));
			images.Images.Add("thread", Image.FromFile("res/thread.bmp"));
			images.Images.Add("prop_set", Image.FromFile("res/prop_set.bmp"));
			images.Images.Add("prop_get", Image.FromFile("res/prop_get.bmp"));
			images.Images.Add("method", Image.FromFile("res/method.bmp"));
			tabControl1.ImageList = images;
		}

		Run ProfileProcess(string processName)
		{
			using (new ComServerRegistration("pcomimpl.dll"))
			{
				Run run = new Run();

				ProcessStartInfo info = new ProcessStartInfo(processName);
				info.WorkingDirectory = Path.GetDirectoryName(processName);
				info.UseShellExecute = false;
				info.EnvironmentVariables["Cor_Enable_Profiling"] = "1";
				info.EnvironmentVariables["COR_PROFILER"] = "{C1E9FE1F-F517-45c0-BB0E-EFAECC9401FC}";

				info.EnvironmentVariables["ijwprof_txt"] = run.txtFile;
				info.EnvironmentVariables["ijwprof_bin"] = run.binFile;

				Process.Start(info).WaitForExit();

				return run;
			}
		}

		CallTreeView CreateNewView(string name, Monkey node)
		{
			TabPage page = new TabPage(name);
			if (node != null)
				page.ImageKey = node.Key;
			CallTreeView view = new CallTreeView( GetFunctionFilter() );
			page.Controls.Add(view);
			page.Tag = view;
			view.Dock = DockStyle.Fill;

			tabControl1.Controls.Add(page);
			tabControl1.SelectedTab = page;
			return view;
		}

		void NewRun(object sender, EventArgs e)
		{
			OpenFileDialog d = new OpenFileDialog();
			d.Filter = "Application|*.exe";
			d.RestoreDirectory = true;

			if (DialogResult.OK != d.ShowDialog())
				return;

			Run run = ProfileProcess(d.FileName);

			LoadTraceData(run);
		}

		void LoadTraceData( Run run )
		{
			using (run)
			{
				FunctionNameProvider names = new FunctionNameProvider(run.txtFile);

				string baseText = Text;

				Action<float> progressCallback = delegate(float frac)
				{
					Text = baseText + " - Slurping " + frac.ToString("P0");
				};

				CallTree tree = new CallTree(run.binFile, names, progressCallback);
				CallTreeView view = CreateNewView(run.name, null);

				Text = baseText + " - Preparing view...";

				view.Nodes.Clear();
				foreach (Thread thread in tree.threads.Values)
					view.Nodes.Add(thread.CreateView(thread.TotalTime));

				Text = baseText;
			}
		}

		Predicate<Function> GetFunctionFilter()
		{
			return new FunctionFilter("System.", "Microsoft.").Evaluate;
		}

		void OnCloseClicked(object sender, EventArgs e) { Close(); }

		TreeNode GetSelectedNode()
		{
			if (tabControl1.SelectedTab == null)
				return null;

			CallTreeView view = tabControl1.SelectedTab.Tag as CallTreeView;
			if (view == null)
				return null;

			return view.SelectedNode;
		}

		void OnOpenInNewTab(object sender, EventArgs e)
		{
			TreeNode n = GetSelectedNode();
			if (n == null)
				return;

			IProfilerElement t = n.Tag as IProfilerElement;

			CallTreeView v = CreateNewView(t.TabTitle, n as Monkey);
			TreeNode n2 = t.CreateView(t.TotalTime);
			v.Nodes.Add(n2);
			v.SelectedNode = n2;
			v.Focus();
		}
	}
}
