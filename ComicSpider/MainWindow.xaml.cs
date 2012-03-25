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
		}

		public static MainWindow Main;

		public delegate void Show_vol_list_delegate(ObservableCollection<Web_page_info> list);
		public void Show_vol_list(ObservableCollection<Web_page_info> list)
		{
			vol_list.Model = new Vol_info_model(list);
			vol_info_list = list;

			btn_get_list.IsEnabled = true;
			btn_start.IsEnabled = true;
		}

		public delegate void Show_info_delegate(string info);
		public void Show_info(string info)
		{
			this.Title = info;
		}

		public delegate void Update_progress_delegate(Web_page_info img_info);
		public void Update_progress(Web_page_info img_info)
		{
			vol_info_list.First((m) => { return m.Url == img_info.Parent.Parent.Url; }).State
				= string.Format("{0}/{1}", img_info.Parent.Counter.Downloaded, img_info.Parent.Counter.All);
		}

		private Comic_spider comic_spider;
		private Settings settings;
		private ObservableCollection<Web_page_info> vol_info_list;

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			comic_spider = new Comic_spider();

			#region Get settings

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

			#endregion		

			#region Recovery last download history

			Vol_infoTableAdapter vol_adpter = new Vol_infoTableAdapter();
			App_data.Vol_infoDataTable table = vol_adpter.GetData();
			if (table.Count > 0)
			{
				foreach (App_data.Vol_infoRow row in table.Rows)
				{
					vol_info_list.Add(new Web_page_info(row.Url, 0));
				}
			}
			
			#endregion
		}

		private void btn_start_Click(object sender, RoutedEventArgs e)
		{
			if (btn_start.Content.ToString() == "Start")
			{
				btn_start.Content = "Stop";
				btn_get_list.IsEnabled = false;

				comic_spider.Thread_count = int.Parse(txt_thread.Text);
				comic_spider.Async_download(vol_info_list, txt_dir.Text);
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

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			comic_spider.Stop();

			#region Save settings

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
			#endregion		
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Environment.Exit(0);
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
