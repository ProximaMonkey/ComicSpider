/*
	Comic Spider - a tool to download online manga.
	For further information, visit Project Home https://github.com/ysmood/ComicSpider
	April 2012 y.s.

	This program is free software: you can redistribute it and/or modify 
	it under the terms of the GNU General Public License as published by 
	the Free Software Foundation, either version 3 of the License, or 
	(at your option) any later version. 

	This program is distributed in the hope that it will be useful, 
	but WITHOUT ANY WARRANTY; without even the implied warranty of 
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
	GNU General Public License for more details. 

	You should have received a copy of the GNU General Public License
	along with this program. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Windows;
using System.Windows.Media;
using ComicSpider.UserTableAdapters;
using System.Data.SQLite;

namespace ComicSpider
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			Main = this;

			Key_valueTableAdapter kv_adpter = new Key_valueTableAdapter();
			kv_adpter.Adapter.SelectCommand = kv_adpter.Connection.CreateCommand();
			kv_adpter.Adapter.SelectCommand.CommandText = "select * from Key_value where Key = 'Settings' limit 0,1";

			kv_adpter.Connection.Open();

			SQLiteDataReader data_reader = kv_adpter.Adapter.SelectCommand.ExecuteReader();
			if (data_reader.Read())
			{
				Main_settings settings = ys.Common.ByteArrayToObject(data_reader["Value"] as byte[]) as Main_settings;
				if (settings != null)
					cb_latest_volume_only.IsChecked = settings.Latest_volume_only;
			}

			kv_adpter.Connection.Close();
		}

		public static MainWindow Main;

		public string Main_progress
		{
			set
			{
				txt_main_progress.Text = value;
			}
		}
		public string Tray_tooltip
		{
			set
			{
				tray.ToolTipText = value;
			}
		}
		public bool Latest_volume_only
		{
			get
			{
				return cb_latest_volume_only.IsChecked == true;
			}
			set { cb_latest_volume_only.IsChecked = value; }
		}

		public void Show_balloon(string info)
		{
			tray_balloon = new Tray_balloon();
			tray_balloon.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler((oo, ee) =>
			{
				tray_balloon.Visibility = System.Windows.Visibility.Collapsed;
			});
			tray.ShowCustomBalloon(tray_balloon, System.Windows.Controls.Primitives.PopupAnimation.Slide, 8000);
			tray_balloon.Text = info;

			MediaPlayer mplayer = new MediaPlayer();
			mplayer.Open(new Uri(@"Asset\msg.wav", UriKind.Relative));
			mplayer.Play();
		}

		/***************************** Private ********************************/

		private Tray_balloon tray_balloon;

		private void Window_DragEnter(object sender, DragEventArgs e)
		{
			e.Effects = DragDropEffects.All;
		}
		private void Window_Drop(object sender, DragEventArgs e)
		{
			this.Topmost = false;
			string url = e.Data.GetData(typeof(string)) as string;

			if (url != null)
			{
				Dashboard.Instance.Get_volume_list(url);
			}
			this.Topmost = true;
		}

		private void btn_hide_Click(object sender, RoutedEventArgs e)
		{
			Dashboard.Instance.Visibility = System.Windows.Visibility.Collapsed;
			this.Visibility = System.Windows.Visibility.Collapsed;
			tray.Visibility = System.Windows.Visibility.Visible;
		}
		private void btn_dashboard_Click(object sender, RoutedEventArgs e)
		{
			Dashboard.Instance.Show();
			Dashboard.Instance.WindowState = System.Windows.WindowState.Normal;
			Dashboard.Instance.Activate();
		}

		private void btn_hide_window_Click(object sender, RoutedEventArgs e)
		{
			this.Visibility = System.Windows.Visibility.Collapsed;
			tray.Visibility = System.Windows.Visibility.Visible;
		}

		private void tray_TrayLeftMouseDown(object sender, RoutedEventArgs e)
		{
			this.Visibility = System.Windows.Visibility.Visible;
			this.Activate();
			tray.Visibility = System.Windows.Visibility.Collapsed;
		}

		private void tray_TrayToolTipOpen(object sender, RoutedEventArgs e)
		{
			txt_main_progress.Text = Dashboard.Instance.Main_progress;
			tray.ToolTipText = Dashboard.Instance.Title;
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			tray.Dispose();
			if (Dashboard.Initialized)
			{
				Dashboard.Instance.Close();
			}
		}
	}
}
