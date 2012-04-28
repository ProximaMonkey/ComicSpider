using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComicSpider
{
	[Serializable]
	public class Main_settings
	{
		public static Main_settings Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new Main_settings();
				}
				return instance;
			}
			set
			{
				instance = value;
			}
		}

		private static Main_settings instance;

		private Main_settings()
		{
			Main_url = string.Empty;
			Root_dir = string.Empty;
			Thread_count = 3;
			Is_auto_begin = true;
			Is_silent = false;
			Max_console_line = 500;
			Max_download_speed = 0;
			clear_cache_date = DateTime.Now;
		}

		public string Main_url { get; set; }
		public string Root_dir { get; set; }
		public int Thread_count { get; set; }

		public bool Is_auto_begin { get; set; }
		public bool Is_silent { get; set; }

		public int Max_console_line { get; set; }
		public int Max_download_speed { get; set; } // unit: KB/s

		public string App_version { get; set; }

		public bool Is_need_clear_cache
		{
			get
			{
				return DateTime.Now.Subtract(clear_cache_date).TotalDays > 30;
			}
		}

		private DateTime clear_cache_date { get; set; }
	}
}
