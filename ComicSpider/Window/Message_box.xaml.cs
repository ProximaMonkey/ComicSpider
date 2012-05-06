using System.Windows;

namespace ComicSpider
{
	public partial class Message_box : Window
	{
		public static bool Show(string msg, bool show_cancel_button = false, string title = "Comic Spider")
		{
			Message_box msg_box = new Message_box(msg, show_cancel_button, title);
			
			msg_box.ShowDialog();

			return ok;
		}

		private static bool ok = false;

		private Message_box(string msg, bool show_cancel_button = false, string title = "")
		{
			InitializeComponent();

			this.Title = title;
			txtMain.Text = msg;

			if (!show_cancel_button)
				btn_cancel.Visibility = System.Windows.Visibility.Collapsed;

			string sound_path = @"Asset\メッセージ(alert).wav";
			if (System.IO.File.Exists(sound_path))
			{
				System.Media.SoundPlayer sp = new System.Media.SoundPlayer(sound_path);
				sp.Play();
			}
			else
				System.Media.SystemSounds.Hand.Play();
		}

		private void btn_ok_Click(object sender, RoutedEventArgs e)
		{
			ok = true;
			this.Close();
		}

		private void btn_cancel_Click(object sender, RoutedEventArgs e)
		{
			ok = false;
			this.Close();
		}
	}
}
