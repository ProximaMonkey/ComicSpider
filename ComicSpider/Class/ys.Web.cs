using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ys
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

	public class Web
	{
		public static string Post(
			string url,
			Dictionary<string, string> info)
		{
			string data = "";

			foreach (var item in info)
			{
				data += System.Web.HttpUtility.UrlEncode(item.Key) + "="
					+ System.Web.HttpUtility.UrlEncode(item.Value) + "&";
			}

			WebClient wc = new WebClient();
			wc.Headers["Content-type"] = "application/x-www-form-urlencoded";
			return wc.UploadString(url, data);
		}
	}
}
