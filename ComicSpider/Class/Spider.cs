using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using ComicSpider;
using ComicSpider.App_dataTableAdapters;
using System.Data;
using System.Collections.ObjectModel;

namespace ys.Web
{
	class Comic_spider
	{
		public Comic_spider()
		{
			stopped = true;
			Thread_count = 1;
			file_info_queue = new Queue<Web_src_info>();
			vol_info_queue = new Queue<Web_src_info>();
		}

		public void Async_show_vol_list(string url)
		{
			Thread thread = new Thread(new ParameterizedThreadStart(Show_vol_list));
			thread.Name = "Show_vol_list";
			thread.Start(url);

			Report("Show vol list...");
		}
		public void Async_start(ObservableCollection<Web_src_info> vol_info_list, string root_dir = "")
		{
			Root_dir = root_dir;
			stopped = false;

			foreach (var vol_info in vol_info_list)
			{
				vol_info.Counter.Reset_all();
				vol_info_queue.Enqueue(vol_info);
			}

			for (int i = 0; i < Thread_count; i++)
			{
				Thread downloader = new Thread(new ThreadStart(Downloader));
				downloader.Name = "Downloader";
				downloader.Start();

				Thread info_getter = new Thread(new ThreadStart(Get_page_info_list));
				info_getter.Name = "Get_page_info_list";
				info_getter.Start();
			}

			Report("Comic Spider start...");
		}

		public void Stop()
		{
			file_info_queue.Clear();
			stopped = true;
		}
		public bool Stopped { get { return stopped; } }
		public string Root_dir { get; set; }
		public int Thread_count { get; set; }

		private bool stopped;
		private Queue<Web_src_info> file_info_queue;
		private Queue<Web_src_info> vol_info_queue;

		private void Show_vol_list(object arg)
		{
			string url = arg as string;
			ObservableCollection<Web_src_info> vol_info_list = Get_vol_info_list(url);

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
		private void Log_missed(Exception ex, string url = "", string type = "page")
		{
			try
			{
				Missed_logTableAdapter a = new Missed_logTableAdapter();
				a.Connection.Open();
				a.Insert(
					DateTime.Now,
					url,
					type);
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

		private ObservableCollection<Web_src_info> Get_vol_info_list(string comic_url)
		{
			ObservableCollection<Web_src_info> vol_info_list = new ObservableCollection<Web_src_info>();
			string html = "";

			WebClient wc = new WebClient();
			try
			{
				html = wc.DownloadString(comic_url);
				string cookie = wc.ResponseHeaders["Set-Cookie"];

				Regex reg = new Regex("<title>(?<comic>.+?) Manga .+</title>");
				string comic_name = reg.Match(html).Groups["comic"].Value.Trim();

				reg = new Regex(@"color_0077"" href=""(?<url>.+?)"" (name="".*?"")?>(?<vol>(\n|.)+?)</a>(\n|.)*?</span>(?<title>.*?)</span>");
				MatchCollection mc = reg.Matches(html);
				Counter counter = new Counter(mc.Count);
				for (int i = 0; i < mc.Count; i++)
				{
					vol_info_list.Add(new Web_src_info(
						mc[i].Groups["url"].Value,
						i,
						"",
						counter,
						cookie,
						mc[i].Groups["vol"].Value.Trim(),
						new Web_src_info(comic_url, 0, "", null, cookie, comic_name, null))
					);
				}

				Report("Vol list: {0}", vol_info_list);
			}
			catch (Exception ex)
			{
				Log_error(ex, comic_url);
			}

			return vol_info_list;
		}

		private void Get_page_info_list()
		{
			while (!stopped)
			{
				Web_src_info vol_info;
				lock (vol_info_queue)
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
								@"value=""(?<url>http://.+?)""",
								@"change_page(.|\n)+?/select");
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
		private void Get_file_info_list(ObservableCollection<Web_src_info> page_info_list)
		{
			if (stopped) return;

			string dir_path = "";
			Web_src_info parent = page_info_list[0];
			while ((parent = parent.Parent) != null)
			{
				dir_path = Path.Combine(parent.Name, dir_path);
			}
			dir_path = Path.Combine(Root_dir, dir_path);
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
					int downloaded = page_info.Counter.Increment();
					if (downloaded == page_info.Counter.All)
					{
						page_info.Parent.State = "OK";
					}
					continue;
				}

				try
				{
					ObservableCollection<Web_src_info> file_info_list = Get_info_list_from_html(
								page_info,
								@"src=""(?<url>http://c.mhcdn.net/store/manga/.+?((jpg)|(png)|(gif)|(bmp)))""");

					lock (file_info_queue)
					{
						file_info_queue.Enqueue(file_info_list[0]);
					}
					Report("File: {0}", file_info_list[0].Name);
				}
				catch (Exception ex)
				{
					Log_missed(ex, page_info.Url);
				}
			}
		}

		private void Downloader()
		{
			Web_src_info file_info;

			while (!stopped)
			{
				lock (file_info_queue)
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
				file_path = Path.Combine(Root_dir, file_path);

				#endregion

				WebClient wc = new WebClient();
				wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; rv:10.0.2) Gecko/20100101 Firefox/10.0.2");
				wc.Headers.Add("Cookie", file_info.Cookie);
				wc.Headers.Add("Referer", file_info.Parent.Url);
				try
				{
					wc.DownloadFile(file_info.Url, file_path);
					byte[] data = wc.DownloadData(file_info.Url);
					FileStream sw = new FileStream(file_path, FileMode.Create);
					sw.Write(data, 0, data.Length);
					sw.Close();

					int downloaded = file_info.Parent.Counter.Increment();
					file_info.Parent.State = "OK";
					if (downloaded == file_info.Parent.Counter.All)
					{
						file_info.Parent.Parent.State = "OK";
					}
					else
					{
						file_info.Parent.Parent.State = string.Format("{0}/{1}", downloaded, file_info.Parent.Counter.All);
					}

					Report("{0}: {1}/{2} , Downloaded: {3}",
						file_info.Parent.Parent.Name,
						downloaded,
						file_info.Parent.Counter.All,
						file_info.Name);
				}
				catch (Exception ex)
				{
					file_info.Parent.State = "X";
					Log_error(ex, file_info.Url);
					Log_missed(ex, file_info.Parent.Url);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="page_info"></param>
		/// <param name="pattern">Must have adpter group named "page_info"</param>
		/// <param name="region_pattern"></param>
		/// <returns></returns>
		private ObservableCollection<Web_src_info> Get_info_list_from_html(
			Web_src_info src_info,
			string url_pattern,
			string region_pattern = null)
		{
			ObservableCollection<Web_src_info> list = new ObservableCollection<Web_src_info>();
			string html = "";

			WebClient wc = new WebClient();
			html = wc.DownloadString(src_info.Url);

			try
			{
				if (region_pattern != null)
				{
					Regex reg_region = new Regex(region_pattern);
					html = reg_region.Match(html).Value;
				}

				Regex reg_url = new Regex(url_pattern);
				MatchCollection mc = reg_url.Matches(html);
				Counter counter = new Counter(mc.Count);
				for (int i = 0; i < mc.Count; i++)
				{
					string url = mc[i].Groups["url"].Value;
					Web_src_info new_page_info = new Web_src_info(
						url,
						i,
						"",
						counter,
						wc.ResponseHeaders["Set-Cookie"],
						Path.GetFileName(url),
						src_info);
					list.Add(new_page_info);
				}
				src_info.Children = list;
			}
			catch (Exception ex)
			{
				Log_error(ex, src_info.Url);
			}

			return list;
		}
	}
}