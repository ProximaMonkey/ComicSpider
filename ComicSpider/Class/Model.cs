using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace ys.Web
{
	public class Web_src_info : System.ComponentModel.INotifyPropertyChanged
	{
		public Web_src_info()
		{
		}
		public Web_src_info(string url,
			int index,
			string state = "",
			string cookie = "",
			string name = "",
			Web_src_info parent = null,
			List<Web_src_info> children = null)
		{
			Url = url;
			Index = index;
			State = state;
			Name = name;
			Cookie = cookie;
			Parent = parent;
			Children = children;
			Missed = false;
		}

		public const string State_downloaded = "OK";
		public const string State_missed = "X";

		public string Url
		{
			get { return url; }
			private set
			{
				url = value;
				NotifyPropertyChanged("Url");
			}
		}
		public string Name
		{
			get { return name; }
			set
			{
				name = value;
				NotifyPropertyChanged("Name");
			}
		}
		public string State
		{
			get { return state; }
			set
			{
				state = value;
				NotifyPropertyChanged("State");
			}
		}
		public List<Web_src_info> Children
		{
			get { return children; }
			set
			{
				children = value;
				NotifyPropertyChanged("Children");
			}
		}
		public int Index { get; set; }
		public string Cookie { get; set; }
		public bool Missed { get; set; }
		public Web_src_info Parent { get; protected set; }

		public int Count
		{
			get
			{
				return children.Count;
			}
		}
		public int Downloaded
		{
			get
			{
				int downloaded = 0;
				if (children != null)
				{
					foreach (Web_src_info item in children)
					{
						if (item.state == Web_src_info.State_downloaded)
							downloaded++;
					}
				}
				return downloaded;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged(String info)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(info));
			}
		}

		private string url;
		private string name;
		private string state;
		private List<Web_src_info> children;
	}
}
