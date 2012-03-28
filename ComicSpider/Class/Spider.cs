using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using ComicSpider;
using ComicSpider.App_dataTableAdapters;

namespace ys.Web
{
	class Comic_spider
	{
		public Comic_spider()
		{
			stopped = true;
			file_info_queue = new Queue<Web_src_info>();
			vol_info_queue = new Queue<Web_src_info>();
			file_info_queue_lock = new object();
			vol_info_queue_lock = new object();
			thread_list = new List<Thread>();
		}

		public void Async_show_vol_list()
		{
			Thread thread = new Thread(new ParameterizedThreadStart(Show_vol_list));
			thread.Name = "Show_vol_list";
			thread.Start(MainWindow.Main.Settings.Main_url);

			Report("Show vol list...");
		}
		public void Async_start(System.Windows.Controls.ItemCollection vol_info_list)
		{
			stopped = false;

			foreach (Web_src_info vol_info in vol_info_list)
			{
				vol_info.Counter.Reset();
				lock (vol_info_queue_lock)
				{
					vol_info_queue.Enqueue(vol_info); 
				}
			}

			for (int i = 0; i < int.Parse(MainWindow.Main.Settings.Thread_count); i++)
			{
				Thread downloader = new Thread(new ThreadStart(Downloader));
				downloader.Name = "Downloader" + i;
				downloader.Start();
				thread_list.Add(downloader);

				Thread info_getter = new Thread(new ThreadStart(Get_page_info_list));
				info_getter.Name = "Get_page_info_list" + i;
				info_getter.Start();
				thread_list.Add(info_getter);
			}

			Report("Comic Spider start...");
		}

		public void Stop()
		{
			stopped = true;
			vol_info_queue.Clear();
			file_info_queue.Clear();

			foreach (Thread thread in thread_list)
			{
				thread.Abort();
			}
			thread_list.Clear();
		}
		public bool Stopped { get { return stopped; } }

		private bool stopped;
		private Queue<Web_src_info> file_info_queue;
		private Queue<Web_src_info> vol_info_queue;
		private object file_info_queue_lock;
		private object vol_info_queue_lock;
		private List<Thread> thread_list;

		private void Show_vol_list(object arg)
		{
			string url = arg as string;
			List<Web_src_info> vol_info_list = Get_vol_info_list(new Web_src_info(url, 0, "", null, "", "", null));

			MainWindow.Main.Dispatcher.Invoke(
				new MainWindow.Show_vol_list_delegate(MainWindow.Main.Show_vol_list),
				vol_info_list);
		}
		private void Log_error(Exception ex, string url = "")
		{
			Report(ex.Message);

			try
			{
				Error_logTableAdapter a = new Error_logTableAdapter();
				a.Connection.Open();
				a.Insert(
					DateTime.Now,
					url,
					ex.Message,
					ex.StackTrace);
				a.Connection.Close();
			}
			catch (Exception)
			{
			}
		}
		private void Report(string info)
		{
			Console.WriteLine(info);
			MainWindow.Main.Dispatcher.Invoke(new MainWindow.Report_progress_delegate(MainWindow.Main.Report_progress), info);
		}
		private void Report(string format, params object[] arg)
		{
			string info = string.Format(format, arg);
			Console.WriteLine(info);
			MainWindow.Main.Dispatcher.Invoke(new MainWindow.Report_progress_delegate(MainWindow.Main.Report_progress), info);
		}

		private List<Web_src_info> Get_vol_info_list(Web_src_info comic_info)
		{
			List<Web_src_info> vol_info_list = new List<Web_src_info>();

			vol_info_list = Get_info_list_from_html(
				comic_info,
				@"<a(.|\n)+?href=""(?<url>.+?)""(.|\n)+?>(?<name>(.|\n)+?)</a>",
				"{0}",
				MainWindow.Main.Settings.Page_url
			);

			return vol_info_list;
		}

		private void Get_page_info_list()
		{
			while (!stopped)
			{
				Web_src_info vol_info;
				lock (vol_info_queue_lock)
				{
					if (vol_info_queue.Count == 0)
					{
						return;
					}
					vol_info = vol_info_queue.Dequeue();
				}

				if (vol_info.Children == null ||
					vol_info.Children.Count == 0)
				{
					try
					{
						vol_info.Children = Get_info_list_from_html(
								vol_info,
								@"<select[^<>]+change_page(.|\n)+?</select>		value=""(?<url>[1-9]\d?\d?)""",
								vol_info.Url.Remove(vol_info.Url.LastIndexOf('/') + 1) + "{0}" + ".html");
					}
					catch (Exception ex)
					{
						Log_error(ex, vol_info.Url);
						continue;
					}
				}

				Report("Page list: {0}", vol_info.Name);

				Get_file_info_list(vol_info.Children);
			}
		}
		private void Get_file_info_list(List<Web_src_info> page_info_list)
		{
			string dir_path = "";

			if (page_info_list.Count == 0)
			{
				Report("No page found.");
				return;
			}

			Web_src_info parent = page_info_list[0];
			while ((parent = parent.Parent) != null)
			{
				dir_path = Path.Combine(parent.Name, dir_path);
			}
			dir_path = Path.Combine(MainWindow.Main.Settings.Root_dir, dir_path);
			if (!Directory.Exists(dir_path))
			{
				Directory.CreateDirectory(dir_path);
				Report("Create dir: {0}", dir_path);
			}


			for (int i = 0; i < page_info_list.Count; i++)
			{
				if (stopped) return;

				Web_src_info page_info = page_info_list[i];

				if (page_info.State == "OK")
				{
					int downloaded = page_info.Parent.Counter.Increment();
					if (downloaded == page_info.Parent.Counter.All)
					{
						page_info.Parent.State = "OK";
					}
					continue;
				}

				try
				{
					List<Web_src_info> file_info_list = Get_info_list_from_html(
								page_info,
								@"src=""(?<url>.+?((jpg)|(png)|(gif)|(bmp)))""");

					lock (file_info_queue_lock)
					{
						file_info_queue.Enqueue(file_info_list[0]);
					}
					Report("Get file info: {0}", file_info_list[0].Url);
				}
				catch (Exception ex)
				{
					if (ex is ThreadAbortException) return;
					page_info.State = "X";
					Log_error(ex, page_info.Url);
				}
			}
		}

		private void Downloader()
		{
			Web_src_info file_info;

			while (!stopped)
			{
				lock (file_info_queue_lock)
				{
					if (file_info_queue.Count == 0)
					{
						Thread.Sleep(100);
						continue;
					}

					file_info = file_info_queue.Dequeue();
				}
				#region Create file name

				string file_path = "";
				Web_src_info parent = file_info.Parent;
				while ((parent = parent.Parent) != null)
				{
					file_path = Path.Combine(parent.Name, file_path);
				}
				file_path = Path.Combine(file_path,
					string.Format("{0:D3}{1}", file_info.Parent.Index, Path.GetExtension(file_info.Url))
				);
				file_path = Path.Combine(MainWindow.Main.Settings.Root_dir, file_path);

				#endregion

				WebClient wc = new WebClient();
				wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; rv:10.0.2) Gecko/20100101 Firefox/10.0.2");
				wc.Headers.Add("Cookie", file_info.Parent.Cookie);
				wc.Headers.Add("Referer", file_info.Parent.Url);
				try
				{
					wc.DownloadFile(file_info.Url, file_path);
					byte[] data = wc.DownloadData(file_info.Url);

					FileStream sw = new FileStream(file_path, FileMode.Create);
					sw.Write(data, 0, data.Length);
					sw.Close();

					int downloaded = file_info.Parent.Parent.Counter.Increment();
					file_info.Parent.State = "OK";
					if (downloaded == file_info.Parent.Parent.Counter.All)
					{
						file_info.Parent.Parent.State = "OK";
					}
					else
					{
						file_info.Parent.Parent.State = string.Format("{0}/{1}", downloaded, file_info.Parent.Parent.Counter.All);
					}

					Report("{0}: {1}/{2} , Downloaded: {3}",
						file_info.Parent.Parent.Name,
						downloaded,
						file_info.Parent.Parent.Counter.All,
						file_info.Name);
				}
				catch (Exception ex)
				{
					if (ex is ThreadAbortException) return;

					file_info.Parent.State = "X";
					Log_error(ex, file_info.Url);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="src_info"></param>
		/// <param name="pattern">Regex pattern</param>
		/// <param name="anchor">Levenshtein Distance anchor</param>
		/// <param name="threshold">Levenshtein Distance threshold</param>
		/// <returns></returns>
		private List<Web_src_info> Get_info_list_from_html(
			Web_src_info src_info,
			string pattern,
			string url_format = "{0}",
			string anchor = null,
			int threshold = 10)
		{
			List<Web_src_info> list = new List<Web_src_info>();
			string html = "";
			string[] patterns = pattern.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

			WebClient wc = new WebClient();
			try
			{
				html = wc.DownloadString(src_info.Url);

				for (int i = 0; i < patterns.Length - 1; i++)
				{
					html = Regex.Match(html, patterns[i]).Groups[0].Value;
				}

				MatchCollection mc = Regex.Matches(html, patterns[patterns.Length - 1]);
				for (int i = 0; i < mc.Count; i++)
				{
					if (anchor != null &&
						ys.Common.LevenshteinDistance(anchor, mc[i].Groups["url"].Value) > threshold)
						continue;

					string url = string.Format(url_format, mc[i].Groups["url"].Value);
					string name = mc[i].Groups["name"].Value;
					Web_src_info new_page_info = new Web_src_info(
						url,
						i,
						"",
						null,
						"",
						name == "" ? Path.GetFileName(url) : name,
						src_info);
					list.Add(new_page_info);
				}

				src_info.Children = list;
				src_info.Counter = new Counter(mc.Count);
				src_info.Cookie = wc.ResponseHeaders["Set-Cookie"];
			}
			catch (Exception ex)
			{
				if (!(ex is ThreadAbortException))
					Log_error(ex, src_info.Url);
			}

			return list;
		}
	}
}