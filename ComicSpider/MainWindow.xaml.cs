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

		private Comic_spider comic_spider;
		private Settings settings;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			comic_spider = new Comic_spider();
			
			Init_settings();
			Init_vol_info_list();
			Init_page_info_list();

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
			DispatcherTimer auto_saver = new DispatcherTimer();
			auto_saver.Interval = TimeSpan.FromMinutes(5);
			auto_saver.Tick += new EventHandler(auto_saver_Tick);
			auto_saver.Start();
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
				settings = ys.Common.ByteArrayToObject(data_reader["Value"] as byte[]) as Settings;
			}

			kv_adpter.Connection.Close();

			if (settings == null) settings = new Settings();

			txt_dir.Text = settings.Root_dir;
			txt_url.Text = settings.Url;
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
						new Counter(0),
						row.Cookie,
						row.Name,
						new Web_src_info(row.Parent_url, 0, "", null, "", row.Parent_name));
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
							vol.Counter.Increase_all();
							vol.Children.Add(new Web_src_info(
								row.Url,
								row.Index,
								row.State,
								null,
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
			if (btn_start.Content.ToString() == "Start")
			{
				btn_start.Content = "Stop";
				btn_get_list.IsEnabled = false;

				settings.Root_dir = txt_dir.Text;
				settings.Url = txt_url.Text;

				comic_spider.Thread_count = int.Parse(txt_thread.Text);
				comic_spider.Async_start(vol_list.Items, txt_dir.Text);
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
			comic_spider.Async_show_vol_list(txt_url.Text);
			btn_get_list.IsEnabled = false;
		}
		private void btn_delelte_Click(object sender, RoutedEventArgs e)
		{
			while (vol_list.SelectedItems.Count > 0)
			{
				vol_list.Items.RemoveAt(vol_list.SelectedIndex);
			}
		}
		private void btn_select_downloaded_Click(object sender, RoutedEventArgs e)
		{
			vol_list.Focus();
			vol_list.SelectedIndex = -1;
			foreach (Web_src_info item in vol_list.Items)
			{
				if (item.State == "OK")
				{
					vol_list.SelectedItems.Add(item);
				}
			}
		}
		private void Open_url_Click(object sender, RoutedEventArgs e)
		{
			MenuItem item = sender as MenuItem;
			ListView list_view = (item.Parent as ContextMenu).PlacementTarget as ListView;
			foreach (Web_src_info vol in list_view.SelectedItems)
			{
				System.Diagnostics.Process.Start(vol.Url);
			}
		}
		private void Open_folder_Click(object sender, RoutedEventArgs e)
		{
			MenuItem item = sender as MenuItem;
			ListView list_view = (item.Parent as ContextMenu).PlacementTarget as ListView;
			try
			{
				foreach (Web_src_info vol in list_view.SelectedItems)
				{
					string path = "";
					Web_src_info parent = vol;
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
			List<Web_src_info> list = new List<Web_src_info>();
			foreach (Web_src_info vol in vol_list.SelectedItems)
			{
				if (vol.Children == null) continue;
				foreach (var page in vol.Children)
				{
					list.Add(page);
				}
			}
			Page_list.ItemsSource = list;
		}

		private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
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
			comic_spider.Stop();

			Save_all();

			Environment.Exit(0);
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			MessageBox.Show((e.ExceptionObject as Exception).Message);
			Window_Closed(null, null);
		}

		private void auto_saver_Tick(object sender, EventArgs e)
		{
			Save_all();
		}

		private void Save_all()
		{
			Save_settings();
			Save_vol_info_list();
			Save_page_info_list();
		}
		private void Save_settings()
		{
			settings.Root_dir = txt_dir.Text;
			settings.Url = txt_url.Text;
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
		private class Settings
		{
			public Settings()
			{
				Thread_count = "5";
			}

			public string Root_dir { get; set; }
			public string Url { get; set; }
			public string Thread_count { get; set; }
		}
	}
}
