using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ComicSpider
{
	/// <summary>
	/// Interaction logic for WorkingIcon.xaml
	/// </summary>
	public partial class WorkingIcon : UserControl
	{
		public WorkingIcon()
		{
			this.InitializeComponent();

			sb_show_working = Resources["sb_show_working"] as Storyboard;
			sb_working = Resources["sb_working"] as Storyboard;
			sb_hide_working = Resources["sb_hide_working"] as Storyboard;
			sb_working.Begin();
			sb_working.Pause();
		}
		public void Show_working()
		{
			sb_show_working.Begin();
			sb_working.Resume();
		}
		public void Hide_working()
		{
			sb_hide_working.Begin();
			sb_working.Pause();
		}

		/***************************** Private ********************************/

		private Storyboard sb_show_working;
		private Storyboard sb_working;
		private Storyboard sb_hide_working;
	}
}