using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

			System.Media.SystemSounds.Exclamation.Play();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
