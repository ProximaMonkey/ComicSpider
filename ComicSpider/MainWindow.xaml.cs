using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ComicSpider.App_dataTableAdapters;
using ys.Web;
using System.Windows.Threading;
using System.IO;
using System.Windows.Media;

namespace ComicSpider
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			App_data.CheckAndFix();
			Main = this;
		}

		public static MainWindow Main;

		public delegate void Show_vol_list_delegate(List<Web_src_info> list);
		public void Show_vol_list(List<Web_src_info> list)
		{
			foreach (Web_src_info item in list)
			{
				foreach (Web_src_info vol in vol_list.Items)
				{
					if (vol.Url == item.Url)
						goto contains;
				}
				vol_list.Items.Add(item);
			contains: ;
			}

			btn_get_list.IsEnabled = true;
			btn_start.IsEnabled = true;

		}

		public delegate void Report_progress_delegate(string info);
		public void Report_progress(string info)
		{
			this.Title = info;
		}

		public MainSettings Settings
		{
			get { return settings; }
		}

		private Comic_spider comic_spider;
		private MainSettings settings;
		private new string Title
		{
			get { return base.Title; }
			set
			{
				base.Title = value;
				tray.ToolTipText = value;
			}
		}
		private Tray_balloon tray_balloon;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			comic_spider = new Comic_spider();
			
			Init_settings();
			Init_vol_info_list();
			Init_page_info_list();

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			DispatcherTimer monitor = new DispatcherTimer();
			monitor.Interval = TimeSpan.FromMinutes(3);
			monitor.Tick += new EventHandler(Monitor_Tick);
			monitor.Start();
		}

		private void Init_settings()
		{
			Key_valueTableAdapter kv_adpter = new Key_valueTableAdapter();
			kv_adpter.Adapter.SelectCommand = kv_adpter.Connection.CreateCommand();
			kv_adpter.Adapter.SelectCommand.CommandText = "select * from Key_value where Key = 'Settings' limit 0,1";

			kv_adpter.Connection.Open();

			SQLiteDataReader data_reader = kv_adpter.Adapter.SelectCommand.ExecuteReader();
			if (data_reader.Read())
			{
				settings = ys.Common.ByteArrayToObject(data_reader["Value"] as byte[]) as MainSettings;
			}

			kv_adpter.Connection.Close();

			if (settings == null) settings = new MainSettings();

			txt_main_url.Text = settings.Main_url;
			txt_vol.Text = settings.Vol_url;
			txt_page.Text = settings.Page_url;
			txt_file.Text = settings.File_url;
			txt_threshold_vol.Text = settings.Threshold_vol + "";
			txt_threshold_page.Text = settings.Threshold_page + "";
			txt_threshold_file.Text = settings.Threshold_file + "";
			txt_dir.Text = settings.Root_dir;
			txt_thread.Text = settings.Thread_count;
		}
		private void Init_vol_info_list()
		{
			Vol_infoTableAdapter vol_adpter = new Vol_infoTableAdapter();
			App_data.Vol_infoDataTable vol_info_table = vol_adpter.GetData();
			if (vol_info_table.Count > 0)
			{
				List<Web_src_info> list = new List<Web_src_info>();
				foreach (App_data.Vol_infoRow row in vol_info_table.Rows)
				{
					Web_src_info src_info = new Web_src_info(
						row.Url,
						row.Index,
						row.State,
						row.Cookie,
						row.Name,
						new Web_src_info(row.Parent_url, 0, "", "", row.Parent_name));
					src_info.Children = new List<Web_src_info>();
					list.Add(src_info);
				}
				Show_vol_list(list);
			}
		}
		private void Init_page_info_list()
		{
			Page_infoTableAdapter page_adpter = new Page_infoTableAdapter();
			App_data.Page_infoDataTable page_info_table = page_adpter.GetData();
			if (page_info_table.Count > 0)
			{
				foreach (Web_src_info vol in vol_list.Items)
				{
					foreach (var row in page_info_table)
					{
						if (row.Parent_url == vol.Url)
						{
							vol.Children.Add(new Web_src_info(
								row.Url,
								row.Index,
								row.State,
								row.Cookie,
								row.Name,
								vol)
							);
						}
					}
				}
			}
		}

		private void btn_start_Click(object sender, RoutedEventArgs e)
		{
			Save_settings();

			if (btn_start.Content.ToString() == "Start")
			{
				btn_start.Content = "Stop";
				btn_get_list.IsEnabled = false;

				comic_spider.Async_start(vol_list.Items);
			}
			else
			{
				btn_start.Content = "Start";
				btn_get_list.IsEnabled = true;
				comic_spider.Stop();
			}
		}
		private void btn_get_list_Click(object sender, RoutedEventArgs e)
		{
			Save_settings();
			comic_spider.Async_show_vol_list();
			btn_get_list.IsEnabled = false;
		}
		private void btn_select_downloaded_Click(object sender, RoutedEventArgs e)
		{
			vol_list.Focus();
			vol_list.SelectedIndex = -1;
			foreach (Web_src_info item in vol_list.Items)
			{
				if (item.State == Web_src_info.State_downloaded)
				{
					vol_list.SelectedItems.Add(item);
				}
			}
		}
		private void tray_TrayLeftMouseDown(object sender, RoutedEventArgs e)
		{
			tray.ContextMenu.IsOpen = true;
		}

		private void Show_balloon()
		{
			tray_balloon = new Tray_balloon();
			tray_balloon.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler((oo, ee) =>
			{
				tray_balloon.Visibility = System.Windows.Visibility.Collapsed;
			});
			tray.ShowCustomBalloon(tray_balloon, System.Windows.Controls.Primitives.PopupAnimation.Slide, 5000);
			tray_balloon.Text = this.Title;
		}
		private void tray_TrayToolTipOpen(object sender, RoutedEventArgs e)
		{
			int downloaded = 0;
			foreach (Web_src_info item in vol_list.Items)
			{
				if (item.State == Web_src_info.State_downloaded)
					downloaded++;
			}
			txt_main_progress.Text = string.Format("Progress: {0}/{1}", downloaded, vol_list.Items.Count);
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
		private void Close(object sender, RoutedEventArgs e)
		{
			this.Close();
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
					path = Path.Combine(settings.Root_dir, path);
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

		private void vol_list_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Page_list.Items.Clear();
			foreach (Web_src_info vol in vol_list.SelectedItems)
			{
				if (vol.Children == null) continue;
				foreach (var page in vol.Children)
				{
					Page_list.Items.Add(page);
				}
			}
		}

		private void GridView_column_header_Clicked(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader header = e.OriginalSource as GridViewColumnHeader;
			ListView clicked_view = sender as ListView;
			List<Web_src_info> list = new List<Web_src_info>();

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
				temp_list = list.OrderByDescending(info => info.GetType().GetProperty(col_name).GetValue(info, null));			
			}
			else
			{
				header.Column.HeaderTemplate = arrow_up;
				temp_list = list.OrderBy(info => info.GetType().GetProperty(col_name).GetValue(info, null));
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

		private void Window_Closed(object sender, EventArgs e)
		{
			tray.Visibility = System.Windows.Visibility.Collapsed;

			comic_spider.Stop();

			Save_all();

			Environment.Exit(0);
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			MessageBox.Show(ex.Message + '\n' + ex.InnerException.StackTrace);
			Window_Closed(null, null);
		}

		private void Monitor_Tick(object sender, EventArgs e)
		{
			if (comic_spider.Stopped) return;

			if (Try_download_missed_files() == 0)
			{
				MediaPlayer mplayer = new MediaPlayer();
				mplayer.Open(new Uri(@"Audio\msg.wav", UriKind.Relative));
				mplayer.Play();

				this.Title = "All completed.";
				Show_balloon();
				comic_spider.Stop();
			}

			Save_all();
		}
		private int Try_download_missed_files()
		{
			int all_left = 0;
			int missed = 0;
			foreach (Web_src_info vol in vol_list.Items)
			{
				if (vol.State == Web_src_info.State_downloaded ||
					vol.Children == null)
					continue;

				foreach (Web_src_info file in vol.Children)
				{
					if (file.State == Web_src_info.State_downloaded)
						continue;

					if (file.State == Web_src_info.State_missed)
						missed++;

					all_left++;
				}
			}
			if (missed != 0 &&
				missed == all_left)
			{
				this.Title = "Try to download missed files.";
				Show_balloon();

				comic_spider.Stop();
				comic_spider.Async_start(vol_list.Items);
			}

			return all_left;
		}

		private void Save_all()
		{
			Save_settings();
			Save_vol_info_list();
			Save_page_info_list();
		}
		private void Save_settings()
		{
			settings.Main_url = txt_main_url.Text;
			settings.Vol_url = txt_vol.Text;
			settings.Page_url = txt_page.Text;
			settings.File_url = txt_file.Text;
			settings.Threshold_vol = int.Parse(txt_threshold_vol.Text);
			settings.Threshold_page = int.Parse(txt_threshold_page.Text);
			settings.Threshold_file = int.Parse(txt_threshold_file.Text);
			settings.Root_dir = txt_dir.Text;
			settings.Thread_count = txt_thread.Text;

			Key_valueTableAdapter kv_adapter = new Key_valueTableAdapter();
			kv_adapter.Adapter.UpdateCommand = kv_adapter.Connection.CreateCommand();
			kv_adapter.Adapter.UpdateCommand.CommandText = "update Key_value set [Value] = @value where [Key] = 'Settings'";
			kv_adapter.Adapter.UpdateCommand.Parameters.AddWithValue("@value", ys.Common.ObjectToByteArray(settings));

			kv_adapter.Connection.Open();

			kv_adapter.Adapter.UpdateCommand.ExecuteNonQuery();

			kv_adapter.Connection.Close();
		}
		private void Save_vol_info_list()
		{
			Vol_infoTableAdapter vol_adapter = new Vol_infoTableAdapter();
			vol_adapter.Adapter.DeleteCommand = vol_adapter.Connection.CreateCommand();
			vol_adapter.Adapter.DeleteCommand.CommandText = "delete from Vol_info where 1";

			vol_adapter.Connection.Open();

			SQLiteTransaction transaction = vol_adapter.Connection.BeginTransaction();

			vol_adapter.Adapter.DeleteCommand.ExecuteNonQuery();

			foreach (Web_src_info item in vol_list.Items)
			{
				vol_adapter.Insert(
					item.Url,
					item.Name,
					item.State,
					item.Parent.Url,
					item.Parent.Name,
					item.Index,
					item.Cookie,
					DateTime.Now
				);
			}

			transaction.Commit();

			vol_adapter.Connection.Close();
		}
		private void Save_page_info_list()
		{
			Page_infoTableAdapter page_adapter = new Page_infoTableAdapter();
			page_adapter.Adapter.DeleteCommand = page_adapter.Connection.CreateCommand();
			page_adapter.Adapter.DeleteCommand.CommandText = "delete from Page_info where 1";

			page_adapter.Connection.Open();

			SQLiteTransaction transaction = page_adapter.Connection.BeginTransaction();

			page_adapter.Adapter.DeleteCommand.ExecuteNonQuery();

			foreach (Web_src_info vol in vol_list.Items)
			{
				if (vol.Children == null) continue;
				foreach (Web_src_info item in vol.Children)
				{
					page_adapter.Insert(
											item.Url,
											item.Name,
											item.State,
											vol.Url,
											vol.Name,
											item.Index,
											item.Cookie,
											DateTime.Now
										);
				}
			}

			transaction.Commit();
			page_adapter.Connection.Close();
		}

		[Serializable]
		public class MainSettings
		{
			public MainSettings()
			{
				Thread_count = "5";
				Threshold_vol = 10;
				Threshold_page = 10;
				Threshold_file = 20;
			}

			public string Main_url { get; set; }
			public string Vol_url { get; set; }
			public int Threshold_vol { get; set; }
			public string Page_url { get; set; }
			public int Threshold_page { get; set; }
			public string File_url { get; set; }
			public int Threshold_file { get; set; }
			public string Root_dir { get; set; }
			public string Thread_count { get; set; }
		}
	}
}
