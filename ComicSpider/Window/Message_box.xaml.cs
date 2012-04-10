using System.Windows;

namespace ComicSpider
{
	public partial class Message_box : Window
	{
		public static bool? Show(string msg, string title = "Comic Spider")
		{
			Message_box msg_box = new Message_box(msg, title);
			return msg_box.ShowDialog();
		}

		private Message_box(string msg, string title = "")
		{
			InitializeComponent();

			this.Title = title;
			txtMain.Text = msg;

			string sound_path = @"Asset\メッセージ(alert).wav";
			if (System.IO.File.Exists(sound_path))
			{
				System.Media.SoundPlayer sp = new System.Media.SoundPlayer(sound_path);
				sp.Play();
			}
			else
				System.Media.SystemSounds.Hand.Play();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
