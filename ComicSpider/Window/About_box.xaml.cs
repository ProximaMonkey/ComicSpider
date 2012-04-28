using System.Windows;

namespace ComicSpider
{
	public partial class About_box : Window
	{
		public About_box()
		{
			InitializeComponent();
			txt_version.Text = Main_settings.Instance.App_version;
		}

		private void btn_ok_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
