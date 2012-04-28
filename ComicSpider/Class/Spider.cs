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
using System.Text;
using LuaInterface;

namespace ys
{
	class Comic_spider
	{
		public Comic_spider()
		{
			stopped = true;

			worker_thread_pool = new List<Thread>();

			Async_load_lua_script();
			Init_lua_script_watcher();
		}

		public readonly Task_manager Manager = new Task_manager();

		public void Stop(bool completed = false)
		{
			stopped = true;

			Manager.Stop();

			if (!completed)
			{
				foreach (Thread thread in worker_thread_pool)
				{
					thread.Abort();
				}
			}
			worker_thread_pool.Clear();

			Report("Downloading stopped.");
		}
		public bool Stopped { get { return stopped; } }

		public string Default_script_editor = "notepad.exe";
		public string Raw_file_folder = "Raw file";

		public void Async_get_volume_list()
		{
			Thread thread = new Thread(new ParameterizedThreadStart(Get_volume_list));
			thread.Name = "Show_volume_list";
			thread.Start(Main_settings.Instance.Main_url);
			
			Report("Downloading volume list...");
		}
		public void Async_start()
		{
			Report("Downloading...");

			stopped = false;

			Manager.Start_monitor();
			Manager.Reset_failed_items();

			Add_worker(new ThreadStart(Get_page_list), thread_type_Page_list_getter);

			int worker_num = int.Parse(Main_settings.Instance.Thread_count);
			
			for (int i = 0; i < worker_num / 6 + 1; i++)
			{
				Add_worker(new ThreadStart(Get_file_list), thread_type_File_list_getter + i);
			}

			for (int i = 0; i < worker_num; i++)
			{
				Add_worker(new ThreadStart(Download_file), thread_type_File_downloader + i);
			}
		}

		public void Delete_display_pages(string root_dir)
		{
			List<string> old_files = new List<string>();
			old_files.AddRange(Directory.GetFiles(root_dir, "index.js", SearchOption.AllDirectories));
			old_files.AddRange(Directory.GetFiles(root_dir, "layout.js", SearchOption.AllDirectories));
			old_files.AddRange(Directory.GetFiles(root_dir, "lib.js", SearchOption.AllDirectories));
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

			StreamWriter sw;

			var comic_dirs = Directory.GetDirectories(root_dir);

			if (comic_dirs.Length > 0)
			{
				if (Directory.GetDirectories(comic_dirs[0]).Length == 0)
				{
					lock (index_file_lock)
					{
						sw = new StreamWriter(ys.Common.Combine_path(root_dir, "index.js"), true, Encoding.UTF8);
						foreach (var vol_dir in comic_dirs)
						{
							sw.WriteLine("volume_list.push('" + vol_dir.Replace('\\', '/').Replace("'", "\\'") + "/index.html');");
							Create_display_page(vol_dir);
						}
						sw.Close();
					}
				}
				else
				{
					foreach (var comic_dir in comic_dirs)
					{
						lock (index_file_lock)
						{
							sw = new StreamWriter(ys.Common.Combine_path(comic_dir, "index.js"), true, Encoding.UTF8);
							var volume_dirs = Directory.GetDirectories(comic_dir);

							foreach (var vol_dir in volume_dirs)
							{
								sw.WriteLine("volume_list.push('" + vol_dir.Replace('\\', '/').Replace("'", "\\'") + "/index.html');");
								Create_display_page(vol_dir);
							}
							sw.Close();
						}
					}
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
				File.Copy(@"Asset\lib.js", Path.Combine(parent_dir, "lib.js"), true);
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

		public static bool Is_script_loaded;
		public static string Lua_script;

		/***************************** Private ********************************/

		private bool stopped;

		private List<Thread> worker_thread_pool;
		private int worker_cooldown_span = 100;		// millisecond

		private const string thread_type_Page_list_getter = "Page_list_getter";
		private const string thread_type_File_list_getter = "File_list_getter";
		private const string thread_type_File_downloader = "File_downloader";

		private object index_file_lock = new object();
		private object db_lock = new object();

		private List<Website_info> supported_websites;

		private List<string> file_types;

		private void Async_load_lua_script()
		{
			Is_script_loaded = false;

			Thread thread = new Thread(new ThreadStart(() =>
			{
				file_types = new List<string>();
				supported_websites = new List<Website_info>();
				Lua_script = "";

				try
				{
					Lua_script = File.ReadAllText(@"comic_spider.lua");

					Lua_controller lua = new Lua_controller(false);

					foreach (string item in lua.GetTable("settings.file_types").Values)
					{
						file_types.Add(item);
					}

					if (!string.IsNullOrEmpty(lua.GetString("settings.proxy")))
					{
						WebRequest.DefaultWebProxy = new WebProxy(lua.GetString("settings.proxy"), true);
					}

					if (!string.IsNullOrEmpty(lua.GetString("settings.script_editor")))
					{
						Default_script_editor = lua.GetString("settings.script_editor");
					}

					if (!string.IsNullOrEmpty(lua.GetString("settings.raw_file_folder")))
					{
						Raw_file_folder = lua.GetString("settings.raw_file_folder");
					}

					foreach (string url in (lua.GetTable("settings.requires") as LuaTable).Values)
					{
						if (string.IsNullOrEmpty(url))
							continue;

						// Get remote script
						string loaded_script = Load_remote_script(url);
						Lua_script += '\n' + loaded_script;

						if (loaded_script == null)
							Report("Failed to load remote script: " + url);
						else
						{
							Report("Remote script loaded: " + url);

							lua.DoString(loaded_script);
						}

						// Check version
						LuaTable app_info = lua.GetTable("app_info");
						if (app_info != null &&
							(app_info["version"] as string).CompareTo(Main_settings.Instance.App_version) > 0)
						{
							MainWindow.Main.Dispatcher.Invoke(
								new MainWindow.Show_update_info_delegate(MainWindow.Main.Show_update_info),
								app_info["notice"],
								app_info["url"]
							);
							Report(app_info["notice"] as string);
							Report(app_info["url"] as string);
						}
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

				Is_script_loaded = true;

				App_analyse();
			}));

			thread.Name = "ScriptLoader";
			thread.Start();
		}
		private string Load_remote_script(string url)
		{
			Lua_script lua_script = null;
			Web_client wc = new Web_client();
			wc.Encoding = System.Text.Encoding.UTF8;

			Key_valueTableAdapter kv_adpter = new Key_valueTableAdapter();
			kv_adpter.Adapter.SelectCommand = kv_adpter.Connection.CreateCommand();
			kv_adpter.Adapter.SelectCommand.CommandText = "select * from [Key_value] where [Key] = @url";
			kv_adpter.Adapter.SelectCommand.Parameters.AddWithValue("@url", url);

			kv_adpter.Connection.Open();

			SQLiteDataReader data_reader = kv_adpter.Adapter.SelectCommand.ExecuteReader();

			if (data_reader.Read())
			{
				try
				{
					try
					{
						lua_script = ys.Common.ByteArrayToObject(data_reader["Value"] as byte[]) as Lua_script;
						// Chech if has been modified.
						wc.Headers.Add("If-None-Match", lua_script.ETag);
					}
					catch
					{
						lua_script = new Lua_script("", "");
					}

					string loaded_script = wc.DownloadString(url);

					lua_script.ETag = wc.ResponseHeaders["ETag"];
					lua_script.Script = loaded_script;

					kv_adpter.Update(
						ys.Common.ObjectToByteArray(lua_script),
						data_reader["Key"] as string,
						data_reader["Value"] as byte[]
					);
				}
				catch(Exception ex)
				{
					if (((System.Net.HttpWebResponse)((((System.Net.WebException)(ex)).Response))).StatusCode != HttpStatusCode.NotModified)
						Report(ex.Message);
				}
			}
			else
			{
				string loaded_script = wc.DownloadString(url);

				lua_script = new Lua_script(
					loaded_script,
					wc.ResponseHeaders["ETag"]
				);

				kv_adpter.Insert(
					url,
					ys.Common.ObjectToByteArray(lua_script)
				);
			}

			kv_adpter.Connection.Close();

			if (lua_script == null)
				return null;
			else
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
		private void App_analyse()
		{
			var info = new Dictionary<string, string>();
			info.Add("version", Main_settings.Instance.App_version);
			info.Add("os", ys.Web.Get_user_agent());

			try
			{
				Web_client.Post("http://comicspider.sinaapp.com/analytics/?r=a", info);
			}
			catch { }
		}

		private void Log_error(Exception ex, string url = "")
		{
			lock (db_lock)
			{
				Report(ex.Message + " " + url);

				try
				{
					ComicSpider.UserTableAdapters.Error_logTableAdapter a = new ComicSpider.UserTableAdapters.Error_logTableAdapter();

					a.Adapter.UpdateCommand = a.Connection.CreateCommand();
					a.Adapter.UpdateCommand.CommandText = "insert or replace into Error_log ([Date_time], [Url], [Title], [Detail]) values (@Date_time, @Url, @Title, @Detail)";
					a.Adapter.UpdateCommand.Parameters.AddWithValue("@Date_time", DateTime.Now);
					a.Adapter.UpdateCommand.Parameters.AddWithValue("@Url", url);
					a.Adapter.UpdateCommand.Parameters.AddWithValue("@Title", ex.Message);
					a.Adapter.UpdateCommand.Parameters.AddWithValue("@Detail", ex.StackTrace);

					a.Connection.Open();
					a.Adapter.UpdateCommand.ExecuteNonQuery();
					a.Connection.Close();
				}
				catch (Exception err)
				{
					Report(err.Message);
				}
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

		private void Add_worker(ThreadStart work, string name = "comic_spider_worker")
		{
			Thread worker = new Thread(work);
			worker.Name = name;
			worker.Start();
			worker_thread_pool.Add(worker);
		}

		private void Get_volume_list(object arg)
		{
			string url = arg as string;
			Lua_controller lua_c = new Lua_controller();
			Web_resource_info root = new Web_resource_info(url, 0, "", "", null);

			if (file_types.Contains(ys.Common.Get_web_src_extension(url)))
			{
				root.Name = Raw_file_folder;
				root.Children = new List<Web_resource_info>();
				root.Children.Add(new Web_resource_info(url, 0, "", "", root));
			}
			else
			{
				root.Children = Get_info_list_from_html(lua_c, root, "get_volumes");
			}

			if (root.Count > 0)
			{
				foreach (var c in Path.GetInvalidFileNameChars())
				{
					root.Name = root.Name.Replace(c, ' ');
				}
				root.Path = Path.Combine(Main_settings.Instance.Root_dir, root.Name);

				Report("Get volume list: {0}, Count: {1}", root.Name, root.Children.Count);
				Dashboard.Instance.Dispatcher.Invoke(
					new Dashboard.Show_volume_list_delegate(Dashboard.Instance.Show_volume_list),
					root.Children
				);
			}
			else
			{
				Dashboard.Instance.Dispatcher.Invoke(
					new Dashboard.Alert_delegate(Dashboard.Instance.Alert),
					"No volume found in " + url
				);
			}
			Dashboard.Instance.Dispatcher.Invoke(
				new Dashboard.Hide_working_delegate(Dashboard.Instance.Hide_working)
			);
		}
		private void Get_page_list()
		{
			Web_resource_info vol_info = null;
			Lua_controller lua_c = new Lua_controller();

			while (!stopped)
			{
				try
				{
					vol_info = null;
					vol_info = Manager.Volumes_dequeue();
					if (vol_info == null)
					{
						Thread.Sleep(worker_cooldown_span);
						continue;
					}

					if (vol_info.Count == 0)
					{
						if (file_types.Contains(ys.Common.Get_web_src_extension(vol_info.Url)))
						{
							vol_info.Children = new List<Web_resource_info>();
							vol_info.Children.Add(new Web_resource_info(vol_info.Url, 0, "", "", vol_info));
						}
						else
						{
							vol_info.Children = Get_info_list_from_html(lua_c, vol_info, "get_pages");
						}
					}

					if (vol_info.Count > 0)
					{
						Dashboard.Instance.Dispatcher.Invoke(
							new Dashboard.Report_main_progress_delegate(
								Dashboard.Instance.Report_main_progress
							)
						);

						#region Create folder

						foreach (var c in Path.GetInvalidFileNameChars())
						{
							vol_info.Name = vol_info.Name.Replace(c, ' ');
						}

						if (string.IsNullOrEmpty(vol_info.Path))
							vol_info.Path = Path.Combine(vol_info.Parent.Path, vol_info.Name);

						if (!Directory.Exists(vol_info.Path))
						{
							try
							{
								Directory.CreateDirectory(vol_info.Path);
								Report("Create dir: {0}", vol_info.Path);

								lock (index_file_lock)
								{
									StreamWriter sw = new StreamWriter(Path.Combine(vol_info.Parent.Path, "index.js"), true, Encoding.UTF8);
									sw.WriteLine("volume_list.push('" + vol_info.Path.Replace('\\', '/').Replace("'", "\\'") + "/index.html');");
									sw.Close();
								}
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
					}
					else
					{
						vol_info.State = Web_resource_state.Failed;

						Report("No page found in " + vol_info.Url);
					}
				}
				catch (ThreadAbortException)
				{
				}
				catch (Exception ex)
				{
					if (vol_info != null)
					{
						Log_error(ex, vol_info.Url);
						Thread.Sleep(worker_cooldown_span);
					}
				}
			}
		}
		private void Get_file_list()
		{
			Web_resource_info page_info = null;
			Lua_controller lua_c = new Lua_controller();

			while (!stopped)
			{
				try
				{
					page_info = null;
					page_info = Manager.Pages_dequeue();
					if (page_info == null)
					{
						Thread.Sleep(worker_cooldown_span);
						continue;
					}

					if (stopped)
						return;
					if (page_info.State == Web_resource_state.Downloaded)
						continue;

					if (page_info.Count == 0)
					{
						if (file_types.Contains(ys.Common.Get_web_src_extension(page_info.Url)))
						{
							page_info.Children = new List<Web_resource_info>();
							page_info.Children.Add(new Web_resource_info(page_info.Url, 0, "", "", page_info));
						}
						else
						{
							page_info.Children = Get_info_list_from_html(lua_c, page_info, "get_files");
						}

						if (page_info.Count == 0)
						{
							page_info.State = Web_resource_state.Failed;
							Report("No file info found in " + page_info.Url);
						}
					}
				}
				catch (ThreadAbortException)
				{
				}
				catch (Exception ex)
				{
					if (page_info != null)
					{
						page_info.State = Web_resource_state.Failed;

						Log_error(ex, page_info.Url);
					}
					Thread.Sleep(worker_cooldown_span);
				}
			}
		}
		private void Download_file()
		{
			Web_resource_info file_info = null;
			Lua_controller lua_c = new Lua_controller();
			int time_out = 30 * 1000;
			byte[] buffer = new byte[1024 * 10];
			DateTime timestamp = DateTime.Now;

			while (!stopped)
			{
				try
				{
					file_info = null;
					file_info = Manager.Files_dequeue();
					if (file_info == null)
					{
						Thread.Sleep(worker_cooldown_span);
						continue;
					}

					string host = ys.Web.Get_host_name(file_info.Url);

					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(file_info.Url);
					request.ReadWriteTimeout = time_out;
					request.Timeout = time_out;

					#region Update headers

					if (file_info.Parent != null)
						request.Referer = Uri.EscapeUriString(file_info.Parent.Url);
					string cookie = Cookie_pool.Instance.Get(host);
					if (!string.IsNullOrEmpty(cookie))
						request.Headers["Cookie"] = cookie;

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
						if (stopped ||
							file_info.Parent.State == Web_resource_state.Paused)
						{
							throw new Stop_exception();
						}

						current_recieved_bytes = remote_stream.Read(buffer, 0, buffer.Length);
						cache.Write(buffer, 0, current_recieved_bytes);

						// Report state may take up lots of resources.
						total_recieved_bytes += current_recieved_bytes;
						speed_recorder += current_recieved_bytes;
						double time_span = (DateTime.Now - timestamp).TotalSeconds;
						if (time_span > 0.5)
						{
							timestamp = DateTime.Now;
							file_info.Parent.Speed = (double)speed_recorder / time_span / 1024.0;
							file_info.Parent.Progress = (double)total_recieved_bytes / (double)response.ContentLength * 100.0;
							speed_recorder = 0;
						}
					}
					while (current_recieved_bytes > 0);

					#endregion

					#region Create file name

					Website_info website = get_website_info(host);

					string file_path = string.Empty;
					bool? is_auto_format_name = true;
					bool? is_create_view_page = true;

					string name;
					lua_c["dir"] = file_info.Parent.Parent.Path;

					if (file_info.Parent.Parent.Name == Raw_file_folder)
					{
						name = HttpUtility.UrlDecode(Path.GetFileName(file_info.Url));
					}
					else
					{
						is_create_view_page = lua_c.DoString(
							string.Format("return comic_spider['{0}']['is_create_view_page']", website.Name)
						)[0] as bool?;
						is_auto_format_name = lua_c.DoString(
							string.Format("return comic_spider['{0}']['is_auto_format_name']", website.Name)
						)[0] as bool?;
						is_auto_format_name = is_auto_format_name == null ? true : is_auto_format_name;
						is_create_view_page = is_create_view_page == null ? true : is_create_view_page;

						if (is_auto_format_name == true)
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
						lua_c["ext"] = ys.Common.Get_web_src_extension(file_info.Url);

						lua_c["src_info"] = file_info;

						lua_c.DoString(
							string.Format(
								"if comic_spider['{0}']['handle_file'] then comic_spider['{0}']['handle_file']() end",
								website.Name
							)
						);
						name = lua_c.GetString("name") + lua_c.GetString("ext");
					}

					// Remove invalid char
					foreach (var c in Path.GetInvalidFileNameChars())
					{
						name = name.Replace(c, ' ');
					}

					file_path = Path.Combine(lua_c.GetString("dir"), name);

					#endregion

					FileStream fs = new FileStream(file_path, FileMode.Create);
					fs.Write(cache.GetBuffer(), 0, (int)cache.Length);
					fs.Close();

					file_info.Parent.Path = file_path;
					file_info.State = Web_resource_state.Downloaded;
					file_info.Parent.State = Web_resource_state.Downloaded;
					file_info.Parent.Progress = 100;
					file_info.Parent.Speed = 0;

					int downloaded = file_info.Parent.Parent.Downloaded;

					Report("{0} {1}: {2} / {3}",
						file_info.Parent.Parent.Parent.Name,
						file_info.Parent.Parent.Name,
						downloaded,
						file_info.Parent.Parent.Count);

					if (downloaded == file_info.Parent.Parent.Count)
					{
						file_info.Parent.Parent.State = Web_resource_state.Downloaded;

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

					file_info.Parent.Parent.NotifyPropertyChanged("Progress_int_text");

					Dashboard.Instance.Dispatcher.Invoke(
						new Dashboard.Report_main_progress_delegate(
							Dashboard.Instance.Report_main_progress
						)
					);
				}
				catch (ThreadAbortException)
				{ }
				catch (Stop_exception)
				{ }
				catch (LuaException ex)
				{
					Report("Lua exception: " + ex.Message);
				}
				catch (Exception ex)
				{
					if (file_info != null)
					{
						file_info.Parent.State = Web_resource_state.Failed;
						file_info.Parent.Speed = 0;
						Log_error(ex, file_info.Url);
					}

					Thread.Sleep(worker_cooldown_span);
				}
			}
		}

		private Website_info get_website_info(string host)
		{
			foreach (var site in supported_websites)
			{
				if (site.Hosts.Contains(host))
				{
					return site;
				}
			}
			throw new Exception("No controller found for " + host);
		}

		/// <summary>
		/// Donwload and parse html via lua script controller.
		/// </summary>
		/// <param name="lua_c"></param>
		/// <param name="src_info"></param>
		/// <param name="func_name_list">Lua function name list</param>
		/// <returns></returns>
		private List<Web_resource_info> Get_info_list_from_html(
			Lua_controller lua_c,
			Web_resource_info src_info,
			params string[] func_name_list)
		{
			List<Web_resource_info> info_list = new List<Web_resource_info>();
			string host = ys.Web.Get_host_name(src_info.Url);

			Web_client wc = new Web_client();

			// Set headers
			if (src_info.Parent == null)
				wc.Headers["Referer"] = Uri.EscapeUriString(src_info.Url);
			else
				wc.Headers["Referer"] = Uri.EscapeUriString(src_info.Parent.Url);
			string cookie = Cookie_pool.Instance.Get(host);
			if (!string.IsNullOrEmpty(cookie))
				wc.Headers["Cookie"] = cookie;

			try
			{
				#region Lua Lua_script controller

				Website_info website = get_website_info(host);

				lua_c["src_info"] = src_info;
				lua_c["info_list"] = info_list;

				bool exists_method = false;
				foreach (var func in func_name_list)
				{
					exists_method = lua_c.DoString(
						string.Format("return comic_spider['{0}']['{1}']", website.Name, func)
					)[0] != null;
				}
				if (exists_method)
				{
					// Init controller
					if (!website.Is_inited)
					{
						lua_c.DoString(string.Format("if comic_spider['{0}'].init then comic_spider['{0}']:init() end", website.Name));
						website.Is_inited = true;
					}

					// Get encoding
					string encoding = lua_c.DoString(
						string.Format("return comic_spider['{0}']['charset']", website.Name)
					)[0] as string;
					wc.Encoding = System.Text.Encoding.GetEncoding(
						string.IsNullOrEmpty(encoding) ? "utf-8" : encoding);

					try
					{
						lua_c["html"] = wc.DownloadString(src_info.Url);
						Cookie_pool.Instance.Update(host, wc.ResponseHeaders["Set-Cookie"]);
					}
					catch (WebException ex)
					{
						var response = (HttpWebResponse)ex.Response;
						switch (response.StatusCode)
						{
							case HttpStatusCode.InternalServerError:
								lua_c["html"] = new StreamReader(
									response.GetResponseStream(),
									wc.Encoding
								).ReadToEnd();
								Cookie_pool.Instance.Update(host, response.Headers["Set-Cookie"]);
								break;

							default:
								throw ex;
						}
					}

					bool? is_auto_format_name = true;
					is_auto_format_name = lua_c.DoString(
						string.Format("return comic_spider['{0}']['is_auto_format_name']", website.Name)
					)[0] as bool?;
					is_auto_format_name = is_auto_format_name == null ? true : is_auto_format_name;
					lua_c["is_auto_format_name"] = is_auto_format_name;

					foreach (var func in func_name_list)
					{
						lua_c.DoString(string.Format("comic_spider['{0}']:{1}()", website.Name, func));
					}
				}
				else
				{
					info_list.Add(new Web_resource_info(src_info.Url, src_info.Index, src_info.Name, "", src_info));
				}
				#endregion
			}
			catch (ThreadAbortException)
			{
				wc.Dispose();
			}
			catch (LuaException ex)
			{
				Report("Lua exception: " + ex.Message);
				wc.Dispose();
			}
			catch (Exception ex)
			{
				Log_error(ex, src_info.Url);
				Thread.Sleep(worker_cooldown_span);
				wc.Dispose();
			}

			return info_list;
		}

		private class Stop_exception : Exception
		{ }
	}
}