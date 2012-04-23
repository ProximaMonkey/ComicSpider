using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Linq;

namespace ys
{
	public enum Web_resource_state : int
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
			Path = path;
			Parent = parent;
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

		#region Status

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
			get
			{
				switch (state)
				{
					case Web_resource_state.Wait:
						return "-";
					case Web_resource_state.Downloading:
						return "*";
					case Web_resource_state.Downloaded:
						return "√";
					case Web_resource_state.Stopped:
						return "#";
					case Web_resource_state.Failed:
						return "X";
					default:
						return "";
				}
			}
		}

		public double Progress
		{
			get
			{
				return progress;
			}
			set
			{
				progress = value;
				NotifyPropertyChanged("Progress_double_text");
			}
		}
		public string Progress_int_text
		{
			get
			{
				int count = Count;
				if (count == 0) return string.Empty;
				return string.Format("{0} / {1}", Downloaded, count);
			}
		}
		public string Progress_double_text
		{
			get
			{
				if (progress > 0)
					return string.Format("{0:0}%", progress);
				else
					return string.Empty;
			}
		}

		public double Speed
		{
			get
			{
				return speed;
			}
			set
			{
				speed = value;
				NotifyPropertyChanged("Speed_text");
			}
		}
		public string Speed_text
		{
			get
			{
				if (state == Web_resource_state.Downloaded)
					return string.Empty;

				if (speed > 0)
					return string.Format("{0:0}KB/s", speed);
				else
					return string.Empty;
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
				NotifyPropertyChanged("Size_text");
			}
		}
		public string Size_text
		{
			get
			{
				if (size > 0)
				{
					return string.Format("{0:0.00}MB", size);
				}
				else
				{
					return string.Empty;
				}
			}
		}

		public int Index { get; set; }
		public string Path { get; set; }

		#endregion

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


		public void NotifyPropertyChanged(String prop_name)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(prop_name));
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

		private string url;
		private Uri uri;
		private string name;

		private Web_resource_state state;
		private double progress;
		private double size;
		private double speed;

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
