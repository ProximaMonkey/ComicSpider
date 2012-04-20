using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Linq;

namespace ys
{
	public enum Web_src_state
	{
		Wait,
		Loading,
		Downloaded,
		Failed
	};

	public class Web_src_info : System.ComponentModel.INotifyPropertyChanged
	{
		public Web_src_info()
		{
			Url = string.Empty;
			Index = 0;
			Name = string.Empty;
			state = Web_src_state.Wait;
			Cookie = string.Empty;
		}
		public Web_src_info(
			string url,
			int index,
			string name,
			string path,
			Web_src_info parent,
			string cookie = "")
		{
			Url = url;
			Index = index;
			Name = name;
			state = Web_src_state.Wait;
			state_text = string.Empty;
			Path = path;
			Parent = parent;
			Cookie = cookie;
		}

		public Web_src_state State
		{
			get
			{
				return state;
			}
			set
			{
				state = value;
				NotifyPropertyChanged("State_text");
			}
		}
		public string State_text
		{
			set
			{
				state_text = value;
				switch(value)
				{
					case "OK":
						state = Web_src_state.Downloaded;
						break;
					case "X":
						state = Web_src_state.Failed;
						break;
					default:
						state = Web_src_state.Loading;
						break;
				}
				NotifyPropertyChanged("State_text");
			}
			get
			{
				switch (state)
				{
					case Web_src_state.Wait:
						return "";
					case Web_src_state.Downloaded:
						return "OK";
					case Web_src_state.Failed:
						return "X";
					default:
						return state_text;
				}
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
		public Web_src_info Parent { get; protected set; }

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
		public double Size
		{
			get
			{
				return size;
			}
			set
			{
				size = value;
				NotifyPropertyChanged("Size");
			}
		}
		public int Index { get; set; }
		public string Cookie { get; set; }
		public string Path { get; set; }

		public int Count
		{
			get
			{
				if (children == null)
					return 0;
				else
					return children.Count;
			}
		}
		public int Downloaded
		{
			get
			{
				if (children == null) return 0;

				return children.Count(c => c.State == Web_src_state.Downloaded);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public class Comparer : IEqualityComparer<Web_src_info>
		{
			public bool Equals(Web_src_info x, Web_src_info y)
			{
				if (x.Name == y.Name)
				{
					y.Name = x.Name + "'";
				}
				return x.Url == y.Url;
			}
			public int GetHashCode(Web_src_info src_info)
			{
				if (Object.ReferenceEquals(src_info, null)) return 0;

				return src_info.Url.GetHashCode();
			}
		}

		/***************************** Private ********************************/

		private void NotifyPropertyChanged(String prop_name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(prop_name));
			}
		}

		private string url;
		private string name;
		private Web_src_state state;
		private string state_text;
		private double size;
		private List<Web_src_info> children;
	}

	public class Website_info
	{
		public Website_info(string name, string home, List<string> hosts)
		{
			Name = name;
			Home = home;
			Hosts = hosts;
		}

		public string Name { get; set; }
		public string Home { get; set; }
		public List<string> Hosts { get; set; }
	}
}
