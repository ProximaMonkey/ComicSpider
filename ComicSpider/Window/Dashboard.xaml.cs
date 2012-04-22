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

				MainWindow.Main.Taskbar.ChangeProcessValue((ulong)count, (ulong)all);

				return string.Format("{0} / {1}", count, all);
			}
		}

		public bool All_downloaded { get; set; }

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

		public void Update_settings()
		{
			Main_settings.Instance.Main_url = txt_main_url.Text;
			Main_settings.Instance.Root_dir = txt_dir.Text;
			Main_settings.Instance.Thread_count = txt_thread.Text;
		}
		public void Save_all()
		{
			Update_settings();

			try
			{
				Save_vol_info_list();
				Save_page_info_list();
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

		public delegate void Show_volume_list_delegate();
		public void Show_volume_list()
		{
			if(comic_spider.Stopped)
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

		public delegate void Show_supported_sites_delegate(List<Website_info> list);
		public void Show_supported_sites(List<Website_info> list)
		{
			var label = cb_supported_websites.Items[0];
			cb_supported_websites.Items.Clear();
			cb_supported_websites.Items.Add(label);
			cb_supported_websites.SelectedIndex = 0;
			foreach (var item in list)
			{
				cb_supported_websites.Items.Add(
					new ComboBoxItem() { Content = item.Name, Tag = item.Home });
			}
		}

		public delegate void Report_progress_delegate(string info);
		public void Report_progress(string info)
		{
			this.Title = info;
		}

		public delegate void Report_main_progress_delegate();
		public void Report_main_progress()
		{
			if (All_downloaded) return;

			MainWindow.Main.Main_progress = this.Main_progress;

			if (downloaded_files_count == all_files_count)
			{
				All_downloaded = true;

				MainWindow.Main.Taskbar.FlashTaskBar(ys.Win7.FlashOption.FLASHW_ALL);

				comic_spider.Stop(true);
				btn_start.Content = "Start";
				working_icon.Hide_working();

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

		public delegate void Stop_downloading_delegate(string info);
		public void Stop_downloading(string info)
		{
			this.Title = info;
			MainWindow.Main.Show_balloon(this.Title, (o, e) =>
			{
				this.Show();
			}, true);

			comic_spider.Stop(true);
			btn_start.Content = "Start";
			working_icon.Hide_working();
		}

		public void btn_fix_display_pages_Click(object sender, RoutedEventArgs e)
		{
			string path = Get_direcotry("Selet the root folder for opertion");
			if (string.IsNullOrEmpty(path)) return;

			Control btn = sender as Control;
			btn.IsEnabled = false;
			
			working_icon.Show_working();

			System.ComponentModel.BackgroundWorker bg_worker = new System.ComponentModel.BackgroundWorker();
			bg_worker.DoWork += (oo, ee) =>
			{
				try
				{
					comic_spider.Fix_display_pages(path);
				}
				catch (Exception ex)
				{
					ee.Result = ex.Message;
				}
				ee.Result = "Fix display pages completed.";
			};
			bg_worker.RunWorkerCompleted += (oo, ee) =>
			{
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
			bg_worker.RunWorkerAsync();
		}
		public void btn_del_display_pages_Click(object sender, RoutedEventArgs e)
		{
			string path = Get_direcotry("Selet the root folder for opertion");
			if (string.IsNullOrEmpty(path))
				return;

			Control btn = sender as Control;
			btn.IsEnabled = false;

			working_icon.Show_working();

			System.ComponentModel.BackgroundWorker bg_worker = new System.ComponentModel.BackgroundWorker();
			bg_worker.DoWork += (oo, ee) =>
			{
				try
				{
					comic_spider.Delete_display_pages(path);
				}
				catch (Exception ex)
				{
					ee.Result = ex.Message;
				}
				ee.Result = "Delete display pages completed.";
			};
			bg_worker.RunWorkerCompleted += (oo, ee) =>
			{
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
			bg_worker.RunWorkerAsync();
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
			txt_thread.Text = Main_settings.Instance.Thread_count;

			comic_spider.Manager.Stop();

			volume_list.ItemsSource = comic_spider.Manager.Volumes;
			new WPF.JoshSmith.ServiceProviders.UI.ListViewDragDropManager<Web_resource_info>(volume_list);

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
					switch(vol.State)
					{
						case Web_resource_state.Stopped:
						case Web_resource_state.Downloading:
						case Web_resource_state.Downloaded:
							count += vol.Count;
							break;

						case Web_resource_state.Wait:
						case Web_resource_state.Failed:
							count++;
							break;
					}
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

		/**************** Data ****************/

		private void Init_info_list()
		{
			Volume_listTableAdapter vol_adpter = new Volume_listTableAdapter();
			User.Volume_listDataTable vol_info_table = vol_adpter.GetData();

			if (vol_info_table.Count > 0)
			{
				var groups = vol_info_table.GroupBy(v => v.Parent_url);
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
							comic);

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
			User.Page_listDataTable page_info_table = page_adpter.GetData();
			if (page_info_table.Count > 0)
			{
				var groups = page_info_table.GroupBy(p => p.Parent_url);
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

						volume.Children.Add(new Web_resource_info(
							row.Url,
							row.Index,
							row.Name,
							row.Path,
							volume)
							{
								State_text = row.State,
								Size = row.Size,
							}
						);

						if (row.State == "X")
							volume.State = Web_resource_state.Failed;
						else if (volume.Downloaded == volume.Count)
						{
							volume.State = Web_resource_state.Downloaded;
						}
						else
						{
							volume.State_text = string.Format("{0} / {1}", volume.Downloaded, volume.Count);
						}
					}
				}
			}
		}
		private void Save_vol_info_list()
		{
			Volume_listTableAdapter vol_adapter = new Volume_listTableAdapter();
			vol_adapter.Adapter.DeleteCommand = vol_adapter.Connection.CreateCommand();
			vol_adapter.Adapter.DeleteCommand.CommandText = "delete from Volume_list where 1";

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
					item.State_text,
					item.Parent.Url,
					item.Parent.Name,
					item.Path,
					DateTime.Now
				);
			}

			transaction.Commit();

			vol_adapter.Connection.Close();
		}
		private void Save_page_info_list()
		{
			Page_listTableAdapter page_adapter = new Page_listTableAdapter();
			page_adapter.Adapter.DeleteCommand = page_adapter.Connection.CreateCommand();
			page_adapter.Adapter.DeleteCommand.CommandText = "delete from Page_list where 1";

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
											item.State_text,
											item.Size,
											vol.Url,
											vol.Name,
											item.Path,
											DateTime.Now
										);
				}
			}

			transaction.Commit();
			page_adapter.Connection.Close();
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

		private void btn_start_Click(object sender, RoutedEventArgs e)
		{
			if (volume_list.Items.Count == 0)
				return;

			Update_settings();

			if (btn_start.Content.ToString() == "Start")
			{
				All_downloaded = false;
				btn_start.Content = "Stop";
				working_icon.Show_working();
				comic_spider.Async_start();
			}
			else
			{
				btn_start.Content = "Start";
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

		private string Get_direcotry(string info)
		{
			string path = null;

			bool topmost_temp = MainWindow.Main.Topmost;
			MainWindow.Main.Topmost = false;

			var dialog = new Ionic.Utils.FolderBrowserDialogEx();
			dialog.ShowFullPathInEditBox = true;
			dialog.SelectedPath = txt_dir.Text;
			dialog.Description = info;
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
				Directory.Exists(dialog.SelectedPath))
			{
				path = dialog.SelectedPath;
			}

			MainWindow.Main.Topmost = topmost_temp;

			return path;
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

			if (File.Exists(item.Path))
				System.Diagnostics.Process.Start(item.Path);
			else
			{
				string file_path = Path.Combine(item.Path, "index.html");
				if (File.Exists(file_path))
					System.Diagnostics.Process.Start(file_path);
				else
				{
					Message_box.Show("No target file found.");
				}
			}
		}
		private void Open_folder_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			try
			{
				foreach (Web_resource_info list_item in list_view.SelectedItems)
				{
					string path = "";
					Web_resource_info parent = list_item;
					while ((parent = parent.Parent) != null)
					{
						path = Path.Combine(parent.Name, path);
					}
					path = Path.Combine(Main_settings.Instance.Root_dir, path);

					string item_dir = Path.Combine(path, list_item.Name);
					if (Directory.Exists(item_dir))
						System.Diagnostics.Process.Start(item_dir);
					else
						System.Diagnostics.Process.Start(path);
					break;
				}
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message);
			}
		}
		private void Open_url_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			foreach (Web_resource_info list_item in list_view.SelectedItems)
			{
				System.Diagnostics.Process.Start(list_item.Parent.Url);
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

			if (!Message_box.Show("Are you sure to delete?"))
				return;

			if (list_view == volume_list)
			{
				while (list_view.SelectedItems.Count > 0)
				{
					comic_spider.Manager.Volumes.RemoveAt(list_view.SelectedIndex);
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

			MainWindow.Main.Main_progress = this.Main_progress;
		}
		private void Delelte_list_item_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Delete)
			{
				Delelte_list_item_Click(sender, null);
			}
		}
		private void btn_help_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.Main.Help(null, null);
		}
		private void Thread_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
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
		private void volume_list_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			volume_list_SelectionChanged(null, null);
		}
		private void volume_list_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			Web_resource_info item = (sender as ListView).SelectedItem as Web_resource_info;

			View_Click(sender, null);
		}

		private void btn_select_downloaded_Click(object sender, RoutedEventArgs e)
		{
			volume_list.Focus();
			volume_list.SelectedIndex = -1;
			foreach (Web_resource_info item in volume_list.Items)
			{
				if (item.State == Web_resource_state.Downloaded)
				{
					volume_list.SelectedItems.Add(item);
				}
			}
		}
		private void cb_supported_websites_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBoxItem item = cb_supported_websites.SelectedValue as ComboBoxItem;

			if (item == null ||
				string.IsNullOrEmpty(item.Tag as string))
				return;
			try
			{
				System.Diagnostics.Process.Start(item.Tag as string);
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message);
			}
			cb_supported_websites.SelectedIndex = 0;
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
			IOrderedEnumerable<Web_resource_info> temp_list;

			foreach (var col in (header.Parent as GridViewHeaderRowPresenter).Columns)
			{
				if (col != header.Column)
					col.HeaderTemplate = null;
			}

			DataTemplate arrow_up = Resources["HeaderTemplateArrowUp"] as DataTemplate;
			DataTemplate arrow_down = Resources["HeaderTemplateArrowDown"] as DataTemplate;
			if (header.Column.HeaderTemplate == arrow_up)
			{
				header.Column.HeaderTemplate = arrow_down;
				temp_list = list.OrderByDescending(info =>
				{
					if (col_name == "Main")
						return info.Parent.Name;
					else
						return info.GetType().GetProperty(col_name).GetValue(info, null);
				});
			}
			else
			{
				header.Column.HeaderTemplate = arrow_up;
				temp_list = list.OrderBy(info =>
				{
					if (col_name == "Main")
						return info.Parent.Name;
					else
						return info.GetType().GetProperty(col_name).GetValue(info, null);
				});
			}

			List<Web_resource_info> new_list = new List<Web_resource_info>();
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
