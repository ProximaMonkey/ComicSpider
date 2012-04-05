using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using ComicSpider;
using LuaInterface;
using System.Linq;

namespace ys.Web
{
	class Comic_spider
	{
		public Comic_spider()
		{
			stopped = true;

			file_queue = new Queue<Web_src_info>();
			volume_queue = new Queue<Web_src_info>();
			file_queue_lock = new object();
			volume_queue_lock = new object();
			thread_list = new List<Thread>();

			Load_script();
		}

		public void Async_get_volume_list()
		{
			Load_script();

			Thread thread = new Thread(new ParameterizedThreadStart(Show_volume_list));
			thread.Name = "Show_volume_list";
			thread.Start(Main_settings.Main.Main_url);
			
			Report("Show volume list...");
		}
		public void Async_start(System.Collections.IEnumerable vol_info_list)
		{
			stopped = false;

			Load_script();

			volume_queue.Clear();
			file_queue.Clear();
			foreach (Web_src_info vol_info in vol_info_list)
			{
				lock (volume_queue_lock)
				{
					volume_queue.Enqueue(vol_info); 
				}
			}

			for (int i = 0; i < int.Parse(Main_settings.Main.Thread_count); i++)
			{
				Thread downloader = new Thread(new ThreadStart(Downloader));
				downloader.Name = "Downloader" + i;
				downloader.Start();
				thread_list.Add(downloader);

				Thread info_getter = new Thread(new ThreadStart(Get_page_list));
				info_getter.Name = "Get_page_list" + i;
				info_getter.Start();
				thread_list.Add(info_getter);
			}

			Report("Comic Spider start...");
		}

		public void Add_volume_list(List<Web_src_info> list)
		{
			lock (volume_queue)
			{
				foreach (var vol_info in list)
				{
					volume_queue.Enqueue(vol_info);
				}
			}
		}

		public void Stop(bool completed = false)
		{
			stopped = true;

			if (!completed)
			{
				foreach (Thread thread in thread_list)
				{
					thread.Abort();
				}
			}
			thread_list.Clear();
		}
		public bool Stopped { get { return stopped; } }

		/***************************** Private ********************************/

		private bool stopped;
		private Queue<Web_src_info> file_queue;
		private Queue<Web_src_info> volume_queue;
		private object file_queue_lock;
		private object volume_queue_lock;
		private List<Thread> thread_list;
		private LuaTable file_types;
		private string script;

		public void Delete_display_pages()
		{
			string root_dir = Main_settings.Main.Root_dir;
			List<string> old_files = new List<string>();
			old_files.AddRange(Directory.GetFiles(root_dir, "layout.js", SearchOption.AllDirectories));
			old_files.AddRange(Directory.GetFiles(root_dir, "jquery.js", SearchOption.AllDirectories));
			old_files.AddRange(Directory.GetFiles(root_dir, "layout.css", SearchOption.AllDirectories));
			old_files.AddRange(Directory.GetFiles(root_dir, "index.html", SearchOption.AllDirectories));
			foreach (var item in old_files)
			{
				File.Delete(item);
			}
		}
		public void Fix_display_pages(string root_dir)
		{
			var comic_dirs = Directory.GetDirectories(root_dir);

			// Save modified date.
			List<DateTime> modify_date_list = new List<DateTime>();
			foreach (var comic_dir in comic_dirs)
			{
				modify_date_list.Add(new DirectoryInfo(comic_dir).LastWriteTime);
			}

			Delete_display_pages();

			foreach (var comic_dir in comic_dirs)
			{
				string[] volume_dirs = Directory.GetDirectories(comic_dir);
				if (volume_dirs.Length == 0)
				{
					Create_display_page(comic_dir, Path.GetFileName(root_dir));
					continue;
				}

				foreach (var volume_dir in volume_dirs)
				{
					Create_display_page(volume_dir, Path.GetFileName(comic_dir));
				}
			}

			// Recover modified date.
			for (int i = 0; i < comic_dirs.Length; i++)
			{
				new DirectoryInfo(comic_dirs[i]).LastWriteTime = modify_date_list[i];
			}
		}
		public void Create_display_page(string voluem_dir, string comic_name)
		{
			string img_dom_list = "";
			List<string> files = new List<string>();
			foreach (var pattern in file_types.Values)
			{
				files.AddRange(Directory.GetFiles(voluem_dir, "*" + pattern.ToString()));
			}

			if (files.Count == 0)
			{
				return;
			}

			string parent_dir = Directory.GetParent(voluem_dir).FullName;
			if (!File.Exists(Path.Combine(parent_dir, "layout.js")))
			{
				File.Copy(@"Asset\layout.js", Path.Combine(parent_dir, "layout.js"), true);
				File.Copy(@"Asset\jquery.js", Path.Combine(parent_dir, "jquery.js"), true);
				File.Copy(@"Asset\layout.css", Path.Combine(parent_dir, "layout.css"), true);
			}

			for (int i = 0; i < files.Count; i++)
			{
				img_dom_list += string.Format(@"
					<div class=""img_frame"" index=""{0:D3}"">
						<div><span class=""page_num"">{0:D3} / {1:D3}</span></div>
						<img class=""page"" index=""{0:D3}"" src=""{2:D3}""/>
					</div>
					<hr />", i, files.Count, Path.GetFileName(files[i])
				);
			}
			StreamReader sr = new StreamReader(@"Asset\layout.html");
			string layout_html = sr.ReadToEnd();
			sr.Close();

			layout_html = layout_html.Replace("<?= img_dom_list ?>", img_dom_list);

			StreamWriter sw = new StreamWriter(Path.Combine(voluem_dir, "index.html"));
			sw.Write(layout_html);
			sw.Close();
		}

		private void Show_volume_list(object arg)
		{
			string url = arg as string;
			List<Web_src_info> vol_info_list = Get_volume_list(new Web_src_info(url, 0, ""));

			if (Main_settings.Main.Latest_volume_only)
			{
				object is_volume_order_desc = null;
				Web_src_info latest_volume = vol_info_list.First();
				Lua lua = new Lua();
				lua.DoString(script);

				try
				{
					is_volume_order_desc = lua.DoString(
								string.Format("return comic_spider['{0}']['{1}']",
									get_host(url),
									"is_volume_order_desc"
								)
							)[0];
				}
				catch (Exception ex)
				{
					Log_error(ex, url);
				}
				lua.Dispose();

				if (is_volume_order_desc != null &&
					(bool)is_volume_order_desc == false)
				{
					latest_volume = vol_info_list.Last();
				}

				vol_info_list.Clear();
				vol_info_list.Add(latest_volume);
			}

			Dashboard.Instance.Dispatcher.Invoke(
				new Dashboard.Show_vol_list_delegate(Dashboard.Instance.Show_volume_list),
				vol_info_list);
		}
		private void Log_error(Exception ex, string url = "")
		{
			Report(ex.Message, url);

			try
			{
				ComicSpider.UserTableAdapters.Error_logTableAdapter a = new ComicSpider.UserTableAdapters.Error_logTableAdapter();
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
		private void Report(string format, params object[] arg)
		{
			string info = string.Format(format, arg);
			Console.WriteLine(info);
			Dashboard.Instance.Dispatcher.Invoke(new Dashboard.Report_progress_delegate(Dashboard.Instance.Report_progress), info);
		}

		private string get_host(string url)
		{
			string main = Regex.Match(url, @"(http://)?(?<host>.+?)/").Groups["host"].Value;
			string[] sections = main.Split('.');
			if (sections.Length >= 2)
			{
				return sections[sections.Length - 2] + '.' + sections[sections.Length - 1];
			}
			else
				return string.Empty;
		}
		private void Load_script()
		{
			try
			{
				var sr = new StreamReader(@"comic_spider.lua");
				script = sr.ReadToEnd();
				sr.Close();

				Lua lua = new Lua();
				lua.DoString(script);
				file_types = lua.GetTable("comic_spider.file_types");
			}
			catch (Exception ex)
			{
				Report(ex.Message);
			}
		}
		private List<Web_src_info> Get_volume_list(Web_src_info comic_info)
		{
			List<Web_src_info> vol_info_list = new List<Web_src_info>();

			vol_info_list = Get_info_list_from_html(comic_info, "get_comic_name", "get_volume_list");

			if (vol_info_list.Count == 0)
			{
				vol_info_list.Add(new Web_src_info(comic_info.Url, 0, comic_info.Name));
			}

			Report("Get volume list: {0}, Count: {1}", comic_info.Name, comic_info.Children.Count);

			return vol_info_list;
		}
		private void Get_page_list()
		{
			while (!stopped)
			{
				lock (file_queue_lock)
				{
					if (file_queue.Count > thread_list.Count)
					{
						Thread.Sleep(100);
						continue;
					}
				}

				Web_src_info vol_info;
				lock (volume_queue_lock)
				{
					if (volume_queue.Count == 0)
					{
						return;
					}
					vol_info = volume_queue.Dequeue();
				}

				if (vol_info.Children == null ||
					vol_info.Children.Count == 0)
				{
					try
					{
						vol_info.Children = Get_info_list_from_html(vol_info, "get_page_list");
					}
					catch (Exception ex)
					{
						if (ex is ThreadAbortException) return;

						Log_error(ex, vol_info.Url);
						continue;
					}
				}

				//Report("Page list: {0}", vol_info.Name);

				Get_file_list(vol_info.Children);
			}
		}
		private void Get_file_list(List<Web_src_info> page_info_list)
		{
			string dir_path = "";

			if (page_info_list.Count == 0)
			{
				Report("No page found.");
				return;
			}

			#region Create folder
			Web_src_info parent = page_info_list[0];
			while ((parent = parent.Parent) != null)
			{
				dir_path = Path.Combine(parent.Name, dir_path);
			}
			dir_path = Path.Combine(Main_settings.Main.Root_dir, dir_path);
			if (!Directory.Exists(dir_path))
			{
				Directory.CreateDirectory(dir_path);
				Report("Create dir: {0}", dir_path);
			}
			#endregion

			foreach (var page_info in page_info_list)
			{
				if (stopped)
					return;
				if (page_info.State == Web_src_info.State_downloaded)
					continue;

				try
				{
					List<Web_src_info> file_info_list = Get_info_list_from_html(page_info, "get_file_list");

					lock (file_queue_lock)
					{
						file_queue.Enqueue(file_info_list[0]);
					}
					Report("Get file info: {0}", file_info_list[0].Url);
				}
				catch (Exception ex)
				{
					if (ex is ThreadAbortException) return;
					page_info.State = Web_src_info.State_missed;
					Log_error(ex, page_info.Url);
				}
			}
		}
		private List<Web_src_info> Get_info_list_from_html(Web_src_info src_info, params string[] func_list)
		{
			List<Web_src_info> info_list = new List<Web_src_info>();
			src_info.Children = info_list;

			string host = get_host(src_info.Url);

			Lua_controller lua_c = new Lua_controller(script);
			lua_c["lc"] = lua_c;
			lua_c["cs"] = this;
			lua_c["dashboard"] = Dashboard.Instance;
			lua_c["settings"] = Main_settings.Main;
			lua_c["src_info"] = src_info;
			lua_c["info_list"] = info_list;


			WebClientEx wc = new WebClientEx();
			wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; rv:10.0.2) Gecko/20100101 Firefox/10.0.2");
			try
			{
				#region Lua script controller
				bool exists_method = false;
				foreach (var func in func_list)
				{
					exists_method = lua_c.DoString(string.Format("return comic_spider['{0}']['{1}']", host, func))[0] != null;
				}
				if (exists_method)
				{
					if (lua_c.DoString(string.Format("return comic_spider['{0}']", host))[0] == null)
						throw new Exception("No controller found for this site.");

					string encoding = lua_c.DoString(string.Format("return comic_spider['{0}']['charset']", host))[0] as string;

					wc.Encoding = System.Text.Encoding.GetEncoding(
						string.IsNullOrEmpty(encoding) ? "utf-8" : encoding);

					lua_c["html"] = wc.DownloadString(src_info.Url);

					foreach (var func in func_list)
					{
						lua_c.DoString(string.Format("comic_spider['{0}']['{1}']();", host, func));
					}

					src_info.Cookie = wc.ResponseHeaders["Set-Cookie"];
				}
				else
				{
					info_list.Add(new Web_src_info(src_info.Url, src_info.Index, src_info.Name, src_info));
					src_info.Cookie = src_info.Parent.Cookie;
				}
				#endregion
			}
			catch (Exception ex)
			{
				if (!(ex is ThreadAbortException))
					Log_error(ex, src_info.Url);
			}

			lua_c.Dispose();

			return info_list;
		}
		private void Downloader()
		{
			Web_src_info file_info;

			while (!stopped)
			{
				lock (file_queue_lock)
				{
					if (file_queue.Count == 0)
					{
						Thread.Sleep(100);
						continue;
					}

					file_info = file_queue.Dequeue();
				}

				#region Create file name
				string file_name = string.Format("{0:D3}{1}", file_info.Parent.Index, Path.GetExtension(file_info.Url));
				string dir = "";
				Web_src_info parent = file_info.Parent;
				while ((parent = parent.Parent) != null)
				{
					dir = Path.Combine(parent.Name, dir);
				}
				dir = Path.Combine(Main_settings.Main.Root_dir, dir);
				file_name = Path.Combine(dir, file_name);
				#endregion

				WebClient wc = new WebClient();
				wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; rv:10.0.2) Gecko/20100101 Firefox/10.0.2");
				wc.Headers.Add("Cookie", file_info.Parent.Cookie);
				try
				{
					wc.DownloadFile(file_info.Url, file_name);
					byte[] data = wc.DownloadData(file_info.Url);

					FileStream sw = new FileStream(file_name, FileMode.Create);
					sw.Write(data, 0, data.Length);
					sw.Close();

					file_info.Parent.State = Web_src_info.State_downloaded;

					int downloaded = file_info.Parent.Parent.Downloaded;

					Report("{0} {1}: {2} / {3} , Downloaded: {4}",
						file_info.Parent.Parent.Parent.Name,
						file_info.Parent.Parent.Name,
						downloaded,
						file_info.Parent.Parent.Count,
						file_info.Name);

					if (downloaded == file_info.Parent.Parent.Count)
					{
						file_info.Parent.Parent.State = Web_src_info.State_downloaded;
						try
						{
							Create_display_page(dir, file_info.Parent.Parent.Parent.Name);
						}
						catch (Exception ex)
						{
							Log_error(ex, file_info.Url);
						}
						Dashboard.Instance.Dispatcher.Invoke(
							new Dashboard.Report_volume_progress_delegate(
								Dashboard.Instance.Report_volume_progress
							)
						);
					}
					else
					{
						file_info.Parent.Parent.State = string.Format("{0} / {1}", downloaded, file_info.Parent.Parent.Count);
					}
				}
				catch (Exception ex)
				{
					if (ex is ThreadAbortException) return;

					file_info.Parent.State = Web_src_info.State_missed;

					lock (file_queue_lock)
					{
						file_queue.Enqueue(file_info);
					}

					Log_error(ex, file_info.Url);
				}
			}
		}

		private class Lua_controller : Lua
		{
			public Lua_controller(string script)
			{
				this.DoString(script);
			}

			public string find(string pattern)
			{
				return Regex.Match(this.GetString("html"), pattern, RegexOptions.IgnoreCase).Groups["find"].Value;
			}
			public void filtrate(string pattern)
			{
				string html = string.Empty;
				MatchCollection mc = Regex.Matches(this.GetString("html"), pattern, RegexOptions.IgnoreCase);
				foreach (Match m in mc)
				{
					html += m.Groups[0].Value;
				}
				this["html"] = html;
			}

			public void fill_list(string pattern, LuaFunction step = null)
			{
				Web_src_info src_info = this["src_info"] as Web_src_info;
				List<Web_src_info> list = this["info_list"] as List<Web_src_info>;
				MatchCollection mc = Regex.Matches(this.GetString("html"), pattern, RegexOptions.IgnoreCase);
				for (var i = 0; i < mc.Count; i++)
				{
					this["url"] = mc[i].Groups["url"].Value;
					this["name"] = mc[i].Groups["name"].Value.Trim();

					if (string.IsNullOrEmpty(this.GetString("name")))
						this["name"] = Path.GetFileName(this.GetString("url")).Trim();

					if (!string.IsNullOrEmpty(src_info.Name))
						this["name"] = this.GetString("name").Replace(src_info.Name, "").Trim();

					if (step != null)
						(step as LuaFunction).Call(i, mc[i].Groups, mc);

					list.Add(
						new Web_src_info(
							this.GetString("url"),
							i,
							ys.Common.Format_for_number_sort(this.GetString("name")),
							src_info
						)
					);
				}
			}
			public void fill_list(Newtonsoft.Json.Linq.JArray arr, LuaFunction step = null)
			{
				Web_src_info src_info = this["src_info"] as Web_src_info;
				List<Web_src_info> list = this["info_list"] as List<Web_src_info>;
				for (int i = 0; i < arr.Count; i++)
				{
					this["url"] = arr[i].ToString();
					this["name"] = Path.GetFileName(this.GetString("url")).Trim();

					if (!string.IsNullOrEmpty(src_info.Name))
						this["name"] = this.GetString("name").Replace(src_info.Name, "").Trim();

					if (step != null)
						(step as LuaFunction).Call(i, arr[i].ToString(), arr);

					list.Add(
						new Web_src_info(
							this.GetString("url"),
							i,
							ys.Common.Format_for_number_sort(this.GetString("name")),
							src_info
						)
					);
				}
			}

			public Web_src_info web_src_info(string url, int index, string name, Web_src_info parent = null)
			{
				return new Web_src_info(url, index, name, parent);
			}

			public int levenshtein_distance(string s, string t)
			{
				return ys.Common.LevenshteinDistance(s, t);
			}

			public object json_decode(string input)
			{
				return Newtonsoft.Json.JsonConvert.DeserializeObject(input);
			}
		}
	}
}