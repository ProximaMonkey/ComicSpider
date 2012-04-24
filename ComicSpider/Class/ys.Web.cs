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
			try
			{
//                string doc = @"<!DOCTYPE html>
//<html>
//<head>
//<meta http-equiv=""x-ua-compatible"" content=""ie=11"">
//<meta http-equiv=""x-ua-compatible"" content=""ie=10"">
//<meta http-equiv=""x-ua-compatible"" content=""ie=9"">
//<meta http-equiv=""x-ua-compatible"" content=""ie=8"">
//<meta charset=""utf-8"" />
//<title>Comic Spider</title>
//<script type='text/javascript'>function getUserAgent(){document.write(navigator.userAgent)}</script>
//</head>
//<body>
//</body>
//</html>";

//                System.Windows.Forms.WebBrowser wb = new System.Windows.Forms.WebBrowser();
//                wb.Url = new System.Uri("about:blank");
//                wb.Document.Write(doc);
//                wb.Document.InvokeScript("getUserAgent");

//                return System.Environment.OSVersion.ToString() + " " +
//                    wb.DocumentText.Substring(doc.Length);
				return System.Environment.OSVersion.ToString();
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
