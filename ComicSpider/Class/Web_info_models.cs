using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Linq;

namespace ys
{
	public enum Web_resource_state
	{
		Wait,
		Downloading,
		Downloaded,
		Failed,
		Stopped
	};

	public class Web_resource_info : System.ComponentModel.INotifyPropertyChanged
	{
		public Web_resource_info(
			string url,
			int index,
			string name,
			string path,
			Web_resource_info parent)
		{
			Url = url;
			uri = null;
			Index = index;
			Name = name;
			state = Web_resource_state.Wait;
			state_text = string.Empty;
			Path = path;
			Parent = parent;
		}

		public Web_resource_state State
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
				switch (string.IsNullOrEmpty(value) ? ' ' : value[0])
				{
					case ' ':
						state = Web_resource_state.Wait;
						break;
					case '√':
						state = Web_resource_state.Downloaded;
						break;
					case '#':
						state = Web_resource_state.Stopped;
						state_text = state_text.TrimStart('#').TrimStart();
						break;
					case '×':
						state = Web_resource_state.Failed;
						break;
					default:
						state = Web_resource_state.Downloading;
						break;
				}
				NotifyPropertyChanged("State_text");
			}
			get
			{
				switch (state)
				{
					case Web_resource_state.Wait:
						return "";
					case Web_resource_state.Downloaded:
						return "√";
					case Web_resource_state.Stopped:
						return "# " + state_text;
					case Web_resource_state.Failed:
						return "×";
					default:
						return state_text;
				}
			}
		}

		public List<Web_resource_info> Children
		{
			get { return children; }
			set
			{
				children = value;
				NotifyPropertyChanged("Children");
			}
		}
		public Web_resource_info Parent { get; protected set; }

		public string Url
		{
			get { return url; }
			private set
			{
				url = value;
				NotifyPropertyChanged("Url");
			}
		}
		public Uri Uri
		{
			get
			{
				if (uri == null)
				{
					if (string.IsNullOrEmpty(url))
						return new Uri(url);
					else
						return null;
				}
				else
					return uri;
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

				return children.Count(c => c.State == Web_resource_state.Downloaded);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public class Comparer : IEqualityComparer<Web_resource_info>
		{
			public bool Equals(Web_resource_info x, Web_resource_info y)
			{
				if (x.Name == y.Name)
				{
					y.Name = x.Name + "'";
				}
				return x.Url == y.Url;
			}
			public int GetHashCode(Web_resource_info src_info)
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
		private Uri uri;
		private string name;
		private Web_resource_state state;
		private string state_text;
		private double size;
		private List<Web_resource_info> children;
	}

	public class Website_info
	{
		public Website_info(string name, string home, List<string> hosts)
		{
			Name = name;
			Home = home;
			Hosts = hosts;
		}

		public bool Is_inited = false;

		public string Name { get; set; }
		public string Home { get; set; }
		public List<string> Hosts { get; set; }
	}
}
