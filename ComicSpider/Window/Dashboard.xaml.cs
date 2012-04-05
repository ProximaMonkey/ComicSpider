using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using ComicSpider.UserTableAdapters;
using ys.Web;
using System.Windows.Media.Animation;

namespace ComicSpider
{
	public partial class Dashboard : Window
	{
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

		public void Get_volume_list(string url)
		{
			txt_main_url.Text = url;
			if (string.IsNullOrEmpty(txt_dir.Text))
			{
				var dialog = new Ionic.Utils.FolderBrowserDialogEx();
				dialog.ShowFullPathInEditBox = true;
				dialog.Description = "Please select a folder to save the comic";
				if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					txt_dir.Text = dialog.SelectedPath;
					btn_get_list_Click(null, null);
				}
			}
			else
			{
				btn_get_list_Click(null, null);
			}
		}

		public string Main_progress
		{
			get 
			{
				int downloaded = 0;
				foreach (Web_src_info item in volume_list.Items)
				{
					if (item.State == Web_src_info.State_downloaded)
						downloaded++;
				}
				return string.Format("{0} / {1}", downloaded, volume_list.Items.Count);
			}
		}
		public new string Title
		{
			get { return base.Title; }
			set
			{
				base.Title = value;

				if (txt_console.LineCount >= Main_settings.Main.Max_console_line)
				{
					txt_console.Text = txt_console.Text.Remove(
												txt_console.GetCharacterIndexFromLineIndex(
													Main_settings.Main.Max_console_line / 2
												)
											);
				}
				txt_console.Text = value + '\n' + txt_console.Text;
			}
		}
		public bool All_downloaded { get; set; }

		public delegate void Show_vol_list_delegate(List<Web_src_info> list);
		public void Show_volume_list(List<Web_src_info> list)
		{
			List<Web_src_info> added_list = new List<Web_src_info>();
			foreach (Web_src_info item in list.Distinct(new Web_src_info.Comparer()))
			{
				foreach (Web_src_info vol in volume_list.Items)
				{
					if (vol.Url == item.Url)
						goto skip;
				}
				volume_list.Items.Add(item);
				added_list.Add(item);
			skip: ;
			}

			btn_start.IsEnabled = true;
			Hide_working();

			if (instance != null &&
				this.Visibility != System.Windows.Visibility.Visible)
			{
				if (comic_spider.Stopped)
				{
					btn_start_Click(null, null);
				}

				comic_spider.Add_volume_list(added_list);
			}
		}

		public delegate void Report_progress_delegate(string info);
		public void Report_progress(string info)
		{
			this.Title = info;
		}

		public delegate void Report_volume_progress_delegate();
		public void Report_volume_progress()
		{
			if (All_downloaded) return;

			int all_left = 0;
			foreach (Web_src_info vol in volume_list.Items)
			{
				if (vol.Children == null)
					continue;

				foreach (Web_src_info file in vol.Children)
				{
					if (file.State == Web_src_info.State_downloaded)
						continue;

					all_left++;
				}
			}
			if (all_left == 0)
			{
				All_downloaded = true;
				this.Title = "All completed.";
				MainWindow.Main.Show_balloon(this.Title);
				comic_spider.Stop(true);
				btn_start.Content = "Start";
				Hide_working();
			}

			Save_all();
		}

		public new void Close()
		{
			this.Closing -= Window_Closing;
			base.Close();
		}

		/***************************** Private ********************************/

		private static Dashboard instance;
		private Dashboard()
		{
			InitializeComponent();

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			sb_show_working = Resources["sb_show_working"] as Storyboard;
			sb_working = Resources["sb_working"] as Storyboard;
			sb_hide_working = Resources["sb_hide_working"] as Storyboard;
			sb_working.Begin();
			sb_working.Pause();

			comic_spider = new Comic_spider();

			Init_settings();
			Init_vol_info_list();
			Init_page_info_list();
		}

		private Comic_spider comic_spider;

		private void Init_settings()
		{
			Key_valueTableAdapter kv_adpter = new Key_valueTableAdapter();
			kv_adpter.Adapter.SelectCommand = kv_adpter.Connection.CreateCommand();
			kv_adpter.Adapter.SelectCommand.CommandText = "select * from Key_value where Key = 'Settings' limit 0,1";

			kv_adpter.Connection.Open();

			SQLiteDataReader data_reader = kv_adpter.Adapter.SelectCommand.ExecuteReader();
			if (data_reader.Read())
			{
				Main_settings.Main = ys.Common.ByteArrayToObject(data_reader["Value"] as byte[]) as Main_settings;
			}

			kv_adpter.Connection.Close();

			txt_main_url.Text = Main_settings.Main.Main_url;
			txt_dir.Text = Main_settings.Main.Root_dir;
			txt_thread.Text = Main_settings.Main.Thread_count;

			Main_settings.Main.Max_console_line = 300;
		}
		private void Init_vol_info_list()
		{
			Volume_listTableAdapter vol_adpter = new Volume_listTableAdapter();
			User.Volume_listDataTable vol_info_table = vol_adpter.GetData();
			List<Web_src_info> list = new List<Web_src_info>();

			if (vol_info_table.Count > 0)
			{
				var groups = vol_info_table.GroupBy(v => v.Parent_url);
				foreach (var group in groups)
				{
					Web_src_info comic = null;
					foreach (User.Volume_listRow row in group)
					{
						if (comic == null)
						{
							comic = new Web_src_info(row.Parent_url, 0, row.Parent_name);
							comic.Children = new List<Web_src_info>();
						}
						Web_src_info src_info = new Web_src_info(
							row.Url,
							row.Index,
							row.Name,
							comic);

						comic.Children.Add(src_info);
						list.Add(src_info);
					}
				}
				Show_volume_list(list);
			}
		}
		private void Init_page_info_list()
		{
			Page_listTableAdapter page_adpter = new Page_listTableAdapter();
			User.Page_listDataTable page_info_table = page_adpter.GetData();
			if (page_info_table.Count > 0)
			{
				var groups = page_info_table.GroupBy(p => p.Parent_url);
				foreach (var group in groups)
				{
					Web_src_info volume = null;
					foreach (User.Page_listRow row in group)
					{
						if (volume == null)
						{
							foreach (Web_src_info vol in volume_list.Items)
							{
								if (vol.Url == row.Parent_url)
									volume = vol;
							}
							volume.Children = new List<Web_src_info>();
						}

						volume.Children.Add(new Web_src_info(
							row.Url,
							row.Index,
							row.Name,
							volume)
						{
							State = row.State,
						}
						);
						volume.Cookie = row.Parent_cookie;
						if (volume.Downloaded == volume.Count)
						{
							volume.State = Web_src_info.State_downloaded;
						}
						else
						{
							volume.State = string.Format("{0} / {1}", volume.Downloaded, volume.Count);
						}
					}
				}
			}
		}

		private void btn_start_Click(object sender, RoutedEventArgs e)
		{
			Save_all();

			if (btn_start.Content.ToString() == "Start")
			{
				All_downloaded = false;
				btn_start.Content = "Stop";
				comic_spider.Async_start(volume_list.Items);
				Show_working();
			}
			else
			{
				btn_start.Content = "Start";
				comic_spider.Stop();
				Hide_working();
			}
		}
		private void btn_get_list_Click(object sender, RoutedEventArgs e)
		{
			Show_working();
			Save_settings();
			comic_spider.Async_get_volume_list();
		}

		private void btn_select_downloaded_Click(object sender, RoutedEventArgs e)
		{
			volume_list.Focus();
			volume_list.SelectedIndex = -1;
			foreach (Web_src_info item in volume_list.Items)
			{
				if (item.State == Web_src_info.State_downloaded)
				{
					volume_list.SelectedItems.Add(item);
				}
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

		private void btn_fix_display_pages_Click(object sender, RoutedEventArgs e)
		{
			btn_fix_display_pages.IsEnabled = false;

			Update_settings();

			System.ComponentModel.BackgroundWorker bg_worker = new System.ComponentModel.BackgroundWorker();
			bg_worker.DoWork += (oo, ee) =>
			{
				try
				{
					comic_spider.Fix_display_pages(Main_settings.Main.Root_dir);
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
				MainWindow.Main.Show_balloon(this.Title);
				btn_fix_display_pages.IsEnabled = true;
			};
			bg_worker.RunWorkerAsync();
		}
		private void btn_del_display_pages_Click(object sender, RoutedEventArgs e)
		{
			btn_del_display_pages.IsEnabled = false;

			System.ComponentModel.BackgroundWorker bg_worker = new System.ComponentModel.BackgroundWorker();
			bg_worker.DoWork += (oo, ee) =>
			{
				try
				{
					comic_spider.Delete_display_pages();
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
				MainWindow.Main.Show_balloon(this.Title);
				btn_del_display_pages.IsEnabled = true;
			};
			bg_worker.RunWorkerAsync();
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

		private void btn_controller_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start("notepad.exe", "comic_spider.lua");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void View_volume_Click(object sender, RoutedEventArgs e)
		{
			Update_settings();
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			try
			{
				foreach (Web_src_info list_item in list_view.SelectedItems)
				{
					string path = "";
					Web_src_info parent = list_item;
					while ((parent = parent.Parent) != null)
					{
						path = Path.Combine(parent.Name, path);
					}
					path = Path.Combine(Main_settings.Main.Root_dir, path);

					string file_path = Path.Combine(path, "index.html");
					if (File.Exists(file_path))
					{
						System.Diagnostics.Process.Start(file_path);
						return;
					}

					file_path = Path.Combine(Path.Combine(path, list_item.Name), "index.html");
					if (File.Exists(file_path))
						System.Diagnostics.Process.Start(file_path);
					else
						MessageBox.Show("No view page found.");
					break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		private void Copy_name_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			foreach (Web_src_info list_item in list_view.SelectedItems)
			{
				Clipboard.SetText(list_item.Name);
				break;
			}
		}
		private void Copy_url_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			foreach (Web_src_info list_item in list_view.SelectedItems)
			{
				Clipboard.SetText(list_item.Url);
				break;
			}
		}
		private void Open_url_Click(object sender, RoutedEventArgs e)
		{
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			foreach (Web_src_info list_item in list_view.SelectedItems)
			{
				System.Diagnostics.Process.Start(list_item.Url);
			}
		}
		private void Open_folder_Click(object sender, RoutedEventArgs e)
		{
			Update_settings();
			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;
			try
			{
				foreach (Web_src_info list_item in list_view.SelectedItems)
				{
					string path = "";
					Web_src_info parent = list_item;
					while ((parent = parent.Parent) != null)
					{
						path = Path.Combine(parent.Name, path);
					}
					path = Path.Combine(Main_settings.Main.Root_dir, path);

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
				MessageBox.Show(ex.Message);
			}
		}
		private void Delelte_list_item_Click(object sender, RoutedEventArgs e)
		{
			if (!comic_spider.Stopped)
			{
				MessageBox.Show("Stop downloading before deleting.");
				return;
			}

			MenuItem menu_item = sender as MenuItem;
			ListView list_view = (menu_item.Parent as ContextMenu).PlacementTarget as ListView;

			while (list_view.SelectedItems.Count > 0)
			{
				int index = list_view.SelectedIndex;
				list_view.Items.RemoveAt(index);
			}
		}
		private void Delelte_list_item_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Delete)
			{
				ListView list_view = sender as ListView;
				while (list_view.SelectedItems.Count > 0)
				{
					int index = list_view.SelectedIndex;
					list_view.Items.RemoveAt(index);
				}
			}
		}
		private void btn_help_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start("Comic Spider.chm");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private Storyboard sb_show_working;
		private Storyboard sb_working;
		private Storyboard sb_hide_working;
		private void Show_working()
		{
			sb_show_working.Begin();
			sb_working.Resume();
		}
		private void Hide_working()
		{
			sb_hide_working.Begin();
			sb_working.Pause();
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
			Page_list.Items.Clear();
			foreach (Web_src_info vol in volume_list.SelectedItems)
			{
				if (vol.Children == null) continue;
				foreach (var page in vol.Children)
				{
					Page_list.Items.Add(page);
				}
			}
		}
		private void volume_list_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			volume_list_SelectionChanged(null, null);
		}

		private void GridView_column_header_Clicked(object sender, RoutedEventArgs e)
		{

			GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
			ListView clicked_view = sender as ListView;
			List<Web_src_info> list = new List<Web_src_info>();

			if (header == null) return;

			foreach (Web_src_info item in clicked_view.Items)
			{
				list.Add(item);
			}

			string col_name = header.Column.Header as string;
			IOrderedEnumerable<Web_src_info> temp_list;

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
					if (col_name == "Comic")
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
					if (col_name == "Comic")
						return info.Parent.Name;
					else
						return info.GetType().GetProperty(col_name).GetValue(info, null);
				});
			}


			List<Web_src_info> new_list = new List<Web_src_info>();
			foreach (var item in temp_list)
			{
				new_list.Add(item);
			}
			list = new_list;

			clicked_view.Items.Clear();
			foreach (var item in list)
			{
				clicked_view.Items.Add(item);
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

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			MessageBox.Show(ex.Message + '\n' + ex.InnerException.StackTrace);
			Window_Closed(null, null);
		}

		private void Save_all()
		{
			Save_settings();
			Save_vol_info_list();
			Save_page_info_list();
		}
		private void Update_settings()
		{
			Main_settings.Main.Main_url = txt_main_url.Text;
			Main_settings.Main.Root_dir = txt_dir.Text;
			Main_settings.Main.Thread_count = txt_thread.Text;
			Main_settings.Main.Latest_volume_only = MainWindow.Main.Latest_volume_only;
		}
		private void Save_settings()
		{
			Update_settings();

			Key_valueTableAdapter kv_adapter = new Key_valueTableAdapter();
			kv_adapter.Adapter.UpdateCommand = kv_adapter.Connection.CreateCommand();
			kv_adapter.Adapter.UpdateCommand.CommandText = "update Key_value set [Value] = @value where [Key] = 'Settings'";
			kv_adapter.Adapter.UpdateCommand.Parameters.AddWithValue("@value", ys.Common.ObjectToByteArray(Main_settings.Main));

			kv_adapter.Connection.Open();

			kv_adapter.Adapter.UpdateCommand.ExecuteNonQuery();

			kv_adapter.Connection.Close();
		}
		private void Save_vol_info_list()
		{
			Volume_listTableAdapter vol_adapter = new Volume_listTableAdapter();
			vol_adapter.Adapter.DeleteCommand = vol_adapter.Connection.CreateCommand();
			vol_adapter.Adapter.DeleteCommand.CommandText = "delete from Volume_list where 1";

			vol_adapter.Connection.Open();

			SQLiteTransaction transaction = vol_adapter.Connection.BeginTransaction();

			vol_adapter.Adapter.DeleteCommand.ExecuteNonQuery();

			foreach (Web_src_info item in volume_list.Items)
			{
				if (item.Parent == null) continue;

				vol_adapter.Insert(
					item.Url,
					item.Name,
					item.Index,
					item.State,
					item.Parent.Url,
					item.Parent.Name,
					item.Parent.Cookie + "",
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

			foreach (Web_src_info vol in volume_list.Items)
			{
				if (vol.Children == null) continue;
				foreach (Web_src_info item in vol.Children.Distinct(new Web_src_info.Comparer()))
				{
					page_adapter.Insert(
											item.Url,
											item.Name,
											item.Index,
											item.State,
											vol.Url,
											vol.Name,
											item.Parent.Cookie + "",
											DateTime.Now
										);
				}
			}

			transaction.Commit();
			page_adapter.Connection.Close();
		}
	}
}
