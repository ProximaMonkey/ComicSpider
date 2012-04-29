using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ys;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace ComicSpider
{
	class Task_manager
	{
		public Task_manager()
		{
			Volumes = new ObservableCollection<Web_resource_info>();

			monitor = new System.Timers.Timer();
			monitor.Interval = 3 * 60 * 1000;
			monitor.Elapsed += new System.Timers.ElapsedEventHandler(monitor_Elapsed);
		}

		public void Start_monitor()
		{
			monitor.Start();
		}
		public void Reset_failed_items()
		{
			foreach (var vol in Volumes)
			{
				if (vol.State == Web_resource_state.Failed)
					vol.State = Web_resource_state.Wait;

				if (vol.Count == 0) continue;

				foreach (var page in vol.Children)
				{
					if (page.State == Web_resource_state.Failed)
					{
						page.State = Web_resource_state.Wait;
					}

					if (page.Count > 0)
					{
						page.Children[0].State = Web_resource_state.Wait;
					}
				}
			}
		}
		public void Stop()
		{
			monitor.Stop();

			foreach (var vol in Volumes)
			{
				if (vol.State == Web_resource_state.Downloading)
					vol.State = Web_resource_state.Wait;

				if (vol.Count == 0) continue;

				foreach (var page in vol.Children)
				{
					if (page.State == Web_resource_state.Downloading)
					{
						page.State = Web_resource_state.Wait;
						page.Speed = 0;
					}

					if (page.Count > 0)
					{
						page.Children[0].State = Web_resource_state.Wait;
					}
				}
			}
		}

		public ObservableCollection<Web_resource_info> Volumes { get; private set; }

		public Web_resource_info Volumes_dequeue()
		{
			lock (volume_lock)
			{
				return Dequeue(Volumes);
			}
		}
		public Web_resource_info Pages_dequeue()
		{
			lock (page_lock)
			{
				// Tranverse the tree for performance. Skip the undownloading node.
				foreach (var vol in Volumes)
				{
					if(vol.State != Web_resource_state.Downloading ||
						vol.Count == 0)
						continue;

					foreach (var page in vol.Children)
					{
						if (page.State == Web_resource_state.Wait)
						{
							page.State = Web_resource_state.Downloading;
							return page;
						}
					}
				}
				return null;
			}
		}
		public Web_resource_info Files_dequeue()
		{
			lock (file_lock)
			{
				// Tranverse the tree for performance. Skip the undownloading node.
				foreach (var vol in Volumes)
				{
					if (vol.State != Web_resource_state.Downloading ||
						vol.Count == 0)
						continue;

					foreach (var page in vol.Children)
					{
						if (page.State != Web_resource_state.Downloading ||
							page.Count == 0)
							continue;

						foreach (var file in page.Children)
						{
							if (file.State == Web_resource_state.Wait)
							{
								file.State = Web_resource_state.Downloading;
								return file;
							}
						}
					}
				}
				return null;
			}
		}

		/***************************** Private ********************************/

		private object volume_lock = new object();
		private object page_lock = new object();
		private object file_lock = new object();

		private System.Timers.Timer monitor;

		private void monitor_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (Dashboard.Instance.Is_all_left_failed)
			{
				Reset_failed_items();
			}
		}

		private Web_resource_info Dequeue(Collection<Web_resource_info> volumes)
		{
			var item = Peek(volumes);

			if (item == null) return null;

			item.State = Web_resource_state.Downloading;
			return item;
		}

		private Web_resource_info Peek(Collection<Web_resource_info> volumes)
		{
			if (volumes == null || volumes.Count == 0) return null;

			return volumes.FirstOrDefault(v =>
			{
				return v.State == Web_resource_state.Wait;
			});
		}
	}
}
