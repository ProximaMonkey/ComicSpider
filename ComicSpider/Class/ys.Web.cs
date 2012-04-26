using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace ys
{
	public class Web
	{
		public static string Get_host_name(string url)
		{
			// Unit test:
			// http://img.abc.com
			// https://img.abc.com/abc.file
			// more.img.abc.com/abc.file
			// abc.com/abc.file
			Regex reg = new Regex(@"(https?://)?([^\.^/]+?\.)*(?<host>[^\.^/]+?\.[^\.^/]+)/?", RegexOptions.IgnoreCase);
			return reg.Match(url).Groups["host"].Value;
		}

		public static string Get_user_agent()
		{
			return System.Environment.OSVersion.ToString();
		}
	}
}
