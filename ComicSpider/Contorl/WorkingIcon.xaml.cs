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

			is_working = false;
			working_counter = 0;
		}

		public bool Is_working { get { return is_working; } }
		public void Show_working()
		{
			working_counter++;

			if (is_working)
				return;

			is_working = true;
			sb_show_working.Begin();
			sb_working.Resume();
		}
		public void Hide_working()
		{
			if (--working_counter <= 0)
			{
				sb_hide_working.Begin();
				sb_working.Pause();

				is_working = false;
				working_counter = 0;
			}
		}

		/***************************** Private ********************************/

		private Storyboard sb_show_working;
		private Storyboard sb_working;
		private Storyboard sb_hide_working;
		private bool is_working;
		private int working_counter;
	}
}