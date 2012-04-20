using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ys.Web
{
	public class WebClientEx : WebClient
	{
		public int Timeout { get; set; }

		protected override WebRequest GetWebRequest(Uri address)
		{
			HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			request.Timeout = timeout;
			return request;
		}

		private int timeout = 30 * 1000;
	}

	public static class App_usage_analytics
	{
		public static void Post(Dictionary<string, string> addition_infos)
		{
			string data = "";

			foreach (var info in addition_infos)
			{
				data += System.Web.HttpUtility.UrlEncode(info.Key) + "=" 
					+ System.Web.HttpUtility.UrlEncode(info.Value) + "&";
			}

			WebClient wc = new WebClient();
			wc.Headers["Content-type"] = "application/x-www-form-urlencoded";
			string ret = wc.UploadString("http://comicspider.sinaapp.com/analytics/?r=a", data);
		}
	}
}
