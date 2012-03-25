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
			img_info_queue = new Queue<Web_file_info>();
		}

		public void Async_show_vol_list(string url)
		{
			Thread thread = new Thread(new ParameterizedThreadStart(Show_vol_list));
			thread.Name = "Show_vol_list";
			thread.Start(url);

			Report("Show vol list...");
		}
		public void Async_download(ObservableCollection<Web_page_info> vol_info_list, string root_dir = "")
		{
			Root_dir = root_dir;
			stopped = false;

			for (int i = 0; i < Thread_count; i++)
			{
				Thread downloader = new Thread(new ThreadStart(Downloader));
				downloader.Name = "Downloader";
				downloader.Start();
			}

			Thread infomer = new Thread(new ParameterizedThreadStart(Get_page_info_list));
			infomer.Name = "Get_page_info_list";
			infomer.Start(vol_info_list);

			Report("Comic Spider start...");
		}
		public void Async_download_missed(ObservableCollection<Web_page_info> img_info_list, string root_dir = "")
		{
			stopped = false;
			Root_dir = root_dir;

			foreach (var img_info in img_info_list)
			{
				img_info_queue.Enqueue(img_info);
			}

			for (int i = 0; i < Thread_count; i++)
			{
				Thread downloader = new Thread(new ThreadStart(Downloader));
				downloader.Name = "Missed Downloader";
				downloader.Start();
			}
		}

		public void Stop()
		{
			img_info_queue.Clear();
			stopped = true;
		}
		public bool Stopped { get { return stopped; } }
		public string Root_dir { get; set; }
		public int Thread_count { get; set; }

		private bool stopped;
		private Queue<Web_file_info> img_info_queue;

		private void Show_vol_list(object arg)
		{
			string url = arg as string;
			ObservableCollection<Web_page_info> vol_info_list = Get_vol_info_list(url);

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
					Guid.NewGuid().ToString());
				a.Connection.Close();
			}
			catch (Exception)
			{
			}
		}
		private void Log_missed(Exception ex, string url = "", Web_file_info web_base_info = null)
		{
			try
			{
				Missed_logTableAdapter a = new Missed_logTableAdapter();
				a.Connection.Open();
				a.Insert(
					DateTime.Now,
					url,
					ys.Common.ObjectToByteArray(web_base_info));
				a.Connection.Close();
			}
			catch (Exception)
			{
			}
		}
		private void Report(string info)
		{
			Console.WriteLine(info);
			MainWindow.Main.Dispatcher.Invoke(new MainWindow.Show_info_delegate(MainWindow.Main.Show_info), info);
		}
		private void Report(string format, params object[] arg)
		{
			string info = string.Format(format, arg);
			Console.WriteLine(info);
			MainWindow.Main.Dispatcher.Invoke(new MainWindow.Show_info_delegate(MainWindow.Main.Show_info), info);
		}

		private ObservableCollection<Web_page_info> Get_vol_info_list(string comic_url)
		{
			ObservableCollection<Web_page_info> vol_info_list = new ObservableCollection<Web_page_info>();
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
					vol_info_list.Add(new Web_page_info(
						mc[i].Groups["url"].Value,
						i,
						counter,
						cookie,
						mc[i].Groups["vol"].Value.Trim(),
						new Web_page_info(comic_url, 0, null, cookie, comic_name, null))
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
		private void Get_page_info_list(object arg)
		{
			ObservableCollection<Web_page_info> vol_info_list = arg as ObservableCollection<Web_page_info>;

			#region Insert Vol_info_list into data base

			try
			{
				App_data.Vol_infoDataTable table = new App_data.Vol_infoDataTable();
				Counter counter = new Counter(vol_info_list.Count);
				foreach (var item in vol_info_list)
				{
					item.Counter = counter;
					table.AddVol_infoRow(
						item.Url,
						item.Name,
						item.Index,
						item.Cookie,
						DateTime.Now
					);
				}

				Vol_infoTableAdapter vol_adapter = new Vol_infoTableAdapter();
				vol_adapter.Adapter.DeleteCommand = vol_adapter.Connection.CreateCommand();
				vol_adapter.Adapter.DeleteCommand.CommandText = "delete from Vol_info where 1";

				vol_adapter.Connection.Open();

				vol_adapter.Adapter.DeleteCommand.ExecuteNonQuery();
				vol_adapter.Update(table);

				vol_adapter.Connection.Close();
			}
			catch (Exception ex)
			{
				Log_error(ex);
			}

			#endregion

			Thread[] threads = new Thread[Thread_count];
			int count = 0;
			foreach (var vol_info in vol_info_list)
			{
				if (stopped) return;

				ObservableCollection<Web_page_info> page_info_list;
				try
				{
					page_info_list = Get_info_list_from_html(
							vol_info,
							@"value=""(?<url>http://.+?)""",
							@"change_page(.|\n)+?/select");
				}
				catch (Exception ex)
				{
					Log_error(ex, vol_info.Url);
					continue;
				}

				Report("Page list: {0}", vol_info.Name);

				if (count < Thread_count)
				{
					threads[count] = new Thread(new ParameterizedThreadStart(Get_img_info_list));
					threads[count].Name = "Get_img_info_list";
					threads[count].Start(page_info_list);
				}
				else
				{
					count = 0;
					for (int i = 0; i < Thread_count; i++)
					{
						threads[i].Join();
					}
				}
			}
		}
		private void Get_img_info_list(object arg)
		{
			if (stopped) return;

			ObservableCollection<Web_page_info> page_info_list = arg as ObservableCollection<Web_page_info>;

			string dir_path = "";
			Web_page_info parent = page_info_list[0];
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

				Web_page_info page_info = page_info_list[i];
				try
				{
					ObservableCollection<Web_page_info> img_info_list = Get_info_list_from_html(
								page_info,
								@"src=""(?<url>http://c.mhcdn.net/store/manga/.+?((jpg)|(png)|(gif)|(bmp)))""");

					lock (img_info_queue)
					{
						img_info_queue.Enqueue(img_info_list[0]);
					}
				}
				catch (Exception ex)
				{
					Log_missed(ex, page_info.Url, page_info);
				}
			}
		}

		private void Downloader()
		{
			Web_file_info img_info;

			while (!stopped)
			{
				lock (img_info_queue)
				{
					if (img_info_queue.Count == 0)
					{
						Thread.Sleep(100);
						continue;
					}

					img_info = img_info_queue.Dequeue();
				}

				#region check and create directory and file name

				string file_path = "";
				Web_page_info parent = img_info.Parent;
				while ((parent = parent.Parent) != null)
				{
					file_path = Path.Combine(parent.Name, file_path);
				}
				file_path = Path.Combine(file_path,
					string.Format("{0:D3}{1}", img_info.Parent.Index, Path.GetExtension(img_info.Url))
				);
				file_path = Path.Combine(Root_dir, file_path);

				#endregion

				WebClient wc = new WebClient();
				wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; rv:10.0.2) Gecko/20100101 Firefox/10.0.2");
				wc.Headers.Add("Cookie", img_info.Cookie);
				wc.Headers.Add("Referer", img_info.Parent.Url);
				try
				{
					wc.DownloadFile(img_info.Url, file_path);
					byte[] data = wc.DownloadData(img_info.Url);
					FileStream sw = new FileStream(file_path, FileMode.Create);
					sw.Write(data, 0, data.Length);
					sw.Close();
				}
				catch (Exception ex)
				{
					Log_error(ex, img_info.Url);
					Log_missed(ex, img_info.Parent.Url, img_info.Parent);
				}

				int downloaded = img_info.Parent.Counter.Increment();
				Report("{0}: {1}/{2} , Downloaded: {3}", img_info.Parent.Parent.Name, downloaded, img_info.Parent.Counter.All, img_info.Name);
				MainWindow.Main.Dispatcher.Invoke(
					new MainWindow.Update_progress_delegate(MainWindow.Main.Update_progress),
					img_info
				);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="page_info"></param>
		/// <param name="pattern">Must have adpter group named "page_info"</param>
		/// <param name="region_pattern"></param>
		/// <returns></returns>
		private ObservableCollection<Web_page_info> Get_info_list_from_html(
			Web_page_info src_info,
			string url_pattern,
			string region_pattern = null)
		{
			ObservableCollection<Web_page_info> list = new ObservableCollection<Web_page_info>();
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
					list.Add(new Web_page_info(
						url,
						i,
						counter,
						wc.ResponseHeaders["Set-Cookie"],
						Path.GetFileName(url),
						src_info)
					);
				}
			}
			catch (Exception ex)
			{
				Log_error(ex, src_info.Url);
			}

			return list;
		}
	}
}