using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using ComicSpider.UserTableAdapters;
using ys;

namespace ComicSpider
{
	public enum Start_button_state { Start, Stop }

	public partial class Dashboard : Window
	{
		/***************************** Public ********************************/

		public static Dashboard Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new Dashboard();
					Is_initialized = true;
				}
				return instance;
			}
		}
		public static bool Is_initialized = false;
		public new string Title
		{
			get { return base.Title; }
			set
			{
				base.Title = value;
				MainWindow.Main.Title = value;

				if (txt_console.LineCount >= Main_settings.Instance.Max_console_line)
				{
					txt_console.Text = txt_console.Text.Substring(
												txt_console.GetCharacterIndexFromLineIndex(
													Main_settings.Instance.Max_console_line / 2
												)
											);
				}
				txt_console.AppendText(">> " + value + '\n');
			}
		}
		public string Main_progress
		{
			get
			{
				int count = downloaded_files_count;
				int all = all_files_count;

				btn_start.IsEnabled = all != count;
				MainWindow.Main.Start_button.IsEnabled = btn_start.IsEnabled;

				if (count == all)
					MainWindow.Main.Taskbar.SetProgressState(ys.Win7.TbpFlag.NoProgress);
				else
					MainWindow.Main.Taskbar.ChangeProcessValue((ulong)count, (ulong)all);

				return string.Format("{0}%   {1} / {2}", all == 0 ? 0 : count * 100 / all, count, all);
			}
		}

		public bool Is_all_downloaded { get; set; }
		public bool Is_all_left_failed
		{
			get
			{
				int count = 0;
				foreach (var vol in comic_spider.Manager.Volumes)
				{
					if (vol.State == Web_resource_state.Failed &&
						vol.Count == 0)
					{
						count++;
						continue;
					}

					foreach (var page in vol.Children)
					{
						if (page.State == Web_resource_state.Failed)
						{
							count++;
						}
					}
				}
				return count == (all_files_count - downloaded_files_count) &&
					count > 0;
			}
		}

		public new void Show()
		{
			base.Show();
			this.WindowState = System.Windows.WindowState.Normal;
			this.Activate();
		}

		public void Get_volume_list(string url)
		{
			Update_settings();

			txt_main_url.Text = url;
			if (string.IsNullOrEmpty(txt_dir.Text))
			{
				string path = Get_direcotry("Please select a folder to save the comic");
				if (!string.IsNullOrEmpty(path))
				{
					txt_dir.Text = path;
					btn_get_list_Click(null, null);
				}
			}
			else
			{
				btn_get_list_Click(null, null);
			}
		}

		public void Start()
		{
			btn_start_Click(null, null);
		}
		public void Stop()
		{
			btn_start_Click(null, null);
		}
		public void Update_settings()
		{
			Main_settings.Instance.Main_url = txt_main_url.Text;
			Main_settings.Instance.Root_dir = txt_dir.Text;
			Main_settings.Instance.Max_download_speed = int.Parse(txt_max_speed.Text);
			Main_settings.Instance.Start_button_enabled = btn_start.IsEnabled;

			if (txt_thread.Text == "0")
			{
				Message_box.Show("Thread number should greater than zero.");
				txt_thread.Text = "1";
			}
			Main_settings.Instance.Thread_count = int.Parse(txt_thread.Text);
		}
		public void Save_all()
		{
			Update_settings();

			try
			{
				Save_vol_info_list();
				Save_page_info_list();
				//Save_file_info_list();
				Cookie_pool.Instance.Save();
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message + "\n" + ex.StackTrace);
			}
		}
		public new void Close()
		{
			this.Closing -= Window_Closing;
			base.Close();
		}

		/**************** Delegate ****************/

		public delegate void Show_volume_list_delegate(List<Web_resource_info> list);
		public void Show_volume_list(List<Web_resource_info> list)
		{
			foreach (var item in list)
			{
				comic_spider.Manager.Volumes.Add(item);
			}

			if (comic_spider.Stopped)
				working_icon.Hide_working();

			MainWindow.Main.Task_done();

			if (comic_spider.Stopped)
			{
				if (Main_settings.Instance.Is_auto_begin)
					btn_start_Click(null, null);
			}

			MainWindow.Main.Show_balloon(this.Title, (o, e) =>
			{
				this.Show();
			});

			MainWindow.Main.Main_progress = this.Main_progress;
		}

		public delegate void Log_delegate(string info);
		public void Log(string info)
		{
			if (txt_console.LineCount >= Main_settings.Instance.Max_console_line)
			{
				txt_console.Text = txt_console.Text.Substring(
											txt_console.GetCharacterIndexFromLineIndex(
												Main_settings.Instance.Max_console_line / 2
											)
										);
			}
			txt_console.AppendText(">> " + info + '\n');
		}

		public delegate void Report_progress_delegate(string info);
		public void Report_progress(string info)
		{
			this.Title = info;

			if (volume_list.SelectedItems.Count > 0 &&
				page_list.SelectedItems.Count == 0)
			{
				volume_list_SelectionChanged(null, null);
			}
		}

		public delegate void Report_main_progress_delegate();
		public void Report_main_progress()
		{
			MainWindow.Main.Main_progress = this.Main_progress;

			if (Is_all_downloaded) return;

			int all = all_files_count;
			if (downloaded_files_count == all)
			{
				Is_all_downloaded = true;

				MainWindow.Main.Taskbar.FlashTaskBar(ys.Win7.FlashOption.FLASHW_ALL);

				comic_spider.Stop(true);
				btn_start_state = Start_button_state.Start;
				working_icon.Hide_working();

				if (all != 0)
				{
					this.Title = "All completed.";
					MainWindow.Main.Show_balloon(this.Title, (o, e) =>
					{
						try
						{
							System.Diagnostics.Process.Start(Main_settings.Instance.Root_dir);
						}
						catch (Exception ex)
						{
							Message_box.Show(ex.Message);
						}
					}, true);
				}

				Auto_shutdown();
			}
		}

		public delegate void Alert_delegate(string info);
		public void Alert(string info)
		{
			this.Title = info;
			MainWindow.Main.Show_balloon(this.Title, (o, e) =>
			{
				this.Show();
			}, true);
		}

		public delegate void Hide_working_delegate();
		public void Hide_working()
		{
			if (comic_spider.Stopped)
				working_icon.Hide_working();

			MainWindow.Main.Task_done();
		}

		public delegate void Stop_downloading_delegate(string info);
		public void Stop_downloading(string info)
		{
			this.Title = info;
			MainWindow.Main.Show_balloon(this.Title, (o, e) =>
			{
				this.Show();
			}, true);

			comic_spider.Stop(true);
			btn_start_state = Start_button_state.Start;
			working_icon.Hide_working();
		}

		/***************************** Private ********************************/

		private Dashboard()
		{
			InitializeComponent();
			
			if (Main_settings.Instance.Is_need_clear_cache)
				Clear_cache();

			comic_spider = new Comic_spider();
			
			try
			{
				Init_info_list();
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message + '\n' + ex.StackTrace);
			}

			txt_main_url.Text = Main_settings.Instance.Main_url;
			txt_dir.Text = Main_settings.Instance.Root_dir;
			txt_thread.Text = Main_settings.Instance.Thread_count.ToString();
			txt_max_speed.Text = Main_settings.Instance.Max_download_speed.ToString();

			comic_spider.Manager.Stop();

			volume_list.ItemsSource = comic_spider.Manager.Volumes;
			volume_drag_drop_manager = new WPF.JoshSmith.ServiceProviders.UI.ListViewDragDropManager<Web_resource_info>(volume_list);

			Is_all_downloaded = true;
			MainWindow.Main.Main_progress = this.Main_progress;
		}

		private static Dashboard instance;
		private Comic_spider comic_spider;
		private int all_files_count
		{
			get
			{
				int count = 0;
				foreach (Web_resource_info vol in volume_list.Items)
				{
					if (vol.Count == 0)
						count++;
					else
						count += vol.Count;
				}
				return count;
			}
		}
		private int downloaded_files_count
		{
			get
			{
				int count = 0;
				foreach (Web_resource_info vol in volume_list.Items)
				{
					count += vol.Downloaded;
				}
				return count;
			}
		}

		private Start_button_state btn_start_state
		{
			get
			{
				if ((btn_start.Content as string) == "Start")
					return Start_button_state.Start;
				else
					return Start_button_state.Stop;
			}
			set
			{
				switch (value)
				{
					case Start_button_state.Start:
						btn_start.Content = "Start";
						MainWindow.Main.pbar_downloading.Visibility = Visibility.Hidden;
						break;
					case Start_button_state.Stop:
						btn_start.Content = "Stop";
						MainWindow.Main.pbar_downloading.Visibility = Visibility.Visible;
						break;
					default:
						break;
				}
				MainWindow.Main.Start_button.Header = btn_start.Content;
			}
		}

		WPF.JoshSmith.ServiceProviders.UI.ListViewDragDropManager<Web_resource_info> volume_drag_drop_manager;

		private string Get_direcotry(string info, string init_path = null)
		{
			string path = null;

			bool topmost_temp = MainWindow.Main.Topmost;
			MainWindow.Main.Topmost = false;

			var dialog = new Ionic.Utils.FolderBrowserDialogEx();
			dialog.ShowFullPathInEditBox = true;
			if (init_path == null)
				dialog.SelectedPath = txt_dir.Text;
			else
				dialog.SelectedPath = init_path;
			dialog.Description = info;
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
				Directory.Exists(dialog.SelectedPath))
			{
				path = dialog.SelectedPath;
			}

			MainWindow.Main.Topmost = topmost_temp;

			return path;
		}

		private void Auto_shutdown()
		{
			if (cb_auto_shutdown.IsChecked == true)
			{
				int time_to_wait = 30;		// second
				System.Diagnostics.Process.Start("cmd", "/c shutdown -s -t " + time_to_wait);

				Save_all();

				if (!Message_box.Show(
					string.Format("System will be shutdown after {0} seconds, click 'Cancel' to cancel.", time_to_wait)
					, true))
				{
					System.Diagnostics.Process.Start("cmd", "/c shutdown -a");
				}
			}
		}

		/**************** Data ****************/

		private void Init_info_list()
		{
			Volume_listTableAdapter vol_adpter = new Volume_listTableAdapter();
			User.Volume_listDataTable volume_table = vol_adpter.GetData();

			if (volume_table.Count > 0)
			{
				var groups = volume_table.GroupBy(v => v.Parent_name);
				foreach (var group in groups)
				{
					Web_resource_info comic = null;
					foreach (User.Volume_listRow row in group)
					{
						if (comic == null)
						{
							comic = new Web_resource_info(row.Parent_url, 0, row.Parent_name, "", null);
							comic.Children = new List<Web_resource_info>();
						}
						Web_resource_info src_info = new Web_resource_info(
							row.Url,
							row.Index,
							row.Name,
							row.Path,
							comic)
							{
								State = (Web_resource_state)row.State
							};

						comic.Children.Add(src_info);
						comic_spider.Manager.Volumes.Add(src_info);
					}
				}
				Init_page_info_list(comic_spider.Manager.Volumes);
			}
		}
		private void Init_page_info_list(System.Collections.ObjectModel.ObservableCollection<Web_resource_info> vol_list)
		{
			Page_listTableAdapter page_adpter = new Page_listTableAdapter();
			User.Page_listDataTable page_table = page_adpter.GetData();

			File_listTableAdapter file_adpter = new File_listTableAdapter();
			User.File_listDataTable file_table = file_adpter.GetData();

			if (page_table.Count > 0)
			{
				var groups = page_table.GroupBy(p => p.Parent_url);
				foreach (var group in groups)
				{
					Web_resource_info volume = null;
					foreach (User.Page_listRow row in group)
					{
						if (volume == null)
						{
							foreach (Web_resource_info vol in vol_list)
							{
								if (vol.Url == row.Parent_url)
									volume = vol;
							}
							volume.Children = new List<Web_resource_info>();
						}

						var page_info = new Web_resource_info(
							row.Url,
							row.Index,
							row.Name,
							row.Path,
							volume)
							{
								State = (Web_resource_state)row.State,
								Progress = row.Progress,
								Speed = row.Speed,
								Size = row.Size,
							};

						//var file_row = file_table.FirstOrDefault(f => f.Parent_url == page_info.Url);
						//if (file_row != null)
						//{
						//    page_info.Children = new List<Web_resource_info>();
						//    page_info.Children.Add(
						//        new Web_resource_info(file_row.Url, file_row.Index, file_row.Name, "", null)
						//    );
						//}

						volume.Children.Add(page_info);
					}
				}
			}
		}

		private void Save_vol_info_list()
		{
			Volume_listTableAdapter vol_adapter = new Volume_listTableAdapter();
			vol_adapter.Adapter.DeleteCommand = vol_adapter.Connection.CreateCommand();
			vol_adapter.Adapter.DeleteCommand.CommandText = "delete from [Volume_list] where 1";

			vol_adapter.Connection.Open();

			SQLiteTransaction transaction = vol_adapter.Connection.BeginTransaction();

			vol_adapter.Adapter.DeleteCommand.ExecuteNonQuery();

			foreach (Web_resource_info item in volume_list.Items)
			{
				if (item.Parent == null) continue;

				vol_adapter.Insert(
					item.Url,
					item.Name,
					item.Index,
					(int)item.State,
					item.Parent.Url,
					item.Parent.Name,
					item.Path
				);
			}

			transaction.Commit();

			vol_adapter.Connection.Close();
		}
		private void Save_page_info_list()
		{
			Page_listTableAdapter page_adapter = new Page_listTableAdapter();
			page_adapter.Adapter.DeleteCommand = page_adapter.Connection.CreateCommand();
			page_adapter.Adapter.DeleteCommand.CommandText = "delete from [Page_list] where 1";

			page_adapter.Connection.Open();

			SQLiteTransaction transaction = page_adapter.Connection.BeginTransaction();

			page_adapter.Adapter.DeleteCommand.ExecuteNonQuery();

			foreach (Web_resource_info vol in volume_list.Items)
			{
				if (vol.Count == 0) continue;
				foreach (Web_resource_info item in vol.Children.Distinct(new Web_resource_info.Comparer()))
				{
					page_adapter.Insert(
						item.Url,
						item.Name,
						item.Index,
						(int)item.State,
						item.Progress,
						item.Speed,
						item.Size,
						vol.Url,
						item.Path
					);
				}
			}

			transaction.Commit();
			page_adapter.Connection.Close();
		}
		private void Save_file_info_list()
		{
			File_listTableAdapter file_adapter = new File_listTableAdapter();
			file_adapter.Adapter.DeleteCommand = file_adapter.Connection.CreateCommand();
			file_adapter.Adapter.DeleteCommand.CommandText = "delete from [File_list] where 1";

			file_adapter.Connection.Open();

			SQLiteTransaction transaction = file_adapter.Connection.BeginTransaction();

			file_adapter.Adapter.DeleteCommand.ExecuteNonQuery();

			foreach (Web_resource_info vol in volume_list.Items)
			{
				if (vol.Count == 0) continue;
				foreach (Web_resource_info item in vol.Children.Distinct(new Web_resource_info.Comparer()))
				{
					if (item.Count > 0)
					{
						var file_info = item.Children[0];
						file_adapter.Insert(
							file_info.Url,
							file_info.Name,
							file_info.Index,
							item.Url
						);
					}
				}
			}

			transaction.Commit();
			file_adapter.Connection.Close();
		}

		private void Clear_cache()
		{
			Key_valueTableAdapter kv_adpter = new Key_valueTableAdapter();

			kv_adpter.Connection.Open();

			kv_adpter.Adapter.DeleteCommand = kv_adpter.Connection.CreateCommand();
			kv_adpter.Adapter.DeleteCommand.CommandText =
@"delete from [Key_value] where [Key] != 'Settings';
delete from [Error_log] where 1;
delete from [Cookie] where 1;";

			kv_adpter.Adapter.DeleteCommand.ExecuteNonQuery();

			kv_adpter.Connection.Close();
		}

		/**************** Event ****************/

		public void btn_fix_display_pages_Click(object sender, RoutedEventArgs e)
		{
			string path;
			if (volume_list.SelectedItem != null)
			{
				path = Get_direcotry(
					"Select the root folder for opertion",
					Directory.GetParent((volume_list.SelectedItem as Web_resource_info).Path).FullName
				);
			}
			else
			{
				path = Get_direcotry(
					"Select the root folder for opertion",
					txt_dir.Text
				);
			}

			if (string.IsNullOrEmpty(path)) return;

			Control btn = sender as Control;
			btn.IsEnabled = false;

			working_icon.Show_working();

			System.ComponentModel.BackgroundWorker bg_worker = new System.ComponentModel.BackgroundWorker();
			bg_worker.DoWork += (oo, ee) =>
			{
				comic_spider.Fix_display_pages(path);
				ee.Result = "Fix display pages completed.";
			};
			bg_worker.RunWorkerCompleted += (oo, ee) =>
			{
				if (ee.Error != null)
					this.Title = ee.Error.Message;
				else
					this.Title = ee.Result as string;

				MainWindow.Main.Show_balloon(this.Title, (ooo, eee) =>
				{
					try
					{
						System.Diagnostics.Process.Start(path);
					}
					catch (Exception ex)
					{
						Message_box.Show(ex.Message);
					}
				});
				btn.IsEnabled = true;
				working_icon.Hide_working();
			};
			this.Title = "Fixing display pages...";
			bg_worker.RunWorkerAsync();
		}
		public void btn_del_display_pages_Click(object sender, RoutedEventArgs e)
		{
			string path;
			if (volume_list.SelectedItem != null)
			{
				path = Get_direcotry(
					"Select the root folder for opertion",
					Directory.GetParent((volume_list.SelectedItem as Web_resource_info).Path).FullName
				);
			}
			else
			{
				path = Get_direcotry(
					"Select the root folder for opertion",
					txt_dir.Text
				);
			}

			if (string.IsNullOrEmpty(path))
				return;

			Control btn = sender as Control;
			btn.IsEnabled = false;

			working_icon.Show_working();

			System.ComponentModel.BackgroundWorker bg_worker = new System.ComponentModel.BackgroundWorker();
			bg_worker.DoWork += (oo, ee) =>
			{
				comic_spider.Delete_display_pages(path);
				ee.Result = "Delete display pages completed.";
			};
			bg_worker.RunWorkerCompleted += (oo, ee) =>
			{
				if (ee.Error != null)
					this.Title = ee.Error.Message;
				else
					this.Title = ee.Result as string;

				MainWindow.Main.Show_balloon(this.Title, (ooo, eee) =>
				{
					try
					{
						System.Diagnostics.Process.Start(path);
					}
					catch (Exception ex)
					{
						Message_box.Show(ex.Message);
					}
				});
				btn.IsEnabled = true;
				working_icon.Hide_working();
			};
			this.Title = "Deleting display pages...";
			bg_worker.RunWorkerAsync();
		}

		private void btn_start_Click(object sender, RoutedEventArgs e)
		{
			if (volume_list.Items.Count == 0)
				return;

			Update_settings();

			if (btn_start_state == Start_button_state.Start)
			{
				Is_all_downloaded = false;
				btn_start_state = Start_button_state.Stop;
				working_icon.Show_working();
				comic_spider.Async_start();
			}
			else
			{
				btn_start_state = Start_button_state.Start;
				comic_spider.Stop();
				working_icon.Hide_working();
			}
		}
		private void btn_get_list_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.Main.working_icon.Show_working();
			working_icon.Show_working();
			Update_settings();
			comic_spider.Async_get_volume_list();
		}

		private void btn_controller_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(comic_spider.Default_script_editor, "comic_spider.lua");
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message);
			}
		}
		private void btn_logs_Click(object sender, RoutedEventArgs e)
		{
			if (bd_logs.Visibility == Visibility.Collapsed)
			{
				bd_logs.Visibility = Visibility.Visible;
				(Resources["sb_show_logs"] as Storyboard).Begin();
			}
			else
			{
				(Resources["sb_hide_logs"] as Storyboard).Begin();
			}
		}
		private void sb_hide_logs_Completed(object sender, EventArgs e)
		{
			bd_logs.Visibility = Visibility.Collapsed;
		}

		private void btn_save_to_Click(object sender, RoutedEventArgs e)
		{
			string path = Get_direcotry("Please select a root folder");
			if (!string.IsNullOrEmpty(path))
				txt_dir.Text = path;
		}	

		private void View_Click(object sender, RoutedEventArgs e)
		{
			ListView list_view;
			if (sender is ListView)
			{
				list_view = sender as ListView;
			}
			else
			{
				MenuItem menu_item = sender as MenuItem;
				list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			}
			Web_resource_info item = list_view.SelectedItem as Web_resource_info;

			if(item == null) return;

			if (list_view == volume_list)
			{
				string file_path = Path.Combine(item.Path, "index.html");
				if (File.Exists(file_path))
					System.Diagnostics.Process.Start(file_path);
				else
					Message_box.Show("No view page found. Please wait a volume downloaded or fix the view page manually.");
			}
			else
			{
				if(File.Exists(item.Path))
					System.Diagnostics.Process.Start(item.Path);
				else
					Message_box.Show("No target file found.");
			}
		}
		private void ContextMenu_Opened(object sender, RoutedEventArgs e)
		{
		}
		private void Open_folder_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;

			foreach (Web_resource_info item in list_view.SelectedItems)
			{
				if (Directory.Exists(item.Path))
				{
					System.Diagnostics.Process.Start(item.Path);
					break;
				}
				else if (File.Exists(item.Path))
				{
					System.Diagnostics.Process.Start("explorer.exe", "/select," + item.Path);
					break;
				}
				else
				{
					Message_box.Show("Item doesn't exist.");
					break;
				}
			}
		}
		private void Open_url_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			foreach (Web_resource_info list_item in list_view.SelectedItems)
			{
				System.Diagnostics.Process.Start(list_item.Url);
			}
		}
		private void Copy_url_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			foreach (Web_resource_info list_item in list_view.SelectedItems)
			{
				Clipboard.SetText(list_item.Url);
				break;
			}
		}
		private void Resume_items_Click(object sender, RoutedEventArgs e)
		{
			ListView list_view;
			if (sender is ListView)
			{
				list_view = sender as ListView;
			}
			else
			{
				MenuItem menu_item = sender as MenuItem;
				list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			}

			if (list_view.SelectedItems.Count == 0)
				return;

			foreach (Web_resource_info item in list_view.SelectedItems)
			{
				if (item.State == Web_resource_state.Failed ||
					item.State == Web_resource_state.Paused)
				{
					item.State = Web_resource_state.Wait;

					if (item.Parent.State == Web_resource_state.Failed ||
						item.Parent.State == Web_resource_state.Paused)
					{

						item.Parent.State = Web_resource_state.Wait;
					}
				}
			}

			this.Title = "Item(s) resumed.";
		}
		private void Pause_items_Click(object sender, RoutedEventArgs e)
		{
			ListView list_view;
			if (sender is ListView)
			{
				list_view = sender as ListView;
			}
			else
			{
				MenuItem menu_item = sender as MenuItem;
				list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			}

			if (list_view.SelectedItems.Count == 0)
				return;

			foreach (Web_resource_info item in list_view.SelectedItems)
			{
				if (item.State != Web_resource_state.Downloaded &&
					item.State != Web_resource_state.Failed)
					item.State = Web_resource_state.Paused;
			}

			this.Title = "Item(s) paused.";
		}
		private void Delete_with_folder_Click(object sender, RoutedEventArgs e)
		{
			if (volume_list.SelectedItems.Count == 0)
				return;

			if (!Message_box.Show("All the files in the folder will be deleted permanently. Are you sure to delete?", true))
				return;

			List<Web_resource_info> selected_list = new List<Web_resource_info>();
			foreach (Web_resource_info item in volume_list.SelectedItems)
			{
				selected_list.Add(item);
			}
			foreach (var item in selected_list)
			{
				comic_spider.Manager.Volumes.Remove(item);
				try
				{
					Directory.Delete(item.Path, true);
				}
				catch (Exception ex)
				{
					this.Title = ex.Message;
				}
			}

			this.Title = "Item(s) deleted.";
			Report_main_progress();
		}
		private void Delete_with_parent_Click(object sender, RoutedEventArgs e)
		{
			if (volume_list.SelectedItems.Count == 0)
				return;

			if (!Message_box.Show("All the files in the folder will be deleted permanently. Are you sure to delete?", true))
				return;

			Web_resource_info item = volume_list.SelectedItem as Web_resource_info;
			try
			{
				List<Web_resource_info> list = new List<Web_resource_info>();
				foreach (var vol in (from vol in comic_spider.Manager.Volumes
												   where vol.Parent.Name == item.Parent.Name
												   select vol))
				{
					list.Add(vol);
				}

				foreach (Web_resource_info vol in list)
				{
					comic_spider.Manager.Volumes.Remove(vol);
				}
				Directory.Delete(Directory.GetParent(item.Path).FullName, true);
			}
			catch (Exception ex)
			{
				this.Title = ex.Message;
			}

			this.Title = "Item(s) deleted.";
			Report_main_progress();
		}
		private void Delelte_list_item_Click(object sender, RoutedEventArgs e)
		{
			ListView list_view;
			if (sender is ListView)
			{
				list_view = sender as ListView;
			}
			else
			{
				MenuItem menu_item = sender as MenuItem;
				list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			}

			if (list_view.SelectedItems.Count == 0)
				return;

			if (!Message_box.Show("Are you sure to delete?", true))
				return;

			if (list_view == volume_list)
			{
				List<Web_resource_info> selected_list = new List<Web_resource_info>();
				foreach (Web_resource_info item in list_view.SelectedItems)
				{
					selected_list.Add(item);
				}
				foreach (var item in selected_list)
				{
					comic_spider.Manager.Volumes.Remove(item);
				}
			}
			else
			{
				foreach (Web_resource_info item in list_view.SelectedItems)
				{
					item.Parent.Children.Remove(item);
				}
				volume_list_SelectionChanged(null, null);
			}

			this.Title = "Item(s) deleted.";
			Report_main_progress();
		}
		private void Delelte_list_item_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Delete)
			{
				Delelte_list_item_Click(sender, null);
			}
		}
		private void Delete_downloaded_Click(object sender, RoutedEventArgs e)
		{
			ListView list_view;
			if (sender is ListView)
			{
				list_view = sender as ListView;
			}
			else
			{
				MenuItem menu_item = sender as MenuItem;
				list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			}

			if (!Message_box.Show("Are you sure to delete?", true))
				return;

			var delete_list = new System.Collections.ObjectModel.Collection<Web_resource_info>();
			if (list_view == volume_list)
			{
				foreach (var vol in comic_spider.Manager.Volumes)
				{
					if (vol.State == Web_resource_state.Downloaded)
						delete_list.Add(vol);
				}
				foreach (var vol in delete_list)
				{
					comic_spider.Manager.Volumes.Remove(vol);
				}
			}
			else
			{
				foreach (Web_resource_info item in list_view.Items)
				{
					if (item.State == Web_resource_state.Downloaded)
						item.Parent.Children.Remove(item);
				}
				volume_list_SelectionChanged(null, null);
			}

			this.Title = "Item(s) deleted.";
			Report_main_progress();
		}
		private void btn_help_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.Main.Help(null, null);
		}
		private void Number_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			try
			{
				uint.Parse(e.Text);
			}
			catch
			{
				e.Handled = true;
			}
		}

		private void volume_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			page_list.Items.Clear();
			foreach (Web_resource_info vol in volume_list.SelectedItems)
			{
				if (vol.Count == 0) continue;
				foreach (var page in vol.Children)
				{
					page_list.Items.Add(page);
				}
			}
		}
		private void volume_list_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Web_resource_info item = (sender as ListView).SelectedItem as Web_resource_info;

			View_Click(sender, null);
		}

		private void GridView_column_header_Clicked(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
			ListView list_view = sender as ListView;
			List<Web_resource_info> list = new List<Web_resource_info>();

			if (header == null || header.Column == null) return;

			foreach (Web_resource_info item in list_view.Items)
			{
				list.Add(item);
			}

			string col_name = header.Column.Header as string;
			IOrderedEnumerable<Web_resource_info> temp_list = null;

			foreach (var col in (header.Parent as GridViewHeaderRowPresenter).Columns)
			{
				if (col != header.Column)
					col.HeaderTemplate = null;
			}

			DataTemplate arrow_up = Resources["HeaderTemplateArrowUp"] as DataTemplate;
			DataTemplate arrow_down = Resources["HeaderTemplateArrowDown"] as DataTemplate;

			switch (col_name)
			{
				case "S":
					if (header.Column.HeaderTemplate == arrow_up)
						temp_list = list.OrderByDescending(info => info.State);
					else
						temp_list = list.OrderBy(info => info.State);
					break;

				case "Progress int":
					if (header.Column.HeaderTemplate == arrow_up)
						temp_list = list.OrderByDescending(info => info.Progress_int_text);
					else
						temp_list = list.OrderBy(info => info.Progress_int_text);
					break;

				case "Progress":
					if (header.Column.HeaderTemplate == arrow_up)
						temp_list = list.OrderByDescending(info => info.Progress);
					else
						temp_list = list.OrderBy(info => info.Progress);
					break;

				case "Speed":
					if (header.Column.HeaderTemplate == arrow_up)
						temp_list = list.OrderByDescending(info => info.Speed);
					else
						temp_list = list.OrderBy(info => info.Speed);
					break;

				case "Size":
					if (header.Column.HeaderTemplate == arrow_up)
						temp_list = list.OrderByDescending(info => info.Size);
					else
						temp_list = list.OrderBy(info => info.Size);
					break;

				case "Main":
					if (header.Column.HeaderTemplate == arrow_up)
						temp_list = list.OrderByDescending(info => info.Parent.Name);
					else
						temp_list = list.OrderBy(info => info.Parent.Name);
					break;

				case "Name":
					if (header.Column.HeaderTemplate == arrow_up)
						temp_list = list.OrderByDescending(info => info.Name);
					else
						temp_list = list.OrderBy(info => info.Name);
					break;

				case "Url":
					if (header.Column.HeaderTemplate == arrow_up)
						temp_list = list.OrderByDescending(info => info.Url);
					else
						temp_list = list.OrderBy(info => info.Url);
					break;
			}

			if (header.Column.HeaderTemplate == arrow_up)
				header.Column.HeaderTemplate = arrow_down;
			else
				header.Column.HeaderTemplate = arrow_up;

			List<Web_resource_info> new_list = new List<Web_resource_info>();

			if (temp_list == null) return;
			foreach (var item in temp_list)
			{
				new_list.Add(item);
			}
			list = new_list;

			if (list_view.ItemsSource == null)
			{
				list_view.Items.Clear();
				foreach (var item in list)
				{
					list_view.Items.Add(item);
				}
			}
			else
			{
				comic_spider.Manager.Volumes.Clear();
				foreach (var item in list)
				{
					comic_spider.Manager.Volumes.Add(item);
				}
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			this.Visibility = System.Windows.Visibility.Collapsed;
			e.Cancel = true;
		}
		private void Window_Closed(object sender, EventArgs e)
		{
			comic_spider.Stop();
			Save_all();
		}

		private void ShowHide_window(object sender, RoutedEventArgs e)
		{
			if (this.Visibility == System.Windows.Visibility.Visible)
			{
				this.Visibility = System.Windows.Visibility.Collapsed;
				this.ShowInTaskbar = false;
			}
			else
			{
				this.Visibility = System.Windows.Visibility.Visible;
				this.ShowInTaskbar = true;
				this.Activate();
			}
		}
	}
}
