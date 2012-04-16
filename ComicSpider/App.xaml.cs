using System.Reflection;
using System;
using System.Windows;
using System.IO;
using System.Diagnostics;

namespace ComicSpider
{
	public partial class App : System.Windows.Application
	{
		public App()
		{
			// Get Reference to the current Process
			Process thisProc = Process.GetCurrentProcess();
			// Check how many total processes have the same name as the current one
			if (Process.GetProcessesByName(thisProc.ProcessName).Length > 1)
			{
				// If their is more than one, than it is already running.
				Message_box.Show("Application is already running.");
				return;
			}

			App.Current.ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;

			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
		}

		Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			try
			{
				return Assembly.LoadFrom(@"Lib\" + args.Name.Split(',')[0] + ".dll");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				return null;
			}
		}
	}

}
