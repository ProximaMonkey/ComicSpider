/*
	Comic Spider - a tool to download and view online manga.
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
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using ComicSpider.UserTableAdapters;
using ys;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Threading;
using System.Collections.Generic;

namespace ComicSpider
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			sb_show_window = Resources["sb_show_window"] as Storyboard;
			sb_hide_window = Resources["sb_hide_window"] as Storyboard;

			btn_start = Find_menu_item(this.Resources["main_menu"], "btn_start");
			cb_is_slient = Find_menu_item(this.Resources["main_menu"], "cb_is_slient");
			cb_topmost = Find_menu_item(this.Resources["main_menu"], "cb_topmost");

			App.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Current_DispatcherUnhandledException);
			App.Current.SessionEnding += new SessionEndingCancelEventHandler(Current_SessionEnding);

			User.CheckAndFix();

			Init_settings();
			Init_global_hotkey();

			Main = this;

			this.Title = "Comic Spider "
				+ Main_settings.Instance.App_version
				+ " April 2012 y.s.";
			img_logo.ToolTip = this.Title;

			Taskbar = new ys.Win7.TaskBar();
		}

		public static MainWindow Main;
		public ys.Win7.TaskBar Taskbar;
		public MenuItem Start_button { get { return btn_start; } }

		public string Main_progress
		{
			set
			{
				txt_main_progress.Text = value;
				txt_tray_main_progress.Text = value;
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

		public void Init_settings()
		{
			Key_valueTableAdapter kv_adpter = new Key_valueTableAdapter();
			kv_adpter.Adapter.SelectCommand = kv_adpter.Connection.CreateCommand();
			kv_adpter.Adapter.SelectCommand.CommandText = "select * from Key_value where Key = 'Settings' limit 0,1";

			kv_adpter.Connection.Open();

			SQLiteDataReader data_reader = kv_adpter.Adapter.SelectCommand.ExecuteReader();
			if (data_reader.Read())
			{
				Main_settings.Instance = ys.Common.ByteArrayToObject(data_reader["Value"] as byte[]) as Main_settings;
			}

			cb_auto_begin.IsChecked = Main_settings.Instance.Is_auto_begin;
			cb_is_slient.IsChecked = Main_settings.Instance.Is_silent;
			btn_start.IsEnabled = Main_settings.Instance.Start_button_enabled;

			kv_adpter.Connection.Close();

			Main_settings.Instance.Max_console_line = 500;
			Main_settings.Instance.App_version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
		}
		public void Save_settings()
		{
			Key_valueTableAdapter kv_adapter = new Key_valueTableAdapter();
			kv_adapter.Adapter.UpdateCommand = kv_adapter.Connection.CreateCommand();
			kv_adapter.Adapter.UpdateCommand.CommandText = "insert or replace into Key_value ([Key], [Value]) values ('Settings', @value)";
			kv_adapter.Adapter.UpdateCommand.Parameters.AddWithValue("@value", ys.Common.ObjectToByteArray(Main_settings.Instance));

			kv_adapter.Connection.Open();

			kv_adapter.Adapter.UpdateCommand.ExecuteNonQuery();

			kv_adapter.Connection.Close();
		}

		public void Help(object sender, RoutedEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start("Comic Spider.chm");
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message);
			}
		}

		public void Show_balloon(string info, MouseButtonEventHandler click_event = null, bool play_sound = false)
		{
			if (Main_settings.Instance.Is_silent) return;

			tray_balloon = new Tray_balloon();

			tray_balloon.PreviewMouseDown += (o, e) =>
			{
				tray_balloon.Visibility = System.Windows.Visibility.Collapsed;
			};
			if (click_event != null)
				tray_balloon.PreviewMouseDown += click_event;
			tray.ShowCustomBalloon(tray_balloon, System.Windows.Controls.Primitives.PopupAnimation.Slide, 5000);
			tray_balloon.Text = info;

			string sound_path = @"Asset\message.wav";
			if (play_sound)
			{
				if (System.IO.File.Exists(sound_path))
				{
					System.Media.SoundPlayer sp = new System.Media.SoundPlayer(sound_path);
					sp.Play();
				}
				else
					System.Media.SystemSounds.Asterisk.Play();
			}
		}

		/**************** Delegate ****************/

		public delegate void Show_supported_sites_delegate(List<Website_info> list);
		public void Show_supported_sites(List<Website_info> list)
		{
			if (cb_supported_websites.ItemsSource == null)
				cb_supported_websites.Items.Clear();

			cb_supported_websites.ItemsSource = list;
		}

		public delegate void Show_update_info_delegate(string info, string downlaod_page);
		public void Show_update_info(string info, string downlaod_page)
		{
			Show_balloon(info, (o, e) =>
			{
				try
				{
					System.Diagnostics.Process.Start(downlaod_page);
				}
				catch (Exception ex)
				{
					Message_box.Show(ex.Message);
				}
			});
		}

		/***************************** Private ********************************/

		private Tray_balloon tray_balloon;
		private ManagedWinapi.Hotkey global_hotkey;
		private Storyboard sb_show_window;
		private Storyboard sb_hide_window;

		private MenuItem btn_start;
		private MenuItem cb_topmost;
		private MenuItem cb_is_slient;

		private MenuItem Find_menu_item(object resource, string name)
		{
			MenuItem item = null;
			foreach (MenuItem i in (resource as ContextMenu).Items)
			{
				if (i.Name == name)
				{
					return item = i;
				}
			}
			return item;
		}

		private void Init_global_hotkey()
		{
			try
			{
				// Binding global hot key.
				global_hotkey = new ManagedWinapi.Hotkey();
				global_hotkey.WindowsKey = true;
				global_hotkey.KeyCode = System.Windows.Forms.Keys.C;
				global_hotkey.HotkeyPressed += new EventHandler(global_hotkey_HotkeyPressed);
				global_hotkey.Enabled = true;
			}
			catch
			{
				Message_box.Show("Register global hotkey 'Win + C' failed.");
			}
		}

		private void Window_Drop(object sender, DragEventArgs e)
		{
			string url = e.Data.GetData(typeof(string)) as string;

			if (url != null)
			{
				Dashboard.Instance.Get_volume_list(url);
			}
			else
				this.Title = "It's not a valid link.";
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
		private void Open_folder(object sender, RoutedEventArgs e)
		{
			try
			{
				System.Diagnostics.Process.Start(Dashboard.Instance.txt_dir.Text);
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message);
			}
		}

		private void cb_websites_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Website_info item = cb_supported_websites.SelectedValue as Website_info;

			if (item == null ||
				string.IsNullOrEmpty(item.Home))
				return;
			try
			{
				System.Diagnostics.Process.Start(item.Home);
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message);
			}
		}
		private void cb_websites_DropDownOpened(object sender, EventArgs e)
		{
			if (!Dashboard.Is_initialized)
			{
				var d = Dashboard.Instance;
				cb_supported_websites.Items.Add(new Website_info("Loading parser...", "default"));
			}
		}

		private void View_volume(object sender, RoutedEventArgs e)
		{
			var item = cb_view_pages.SelectedItem as Web_resource_info;
			string file_path = string.Empty;

			if (item == null)
			{
				try_open_view_page();
			}
			else
			{
				file_path = Path.Combine(item.Path, "index.html");
				if (File.Exists(file_path))
				{
					System.Diagnostics.Process.Start(file_path);
					return;
				}
				else
				{
					try_open_view_page();
				}
			}
		}
		private void cb_view_pages_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = cb_view_pages.SelectedItem as Web_resource_info;

			if (item == null) return;

			string file_path = Path.Combine(item.Path, "index.html");
			if (File.Exists(file_path))
			{
				System.Diagnostics.Process.Start(file_path);
				return;
			}
			Message_box.Show("No view page found. Please wait a volume downloaded or fix the view page manually.");
		}
		private bool try_open_view_page()
		{
			var list = Dashboard.Instance.volume_list.Items;
			for (int i = list.Count - 1; i > -1; i--)
			{
				string file_path = Path.Combine((list[i] as Web_resource_info).Path, "index.html");
				if (File.Exists(file_path))
				{
					System.Diagnostics.Process.Start(file_path);
					return true;
				}
			}
			Message_box.Show("No view page found. Please wait a volume downloaded or fix the view page manually.");
			return false;
		}
		private void cb_view_pages_DropDownOpened(object sender, EventArgs e)
		{
			cb_view_pages.ItemsSource = Dashboard.Instance.volume_list.ItemsSource;
		}

		private void btn_dashboard_Click(object sender, RoutedEventArgs e)
		{
			Dashboard.Instance.Show();
		}
		private void btn_start_Click(object sender, RoutedEventArgs e)
		{
			if (btn_start.IsEnabled)
				Dashboard.Instance.Start();
		}
		private void cb_is_slient_Click(object sender, RoutedEventArgs e)
		{
			MenuItem item = sender as MenuItem;
			Main_settings.Instance.Is_silent = item.IsChecked;
		}
		private void btn_close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
		private void About(object sender, RoutedEventArgs e)
		{
			new About_box().Show();
		}
		private void Close(object sender, RoutedEventArgs e)
		{
			if (this.Visibility != System.Windows.Visibility.Visible)
			{
				this.Visibility = System.Windows.Visibility.Visible;
				sb_show_window.Completed += (oo, ee) =>
				{
					this.Activate();
					tray.Visibility = System.Windows.Visibility.Collapsed;
					this.Close();
				};
				sb_show_window.Begin();
			}
			else
				this.Close();
		}

		private void cb_auto_begin_Click(object sender, RoutedEventArgs e)
		{
			Main_settings.Instance.Is_auto_begin = cb_auto_begin.IsChecked == true;
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
				Main_progress = Dashboard.Instance.Main_progress;
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
			try
			{
				if (Dashboard.Is_initialized)
					Dashboard.Instance.Update_settings();

				Save_settings();
			}
			catch (Exception ex)
			{
				Message_box.Show(ex.Message + '\n' + ex.StackTrace);
			}

			tray.Dispose();
		}

		private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			switch(e.Key)
			{
				case System.Windows.Input.Key.A:
					cb_auto_begin.IsChecked = !cb_auto_begin.IsChecked;
					Main_settings.Instance.Is_auto_begin = cb_auto_begin.IsChecked == true;
					break;

				case System.Windows.Input.Key.D:
					btn_dashboard_Click(null, null);
					break;

				case System.Windows.Input.Key.I:
					cb_is_slient.IsChecked = !cb_is_slient.IsChecked;
					Main_settings.Instance.Is_silent = cb_is_slient.IsChecked;
					break;

				case System.Windows.Input.Key.O:
					Open_folder(null, null);
					break;

				case System.Windows.Input.Key.T:
					cb_topmost.IsChecked = !cb_topmost.IsChecked;
					this.Topmost = cb_topmost.IsChecked;
					break;

				case System.Windows.Input.Key.S:
					btn_start_Click(null, null);
					break;

				case System.Windows.Input.Key.V:
					View_volume(null, null);
					break;

				case System.Windows.Input.Key.F1:
					Help(null, null);
					break;
			}
		}

		private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Exception ex = e.Exception;
			Message_box.Show(ex.Message + '\n' + ex.StackTrace);
			try
			{
				Save_settings();
				if (Dashboard.Is_initialized)
					Dashboard.Instance.Save_all();
			}
			catch { }
		}
		private void Current_SessionEnding(object sender, SessionEndingCancelEventArgs e)
		{
			this.Close();
		}

		#region Fix the TaskbarNotification position bug.

		private void ContextMenu_Closed(object sender, RoutedEventArgs e)
		{
			ContextMenu cm = sender as ContextMenu;
			cm.VerticalOffset = 0;
			cm.HorizontalOffset = 0;
		} 

		#endregion
	}
}
