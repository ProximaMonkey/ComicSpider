using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComicSpider.UserTableAdapters;

namespace ComicSpider
{
	public class Cookie_pool
	{
		public static Cookie_pool Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new Cookie_pool();
				}
				return instance;
			}
			set
			{
				instance = value;
			}
		}

		public void Update(string host, string cookie)
		{
			if (!string.IsNullOrEmpty(cookie))
			{
				int i = cookie.IndexOf(';');
				if (i >= 0) cookie = cookie.Remove(i);

				var row = table.FindByHost(host);
				if (row == null)
					table.AddCookieRow(host, cookie);
				else
					row.Value = cookie;
			}
		}

		public string Get(string host)
		{
			var row = table.FindByHost(host);
			if (row == null)
				return null;
			else
				return table.FindByHost(host).Value;
		}

		public void Save()
		{
			CookieTableAdapter adapter = new CookieTableAdapter();
			adapter.Update(table);
		}

		/***************************** Private ********************************/

		private static Cookie_pool instance;

		private Cookie_pool()
		{
			CookieTableAdapter adapter = new CookieTableAdapter();
			table = adapter.GetData();
		}

		private User.CookieDataTable table;
	}
}
