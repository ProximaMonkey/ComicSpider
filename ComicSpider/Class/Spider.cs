using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using ComicSpider;
using ComicSpider.UserTableAdapters;
using LuaInterface;
using System.Text;

namespace ys.Web
{
	class Comic_spider
	{
		public Comic_spider()
		{
			stopped = true;

			random = new Random(DateTime.Now.Millisecond);

			file_queue = new Queue<Web_src_info>();
			page_queue = new Queue<Web_src_info>();
			volume_queue = new Queue<Web_src_info>();
			file_queue_lock = new object();
			page_queue_lock = new object();
			volume_queue_lock = new object();
			thread_list = new List<Thread>();

			Async_load_lua_script();
			Init_lua_script_watcher();
		}

		public void Async_get_volume_list()
		{
			Thread thread = new Thread(new ParameterizedThreadStart(Get_volume_list));
			thread.Name = "Show_volume_list";
			thread.Start(Main_settings.Main.Main_url);
			
			Report("Downloading volume list...");
		}
		public void Async_start(System.Collections.IEnumerable vol_info_list)
		{
			Report("Begin downloading...");

			stopped = false;

			volume_queue.Clear();
			page_queue.Clear();
			file_queue.Clear();

			foreach (Web_src_info vol_info in vol_info_list)
			{
				lock (volume_queue_lock)
				{
					if (vol_info.State != Web_src_state.Downloaded)
						volume_queue.Enqueue(vol_info);
				}
			}

			for (int i = 0; i < int.Parse(Main_settings.Main.Thread_count); i++)
			{
				Thread page_list_getter = new Thread(new ThreadStart(Get_page_list));
				page_list_getter.Name = thread_type_Page_list_getter + i;
				page_list_getter.Start();
				thread_list.Add(page_list_getter);

				Thread downloader = new Thread(new ThreadStart(Download_file));
				downloader.Name = thread_type_File_downloader + i;
				downloader.Start();
				thread_list.Add(downloader);

				Thread file_list_getter = new Thread(new ThreadStart(Get_file_list));
				file_list_getter.Name = thread_type_File_list_getter + i;
				file_list_getter.Start();
				thread_list.Add(file_list_getter);
			}
		}

		public void Add_volume_list(List<Web_src_info> list)
		{
			foreach (var vol_info in list)
			{
				volume_queue.Enqueue(vol_info);
			}
		}

		public void Delete_display_pages(string root_dir)
		{
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
			Delete_display_pages(root_dir);

			foreach (var comic_dir in Directory.GetDirectories(root_dir))
			{
				string[] volume_dirs = Directory.GetDirectories(comic_dir);
				if (volume_dirs.Length == 0)
				{
					Create_display_page(comic_dir);
					continue;
				}

				foreach (var volume_dir in volume_dirs)
				{
					Create_display_page(volume_dir);
				}
			}
		}
		public void Create_display_page(string voluem_dir)
		{
			string img_list = "";
			List<string> files = new List<string>();
			foreach (var pattern in file_types)
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
				img_list += string.Format("'{0}',", Path.GetFileName(files[i]).Replace("'", @"\'"));
			}
			StreamReader sr = new StreamReader(@"Asset\layout.html");
			string layout_html = sr.ReadToEnd();
			sr.Close();

			layout_html = layout_html.Replace("<?= img_list ?>", img_list.TrimEnd(','));

			StreamWriter sw = new StreamWriter(Path.Combine(voluem_dir, "index.html"));
			sw.Write(layout_html);
			sw.Close();
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

			Report("Downloading stopped.");
		}
		public bool Stopped { get { return stopped; } }

		public string Default_script_editor = "notepad.exe";

		public const string Raw_folder_name = "Raw file";

		/***************************** Private ********************************/

		private bool stopped;

		private Queue<Web_src_info> file_queue;
		private Queue<Web_src_info> page_queue;
		private Queue<Web_src_info> volume_queue;
		private object file_queue_lock;
		private object page_queue_lock;
		private object volume_queue_lock;
		private List<Thread> thread_list;

		private const string thread_type_Page_list_getter = "Page_list_getter";
		private const string thread_type_File_downloader = "File_downloader";
		private const string thread_type_File_list_getter = "File_list_getter";

		private List<Website_info> supported_websites;

		private List<string> file_types;
		private List<string> user_agents;

		private static bool script_loaded;
		private static string lua_script;

		private Random random;

		private void Async_load_lua_script()
		{
			script_loaded = false;

			Thread thread = new Thread(new ThreadStart(() =>
			{
				file_types = new List<string>();
				user_agents = new List<string>();
				supported_websites = new List<Website_info>();
				lua_script = "";

				try
				{
					lua_script = File.ReadAllText(@"comic_spider.lua");

					Lua lua = new Lua();
					lua.DoString(lua_script);

					foreach (string item in lua.GetTable("settings.file_types").Values)
					{
						file_types.Add(item);
					}
					foreach (string item in lua.GetTable("settings.user_agents").Values)
					{
						user_agents.Add(item);
					}

					if (!string.IsNullOrEmpty(lua.GetString("settings.proxy")))
					{
						WebRequest.DefaultWebProxy = new WebProxy(lua.GetString("settings.proxy"), true);
					}

					if (!string.IsNullOrEmpty(lua.GetString("settings.script_editor")))
					{
						Default_script_editor = lua.GetString("settings.script_editor");
					}

					foreach (string url in (lua.GetTable("settings.requires") as LuaTable).Values)
					{
						if (string.IsNullOrEmpty(url))
							continue;

						// Get remote script
						string loaded_script = Load_remote_script(url);
						lua_script += '\n' + loaded_script;
						
						lua.DoString(loaded_script);

						// Check version
						LuaTable app_info = lua.GetTable("app_info");
						if (app_info != null &&
							(app_info["version"] as string).CompareTo(Main_settings.Main.App_version) > 0)
						{
							MainWindow.Main.Dispatcher.Invoke(
								new MainWindow.Show_update_info_delegate(MainWindow.Main.Show_update_info),
								app_info["notice"],
								app_info["url"]
							);
							Report(app_info["notice"] as string);
							Report(app_info["url"] as string);
						}

						Report("Remote script '{0}' loaded.", url);
					}

					foreach (string site_name in (lua.GetTable("comic_spider") as LuaTable).Keys)
					{
						string home = lua.DoString(string.Format("return comic_spider['{0}'].home", site_name))[0] as string;
						List<string> hosts = new List<string>();
						var host = lua.DoString(string.Format("return comic_spider['{0}'].hosts", site_name))[0];
						if ((host as LuaTable) != null)
						{
							foreach (string item in (host as LuaTable).Values)
							{
								hosts.Add(item);
							}
						}
						supported_websites.Add(new Website_info(site_name, home, hosts));
					}

					supported_websites.Sort((x, y) => { return x.Name.CompareTo(y.Name); });

					Dashboard.Instance.Dispatcher.Invoke(
						new Dashboard.Show_supported_sites_delegate(Dashboard.Instance.Show_supported_sites),
						supported_websites
					);
				}
				catch (LuaException ex)
				{
					System.Windows.MessageBox.Show("Lua exception, " + ex.Message);
				}
				catch (Exception ex)
				{
					System.Windows.MessageBox.Show(ex.Message);
				}

				script_loaded = true;

				// App usage analytics
				var app_analytics_info = new Dictionary<string, string>();
				app_analytics_info.Add("version", Main_settings.Main.App_version);
				app_analytics_info.Add("os", Environment.OSVersion.ToString());
				ys.Web.App_usage_analytics.Post(app_analytics_info);
			}));

			thread.Name = "ScriptLoader";
			thread.Start();
			thread_list.Add(thread);
		}
		private string Load_remote_script(string url)
		{
			Lua_script lua_script;
			string hash = string.Empty;
			WebClientEx wc = new WebClientEx();
			wc.Encoding = System.Text.Encoding.UTF8;

			Key_valueTableAdapter kv_adpter = new Key_valueTableAdapter();
			kv_adpter.Adapter.SelectCommand = kv_adpter.Connection.CreateCommand();
			kv_adpter.Adapter.SelectCommand.CommandText = "select * from [Key_value] where [Key] = @url";
			kv_adpter.Adapter.SelectCommand.Parameters.AddWithValue("@url", url);

			kv_adpter.Connection.Open();

			SQLiteDataReader data_reader = kv_adpter.Adapter.SelectCommand.ExecuteReader();

			if (data_reader.Read())
			{
				lua_script = ys.Common.ByteArrayToObject(data_reader["Value"] as byte[]) as Lua_script;

				// Chech if has been modified.
				wc.Headers.Add("If-None-Match", lua_script.ETag);
				try
				{
					string loaded_script = wc.DownloadString(url);

					lua_script.ETag = wc.ResponseHeaders["ETag"];
					lua_script.Script = loaded_script;

					kv_adpter.Update(
						ys.Common.ObjectToByteArray(lua_script),
						data_reader["Key"] as string,
						data_reader["Value"] as byte[]
					);
				}
				catch{ }
			}
			else
			{
				string loaded_script = wc.DownloadString(url);

				lua_script = new Lua_script(
					loaded_script,
					hash
				);

				kv_adpter.Insert(
					url,
					ys.Common.ObjectToByteArray(lua_script)
				);
			}

			kv_adpter.Connection.Close();

			return lua_script.Script;
		}
		private void Init_lua_script_watcher()
		{
			try
			{
				FileSystemWatcher lua_script_watcher = new FileSystemWatcher(@".\", "comic_spider.lua");
				lua_script_watcher.NotifyFilter = NotifyFilters.LastWrite;
				lua_script_watcher.Changed += (o, e) =>
				{
					lua_script_watcher.EnableRaisingEvents = false;		// Well known bug. This just a hack.
					Report("Reload lua script.");
					Async_load_lua_script();
					lua_script_watcher.EnableRaisingEvents = true;
				};
				lua_script_watcher.EnableRaisingEvents = true;
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message);
			}
		}

		private void Log_error(Exception ex, string url = "")
		{
			Report(ex.Message, url);

			try
			{
				ComicSpider.UserTableAdapters.Error_logTableAdapter a = new ComicSpider.UserTableAdapters.Error_logTableAdapter();
				a.Connection.Open();

				a.Adapter.UpdateCommand = a.Connection.CreateCommand();
				a.Adapter.UpdateCommand.CommandText = "insert or replace into Error_log ([Date_time], [Url], [Title], [Detail]) values (@Date_time, @Url, @Title, @Detail)";
				a.Adapter.UpdateCommand.Parameters.AddWithValue("@Date_time", DateTime.Now);
				a.Adapter.UpdateCommand.Parameters.AddWithValue("@Url", url);
				a.Adapter.UpdateCommand.Parameters.AddWithValue("@Title", ex.Message);
				a.Adapter.UpdateCommand.Parameters.AddWithValue("@Detail", ex.StackTrace);

				a.Connection.Close();
			}
			catch
			{
			}
		}
		private void Report(string format, params object[] arg)
		{
			try
			{
				string info = string.Format(format, arg);
				Console.WriteLine(info);
				Dashboard.Instance.Dispatcher.Invoke(
					new Dashboard.Report_progress_delegate(Dashboard.Instance.Report_progress),
					info
				);
			}
			catch
			{
			}
		}

		private string get_corresponding_controller_name(string url)
		{
			string host = Regex.Match(url, @"(https?://)?(?<host>.+?)/").Groups["host"].Value;
			string controller_name = string.Empty;

			foreach (var site in supported_websites)
			{
				foreach (var item in site.Hosts)
				{
					if (host.Contains(item))
					{
						return site.Name;
					}
				}
			}

			return controller_name;
		}

		private void Get_volume_list(object arg)
		{
			string url = arg as string;
			Web_src_info src_info = new Web_src_info(url, 0, "", "", null);
			Lua_controller lua_c = new Lua_controller();

			if (file_types.Contains(Path.GetExtension(url)))
			{
				src_info.Name = Comic_spider.Raw_folder_name;
				src_info.Children = new List<Web_src_info>();
				src_info.Children.Add(new Web_src_info(url, 0, "", "", src_info));
			}
			else
			{
				src_info.Children = Get_info_list_from_html(lua_c, src_info, "get_volume_list");
			}

			if (src_info.Children.Count > 0)
			{
				Report("Get volume list: {0}, Count: {1}", src_info.Name, src_info.Children.Count);
			}
			else
			{
				Dashboard.Instance.Dispatcher.Invoke(
					new Dashboard.Alert_delegate(Dashboard.Instance.Alert),
					"No volume found in " + url
				);
			}

			Dashboard.Instance.Dispatcher.Invoke(
				new Dashboard.Show_volume_list_delegate(Dashboard.Instance.Show_volume_list),
				src_info.Children
			);
		}
		private void Get_page_list()
		{
			Web_src_info vol_info;
			Lua_controller lua_c = new Lua_controller();

			while (!stopped)
			{
				if (page_queue.Count > thread_list.Count * 2)
				{
					Thread.Sleep(100);
					continue;
				}

				lock (volume_queue_lock)
				{
					if (volume_queue.Count == 0)
					{
						Thread.Sleep(100);
						continue;
					}
					vol_info = volume_queue.Dequeue();
				}

				if (vol_info.Children == null ||
					vol_info.Children.Count == 0)
				{
					try
					{
						if (file_types.Contains(Path.GetExtension(vol_info.Url)))
						{
							vol_info.Name = "";
							vol_info.Cookie = vol_info.Parent.Cookie;
							vol_info.Children = new List<Web_src_info>();
							vol_info.Children.Add(new Web_src_info(vol_info.Url, 0, "", "", vol_info));
						}
						else
						{
							vol_info.Children = Get_info_list_from_html(lua_c, vol_info, "get_page_list");
						}
					}
					catch (ThreadAbortException)
					{
					}
					catch (Exception ex)
					{
						Log_error(ex, vol_info.Url);
						continue;
					}
				}

				if (vol_info.Children.Count > 0)
				{
					#region Create folder
					string dir_path = "";

					foreach (var c in Path.GetInvalidFileNameChars())
					{
						vol_info.Parent.Name = vol_info.Parent.Name.Replace(c, ' ');
					}

					dir_path = ys.Common.Combine_path(
						Main_settings.Main.Root_dir,
						vol_info.Parent.Name,
						vol_info.Name);

					vol_info.Path = dir_path;

					if (!Directory.Exists(dir_path))
					{
						try
						{
							Directory.CreateDirectory(dir_path);
							Report("Create dir: {0}", dir_path);
						}
						catch (ThreadAbortException)
						{
						}
						catch (Exception ex)
						{
							Dashboard.Instance.Dispatcher.Invoke(
								new Dashboard.Alert_delegate(Dashboard.Instance.Alert),
								ex.Message
							);
						}
					}
					#endregion

					lock (page_queue_lock)
					{
						foreach (var page_list in vol_info.Children)
						{
							page_queue.Enqueue(page_list);
						} 
					}
				}
				else
				{
					vol_info.State = Web_src_state.Failed;
					Report("No page found in " + vol_info.Url);

					lock (volume_queue_lock)
					{
						volume_queue.Enqueue(vol_info);
					}
				}
			}
		}
		private void Get_file_list()
		{
			Web_src_info page_info;
			Lua_controller lua_c = new Lua_controller();

			while (!stopped)
			{
				if (file_queue.Count > thread_list.Count * 2)
				{
					Thread.Sleep(100);
					continue;
				}

				lock (page_queue_lock)
				{
					if (page_queue.Count == 0)
					{
						Thread.Sleep(100);
						continue;
					}
					page_info = page_queue.Dequeue();
				}

				if (stopped)
					return;
				if (page_info.State == Web_src_state.Downloaded)
					continue;

				try
				{
					List<Web_src_info> file_info_list;
					if (file_types.Contains(Path.GetExtension(page_info.Url)))
					{
						file_info_list = new List<Web_src_info>();
						page_info.Cookie = page_info.Parent.Cookie;
						file_info_list.Add(new Web_src_info(page_info.Url, 0, "", "", page_info));
					}
					else
					{
						file_info_list = Get_info_list_from_html(lua_c, page_info, "get_file_list");
					}

					if (file_info_list.Count == 0 ||
						file_info_list[0] == null)
						throw new Exception("No file info found in " + page_info.Url);

					lock (file_queue_lock)
					{
						file_queue.Enqueue(file_info_list[0]); 
					}

					Report("Get file info: {0}", file_info_list[0].Url);
				}
				catch (ThreadAbortException)
				{
				}
				catch (Exception ex)
				{
					page_info.State = Web_src_state.Failed;
					lock (page_queue_lock)
					{
						page_queue.Enqueue(page_info);
					}
					Log_error(ex, page_info.Url);
				}
			}
		}
		private void Download_file()
		{
			Web_src_info file_info;
			Lua_controller lua_c = new Lua_controller();
			int time_out = 30 * 1000;
			byte[] buffer = new byte[1024 * 10];
			DateTime timestamp = DateTime.Now;

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

				try
				{
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(file_info.Url);
					request.ReadWriteTimeout = time_out;
					request.Timeout = time_out;

					#region Add headers

					if (user_agents != null)
						request.UserAgent = user_agents.ElementAt(random.Next(user_agents.Count()));
					if (!string.IsNullOrEmpty(file_info.Parent.Cookie))
						request.Headers.Add("Cookie", file_info.Parent.Cookie);
					if (file_info.Parent != null)
						request.Referer = Uri.EscapeUriString(file_info.Parent.Url);

					#endregion

					#region Download Stream

					WebResponse response = request.GetResponse();
					Stream remote_stream = response.GetResponseStream();
					remote_stream.ReadTimeout = time_out;
					if (response.Headers["Content-Type"].Contains("html"))
						throw new Exception("Remote sever error: download file failed.");

					file_info.Parent.Size = (double)response.ContentLength / 1024.0 / 1024.0;

					MemoryStream cache = new MemoryStream();
					int total_recieved_bytes = 0;
					int current_recieved_bytes = 0;
					int speed_recorder = 0;
					do
					{
						current_recieved_bytes = remote_stream.Read(buffer, 0, buffer.Length);
						cache.Write(buffer, 0, current_recieved_bytes);

						// Report state may take up lots of resources.
						total_recieved_bytes += current_recieved_bytes;
						speed_recorder += current_recieved_bytes;
						double time_span = (DateTime.Now - timestamp).TotalSeconds;
						if (time_span > 0.5)
						{
							timestamp = DateTime.Now;
							file_info.Parent.State_text = string.Format(
								"{0:00.0}%  {1:0}KB/s",
								(double)total_recieved_bytes / (double)response.ContentLength * 100.0,
								(double)speed_recorder / time_span / 1024.0
							);
							speed_recorder = 0;
						}
					}
					while (!stopped || current_recieved_bytes > 0);

					#endregion

					#region Create file name

					string controller_name = get_corresponding_controller_name(file_info.Url);

					string file_path = string.Empty;
					bool? is_indexed_file_name = true;
					bool? is_create_view_page = true;

					string name;
					string dir = "";
					Web_src_info parent = file_info.Parent;
					while ((parent = parent.Parent) != null)
					{
						dir = Path.Combine(parent.Name, dir);
					}
					lua_c["dir"] = ys.Common.Combine_path(Main_settings.Main.Root_dir, dir);

					if (file_info.Parent.Parent.Name != Comic_spider.Raw_folder_name)
					{
						if (lua_c.DoString(
							string.Format("return comic_spider['{0}']", controller_name)
						)[0] == null)
							throw new Exception("No contorller found for " + file_info.Url);

						is_create_view_page = lua_c.DoString(
							string.Format("return comic_spider['{0}']['is_create_view_page']", controller_name)
						)[0] as bool?;
						is_indexed_file_name = lua_c.DoString(
							string.Format("return comic_spider['{0}']['is_indexed_file_name']", controller_name)
						)[0] as bool?;
						is_indexed_file_name = is_indexed_file_name == null ? true : is_indexed_file_name;
						is_create_view_page = is_create_view_page == null ? true : is_create_view_page;

						if (is_indexed_file_name == true)
						{
							lua_c["name"] = string.Format("{0:D3}", file_info.Parent.Index + 1);
						}
						else if (string.IsNullOrEmpty(file_info.Name))
						{
							lua_c["name"] = HttpUtility.UrlDecode(Path.GetFileNameWithoutExtension(file_info.Url));
						}
						else
						{
							lua_c["name"] = file_info.Name;
						}
						lua_c["ext"] = Path.GetExtension(file_info.Url);

						lua_c["src_info"] = file_info;

						lua_c.DoString(
							string.Format(
								"if comic_spider['{0}']['handle_file'] then comic_spider['{0}']['handle_file']() end",
								controller_name
							)
						);
						name = lua_c.GetString("name") + lua_c.GetString("ext");
					}
					else
					{
						name = HttpUtility.UrlDecode(Path.GetFileName(file_info.Url));
					}

					// Remove invalid char
					foreach (var c in Path.GetInvalidFileNameChars())
					{
						name = name.Replace(c, ' ');
					}

					file_path = ys.Common.Combine_path(lua_c.GetString("dir"), name);

					#endregion

					FileStream fs = new FileStream(file_path, FileMode.Create);
					fs.Write(cache.GetBuffer(), 0, (int)cache.Length);
					fs.Close();

					file_info.Parent.Path = file_path;
					file_info.Parent.State = Web_src_state.Downloaded;

					int downloaded = file_info.Parent.Parent.Downloaded;

					Report("{0} {1}: {2} / {3} , Downloaded: {4}",
						file_info.Parent.Parent.Parent.Name,
						file_info.Parent.Parent.Name,
						downloaded,
						file_info.Parent.Parent.Count,
						file_path);

					if (downloaded == file_info.Parent.Parent.Count)
					{
						file_info.Parent.Parent.State = Web_src_state.Downloaded;

						if (is_create_view_page == true)
						{
							try
							{
								Create_display_page(lua_c.GetString("dir"));
							}
							catch (Exception ex)
							{
								Log_error(ex, file_info.Url);
							}
						}
					}
					else
					{
						file_info.Parent.Parent.State_text = string.Format("{0} / {1}", downloaded, file_info.Parent.Parent.Count);
					}

					Dashboard.Instance.Dispatcher.Invoke(
						new Dashboard.Report_main_progress_delegate(
							Dashboard.Instance.Report_main_progress
						)
					);
				}
				catch (ThreadAbortException)
				{
				}
				catch (LuaException ex)
				{
					Report("Lua exception, " + ex.Message);
				}
				catch (Exception ex)
				{
					file_info.Parent.State = Web_src_state.Failed;

					lock (file_queue_lock)
					{
						file_queue.Enqueue(file_info);
					}

					Log_error(ex, file_info.Url);
				}
			}
		}

		private List<Web_src_info> Get_info_list_from_html(Lua_controller lua_c, Web_src_info src_info, params string[] func_list)
		{
			List<Web_src_info> info_list = new List<Web_src_info>();

			string controller_name = get_corresponding_controller_name(src_info.Url);

			WebClientEx wc = new WebClientEx();
			if (user_agents != null)
				wc.Headers.Add("User-Agent", user_agents.ElementAt(
						random.Next(user_agents.Count())
					)
				);
			if (src_info.Parent != null) wc.Headers.Add("Referer", Uri.EscapeUriString(src_info.Parent.Url));

			try
			{
				#region Lua lua_script controller
				lua_c["src_info"] = src_info;
				lua_c["info_list"] = info_list;

				if (lua_c.DoString(string.Format("return comic_spider['{0}']", controller_name))[0] == null)
					throw new Exception("No controller found for " + controller_name);

				bool exists_method = false;
				foreach (var func in func_list)
				{
					exists_method = lua_c.DoString(
						string.Format("return comic_spider['{0}']['{1}']", controller_name, func)
					)[0] != null;
				}
				if (exists_method)
				{
					string encoding = lua_c.DoString(
						string.Format("return comic_spider['{0}']['charset']", controller_name)
					)[0] as string;

					wc.Encoding = System.Text.Encoding.GetEncoding(
						string.IsNullOrEmpty(encoding) ? "utf-8" : encoding);

					lua_c["html"] = wc.DownloadString(src_info.Url);

					src_info.Cookie = wc.ResponseHeaders["Set-Cookie"] + "";

					foreach (var func in func_list)
					{
						lua_c.DoString(string.Format("comic_spider['{0}']:{1}()", controller_name, func));
					}
				}
				else
				{
					src_info.Cookie = src_info.Parent.Cookie + "";
					info_list.Add(new Web_src_info(src_info.Url, src_info.Index, src_info.Name, "", src_info));
				}
				#endregion
			}
			catch (ThreadAbortException)
			{
			}
			catch (LuaException ex)
			{
				Report("Lua exception, " + ex.Message);
			}
			catch (Exception ex)
			{
				Log_error(ex, src_info.Url);
			}

			return info_list;
		}

		private class Lua_controller : Lua
		{
			public Lua_controller()
			{
				while (!Comic_spider.script_loaded)
				{
					Thread.Sleep(100);
				}

				this["lc"] = this;

				main = MainWindow.Main;
				dashboard = Dashboard.Instance;
				settings = Main_settings.Main;

				this.DoString(Comic_spider.lua_script);
			}

			public MainWindow main;
			public Dashboard dashboard;
			public Main_settings settings;

			public string find(string pattern)
			{
				Match m = Regex.Match(this.GetString("html"), pattern, RegexOptions.IgnoreCase);
				if (!string.IsNullOrEmpty(m.Groups["find"].Value))
					return m.Groups["find"].Value;
				else if (!string.IsNullOrEmpty(m.Groups[1].Value))
					return m.Groups[1].Value;
				else
					return m.Groups[0].Value;
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

					if (!string.IsNullOrEmpty(src_info.Name))
						this["name"] = this.GetString("name").Replace(src_info.Name, "").Trim();

					if (step != null)
						(step as LuaFunction).Call(i, mc[i].Groups, mc);

					if (string.IsNullOrEmpty(this.GetString("url")))
						continue;

					list.Add(
						new Web_src_info(
							this.GetString("url"),
							i,
							ys.Common.Format_for_number_sort(this.GetString("name")),
							"",
							src_info
						)
					);
				}
			}
			public void fill_list(LuaTable patterns, LuaFunction step = null)
			{
				Web_src_info src_info = this["src_info"] as Web_src_info;
				List<Web_src_info> list = this["info_list"] as List<Web_src_info>;
				MatchCollection mc;
				string all_sections = this.GetString("html");

				for (int i = 1; i < patterns.Values.Count; i++)
				{
					string sections = string.Empty;
					mc = Regex.Matches(all_sections, patterns[i] as string, RegexOptions.IgnoreCase);
					foreach (Match m in mc)
					{
						sections += '\n' + m.Groups[0].Value;
					}
					all_sections = sections;
				}

				mc = Regex.Matches(all_sections, patterns[patterns.Values.Count] as string, RegexOptions.IgnoreCase);
				for (var i = 0; i < mc.Count; i++)
				{
					this["url"] = mc[i].Groups["url"].Value;
					this["name"] = mc[i].Groups["name"].Value.Trim();

					if (!string.IsNullOrEmpty(src_info.Name))
						this["name"] = this.GetString("name").Replace(src_info.Name, "").Trim();

					if (step != null)
						(step as LuaFunction).Call(i, mc[i].Groups, mc);

					if (string.IsNullOrEmpty(this.GetString("url")))
						continue;
					
					list.Add(
						new Web_src_info(
							this.GetString("url"),
							i,
							ys.Common.Format_for_number_sort(this.GetString("name")),
							"",
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

					if (string.IsNullOrEmpty(this.GetString("url")))
						continue;
					
					list.Add(
						new Web_src_info(
							this.GetString("url"),
							i,
							ys.Common.Format_for_number_sort(this.GetString("name")),
							"",
							src_info
						)
					);
				}
			}
			public void xfill_list(string selector, LuaFunction step)
			{
				Web_src_info src_info = this["src_info"] as Web_src_info;
				List<Web_src_info> list = this["info_list"] as List<Web_src_info>;

				HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
				doc.LoadHtml(this.GetString("html"));

				HtmlAgilityPack.HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes(selector);

				if (nodes == null)
					throw new Exception("XPath: nothing selected.");

				this["name"] = string.Empty;

				for (int i = 0; i < nodes.Count; i++)
				{
					if (step != null)
						(step as LuaFunction).Call(i, nodes[i], nodes);

					if (string.IsNullOrEmpty(this.GetString("url")))
						continue;
					else
					{
						if (!string.IsNullOrEmpty(src_info.Name))
							this["name"] = this.GetString("name").Replace(src_info.Name, "").Trim();
					}

					list.Add(
						new Web_src_info(
							this.GetString("url"),
							i,
							ys.Common.Format_for_number_sort(this.GetString("name")),
							"",
							src_info
						)
					);
				}
			}

			public void add(string url, int index, string name, Web_src_info parent = null)
			{
				if (string.IsNullOrEmpty(url))
					return;

				List<Web_src_info> list = this["info_list"] as List<Web_src_info>;

				if (parent != null &&
					!string.IsNullOrEmpty(parent.Name))
					name = name.Replace(parent.Name, "").Trim();

				list.Add(new Web_src_info(
					url,
					index,
					ys.Common.Format_for_number_sort(name),
					"",
					parent)
				);
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

		[Serializable]
		private class Lua_script
		{
			public Lua_script(string script, string hash)
			{
				Script = script;
				ETag = hash;
			}

			public string Script { get; set; }
			public string ETag { get; set; }
			public DateTime Date { get; set; }
		}
	}
}