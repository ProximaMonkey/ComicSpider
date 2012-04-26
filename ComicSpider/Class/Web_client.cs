using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ComicSpider
{
	public class Web_client : WebClient
	{
		public Web_client()
		{
			Headers["User-Agent"] = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
		}

		public static Web_client Post(
			string url,
			Dictionary<string, string> info)
		{
			string data = "";

			foreach (var item in info)
			{
				data += System.Web.HttpUtility.UrlEncode(item.Key) + "="
					+ System.Web.HttpUtility.UrlEncode(item.Value) + "&";
			}

			Web_client wc = new Web_client();
			wc.Timeout = 6000;
			wc.AllowAutoRedirect = false;
			wc.Headers["Content-type"] = "application/x-www-form-urlencoded";
			wc.UploadString(url, data);

			Cookie_pool.Instance.Update(ys.Web.Get_host_name(url), wc.ResponseHeaders["Set-Cookie"]);

			return wc;
		}

		public int Timeout = 15 * 1000;
		public bool AllowAutoRedirect = true;

		protected override WebRequest GetWebRequest(Uri address)
		{
			HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);

			// GZip
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

			request.Timeout = Timeout;
			request.AllowAutoRedirect = AllowAutoRedirect;

			return request;
		}
	}
}
