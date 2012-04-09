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
using ComicSpider.UserTableAdapters;
using System.Data.SQLite;
using System.Windows.Media.Animation;
using System.Windows.Input;

namespace ComicSpider
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			User.CheckAndFix();

			Main = this;

			this.Title = "Comic Spider "
				+ System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()
				+ " April 2012 y.s.";
			img_logo.ToolTip = this.Title;

			try
			{
				#region Load settings
				Key_valueTableAdapter kv_adpter = new Key_valueTableAdapter();
				kv_adpter.Adapter.SelectCommand = kv_adpter.Connection.CreateCommand();
				kv_adpter.Adapter.SelectCommand.CommandText = "select * from Key_value where Key = 'Settings' limit 0,1";

				kv_adpter.Connection.Open();

				SQLiteDataReader data_reader = kv_adpter.Adapter.SelectCommand.ExecuteReader();
				if (data_reader.Read())
				{
					Main_settings settings = ys.Common.ByteArrayToObject(data_reader["Value"] as byte[]) as Main_settings;
					if (settings != null)
					{
						cb_auto_begin.IsChecked = settings.Auto_begin;
					}
				}

				kv_adpter.Connection.Close();
				#endregion

				// Binding global hot key.
				global_hotkey = new ManagedWinapi.Hotkey();
				global_hotkey.WindowsKey = true;
				global_hotkey.KeyCode = System.Windows.Forms.Keys.C;
				global_hotkey.HotkeyPressed += new EventHandler(global_hotkey_HotkeyPressed);
				global_hotkey.Enabled = true;
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message);
			}

			sb_show_window = Resources["sb_show_window"] as Storyboard;
			sb_hide_window = Resources["sb_hide_window"] as Storyboard;
		}

		public static MainWindow Main;

		public string Main_progress
		{
			set
			{
				txt_main_progress.Text = value;
			}
		}
		public new string Title
		{
			get { return txt_title.Text; }
			set
			{
				txt_title.Text = value;
				tray.ToolTipText = value;
			}
		}
		public bool Auto_begin
		{
			get { return cb_auto_begin.IsChecked == true; }
		}
		public void Task_done()
		{
			working_icon.Hide_working();
		}

		public void Show_balloon(string info, MouseButtonEventHandler click_event = null, bool play_sound = false)
		{
			tray_balloon = new Tray_balloon();

			tray_balloon.PreviewMouseDown += (o, e) =>
			{
				tray_balloon.Visibility = System.Windows.Visibility.Collapsed;
			};
			if (click_event != null)
				tray_balloon.PreviewMouseDown += click_event;
			tray.ShowCustomBalloon(tray_balloon, System.Windows.Controls.Primitives.PopupAnimation.Slide, 5000);
			tray_balloon.Text = info;

			string sound_path = @"Asset\情報バー(message).wav";
			if (play_sound &&
				System.IO.File.Exists(sound_path))
			{
				System.Media.SoundPlayer sp = new System.Media.SoundPlayer(sound_path);
				sp.Play();
			}
		}

		/***************************** Private ********************************/

		private Tray_balloon tray_balloon;
		private ManagedWinapi.Hotkey global_hotkey;
		private Storyboard sb_show_window;
		private Storyboard sb_hide_window;

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
				working_icon.Show_working();
				Dashboard.Instance.Get_volume_list(url);
			}
			this.Topmost = true;
		}

		private void btn_hide_Click(object sender, RoutedEventArgs e)
		{
			sb_hide_window.Completed += (oo, ee) =>
			{
				this.Visibility = System.Windows.Visibility.Collapsed;
				tray.Visibility = System.Windows.Visibility.Visible;
			};
			sb_hide_window.Begin();
		}
		private void btn_dashboard_Click(object sender, RoutedEventArgs e)
		{
			Dashboard.Instance.Show();
		}
		private void btn_close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void cb_topmost_Click(object sender, RoutedEventArgs e)
		{
			this.Topmost = !this.Topmost;
		}
		private void global_hotkey_HotkeyPressed(object sender, EventArgs e)
		{
			if (this.Visibility == System.Windows.Visibility.Visible)
				btn_hide_Click(null, null);
			else
				tray_TrayLeftMouseDown(null, null);
		}
		private void tray_TrayLeftMouseDown(object sender, RoutedEventArgs e)
		{
			this.Visibility = System.Windows.Visibility.Visible;
			sb_show_window.Completed += (oo, ee) =>
			{
				this.Activate();
				tray.Visibility = System.Windows.Visibility.Collapsed;
			};
			sb_show_window.Begin();
		}

		private void window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			this.DragMove();
		}
		private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			sb_hide_window.Completed += (oo, ee) =>
			{
				this.Visibility = System.Windows.Visibility.Collapsed;
				this.Close();
			};
			sb_hide_window.Begin();

			if(this.Visibility == System.Windows.Visibility.Visible)
				e.Cancel = true;
		}
		private void Window_Closed(object sender, EventArgs e)
		{
			tray.Dispose();
			if (Dashboard.Is_initialized)
			{
				Dashboard.Instance.Close();
			}
		}

		private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			switch(e.Key)
			{
				case System.Windows.Input.Key.Escape:
					btn_hide_Click(null, null);
					break;

				case System.Windows.Input.Key.T:
					cb_topmost_Click(null, null);
					break;

				case System.Windows.Input.Key.D:
					btn_dashboard_Click(null, null);
					break;
			}
		}

	}
}
