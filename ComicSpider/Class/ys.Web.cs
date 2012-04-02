using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ys.Web
{
	public class WebClientEx : WebClient
	{
		protected override WebRequest GetWebRequest(Uri address)
		{
			HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
			request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			return request;
		}
	}
}
