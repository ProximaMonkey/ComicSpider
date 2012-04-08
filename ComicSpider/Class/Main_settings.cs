using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComicSpider
{
	[Serializable]
	public class Main_settings
	{
		public static Main_settings Main
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
			Thread_count = "5";
		}

		public string Main_url { get; set; }
		public string Root_dir { get; set; }
		public string Thread_count { get; set; }
		public bool Auto_begin { get; set; }
		public int Max_console_line { get; set; }
	}
}
