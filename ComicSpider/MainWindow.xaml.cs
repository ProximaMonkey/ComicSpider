using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ys.Web;
using System.Xml.Serialization;
using System.IO;
using ComicSpider.App_dataTableAdapters;
using System.Data.SQLite;
using System.Linq;
using System.Collections.ObjectModel;

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

			vol_info_list = new ObservableCollection<Web_src_info>();
			vol_list.ItemsSource = vol_info_list;
		}

		public static MainWindow Main;

		public delegate void Show_vol_list_delegate(ObservableCollection<Web_src_info> list);
		public void Show_vol_list(ObservableCollection<Web_src_info> list)
		{
			foreach (Web_src_info item in list)
			{
				foreach (var vol in vol_info_list)
				{
					if (vol.Url == item.Url)
						goto contains;
				}
				vol_info_list.Add(item);
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
		private ObservableCollection<Web_src_info> vol_info_list;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			comic_spider = new Comic_spider();
			
			Init_settings();
			Init_vol_info_list();
			Init_page_info_list();
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
			Counter vol_info_counter = new Counter(vol_info_table.Count);
			if (vol_info_table.Count > 0)
			{
				ObservableCollection<Web_src_info> list = new ObservableCollection<Web_src_info>();
				foreach (App_data.Vol_infoRow row in vol_info_table.Rows)
				{
					Web_src_info src_info = new Web_src_info(
						row.Url,
						row.Index,
						row.State,
						vol_info_counter,
						row.Cookie,
						row.Name,
						new Web_src_info(row.Parent_url, 0, "", null, "", row.Parent_name));
					src_info.Children = new ObservableCollection<Web_src_info>();
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
				foreach (var vol in vol_info_list)
				{
					Counter counter = new Counter(0);
					foreach (var row in page_info_table)
					{
						if (row.Parent_url == vol.Url)
						{
							counter.Increase_all();
							vol.Children.Add(new Web_src_info(
								row.Url,
								row.Index,
								row.State,
								counter,
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

				comic_spider.Thread_count = int.Parse(txt_thread.Text);
				comic_spider.Async_start(vol_info_list, txt_dir.Text);
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
				vol_info_list.RemoveAt(vol_list.SelectedIndex);
			}
		}

		private void btn_select_downloaded_Click(object sender, RoutedEventArgs e)
		{
			vol_list.Focus();
			vol_list.SelectedIndex = -1;
			foreach (var item in vol_info_list)
			{
				if (item.State == "OK")
				{
					vol_list.SelectedItems.Add(item);
				}
			}
		}

		private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
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
			ObservableCollection<Web_src_info> list = new ObservableCollection<Web_src_info>();
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

		private GridViewColumnHeader _lastHeaderClicked = null;
		private ListSortDirection _lastDirection = ListSortDirection.Ascending;
		private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
		{
			GridViewColumnHeader headerClicked =
				  e.OriginalSource as GridViewColumnHeader;
			ListSortDirection direction;

			if (headerClicked != null)
			{
				if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
				{
					if (headerClicked != _lastHeaderClicked)
					{
						direction = ListSortDirection.Ascending;
					}
					else
					{
						if (_lastDirection == ListSortDirection.Ascending)
						{
							direction = ListSortDirection.Descending;
						}
						else
						{
							direction = ListSortDirection.Ascending;
						}
					}

					string header = headerClicked.Column.Header as string;
					Sort(header, direction);

					if (direction == ListSortDirection.Ascending)
					{
						headerClicked.Column.HeaderTemplate =
						  Resources["HeaderTemplateArrowUp"] as DataTemplate;
					}
					else
					{
						headerClicked.Column.HeaderTemplate =
						  Resources["HeaderTemplateArrowDown"] as DataTemplate;
					}

					// Remove arrow from previously sorted header
					if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
					{
						_lastHeaderClicked.Column.HeaderTemplate = null;
					}


					_lastHeaderClicked = headerClicked;
					_lastDirection = direction;
				}
			}
		}
		private void Sort(string sortBy, ListSortDirection direction)
		{
			ICollectionView dataView =
			  System.Windows.Data.CollectionViewSource.GetDefaultView(vol_list.ItemsSource);

			dataView.SortDescriptions.Clear();
			SortDescription sd = new SortDescription(sortBy, direction);
			dataView.SortDescriptions.Add(sd);
			dataView.Refresh();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			comic_spider.Stop();

			DateTime dt = DateTime.Now;

			Save_settings();
			Save_vol_info_list();
			Save_page_info_list();

			Console.WriteLine(DateTime.Now.Subtract(dt).TotalSeconds);

			Environment.Exit(0);
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
			App_data.Vol_infoDataTable vol_info_table = new App_data.Vol_infoDataTable();
			foreach (var item in vol_info_list)
			{
				vol_info_table.AddVol_infoRow(
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

			Vol_infoTableAdapter vol_adapter = new Vol_infoTableAdapter();
			vol_adapter.Adapter.DeleteCommand = vol_adapter.Connection.CreateCommand();
			vol_adapter.Adapter.DeleteCommand.CommandText = "delete from Vol_info where 1";

			vol_adapter.Connection.Open();

			vol_adapter.Adapter.DeleteCommand.ExecuteNonQuery();
			vol_adapter.Update(vol_info_table);

			vol_adapter.Connection.Close();
		}
		private void Save_page_info_list()
		{
			App_data.Page_infoDataTable page_info_table = new App_data.Page_infoDataTable();
			foreach (Web_src_info vol in vol_info_list)
			{
				if (vol.Children == null) continue;
				foreach (Web_src_info item in vol.Children)
				{
					page_info_table.AddPage_infoRow(
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

			Page_infoTableAdapter page_adapter = new Page_infoTableAdapter();
			page_adapter.Adapter.DeleteCommand = page_adapter.Connection.CreateCommand();
			page_adapter.Adapter.DeleteCommand.CommandText = "delete from Page_info where 1";

			page_adapter.Connection.Open();

			page_adapter.Adapter.DeleteCommand.ExecuteNonQuery();
			page_adapter.Update(page_info_table);

			page_adapter.Connection.Close();
		}

		[Serializable]
		private class Settings
		{
			public Settings()
			{
				Thread_count = "1";
			}

			public string Root_dir { get; set; }
			public string Url { get; set; }
			public string Thread_count { get; set; }
		}
	}
}
