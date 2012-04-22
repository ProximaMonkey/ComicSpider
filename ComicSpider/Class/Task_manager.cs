using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ys;
using System.Threading;
using System.Collections.ObjectModel;

namespace ComicSpider
{
	class Task_manager
	{
		public Task_manager()
		{
			Volumes = new ObservableCollection<Web_resource_info>();
		}

		public void Stop()
		{
			foreach (var vol in Volumes)
			{
				if (vol.State == Web_resource_state.Downloading)
					vol.State = Web_resource_state.Stopped;

				if (vol.Count == 0) continue;

				foreach (var page in vol.Children)
				{
					if (page.Count > 0 &&
						page.Children[0].State == Web_resource_state.Downloading)
					{
						page.State = Web_resource_state.Stopped;
						page.Children[0].State = Web_resource_state.Stopped;
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
				var volume = Peek_downloading(Volumes);
				return Dequeue(volume);
			}
		}
		public Web_resource_info Files_dequeue()
		{
			lock (file_lock)
			{
				var volume = Peek_downloading(Volumes);
				var page = Peek_downloading(volume);
				return Dequeue(page);
			}
		}

		/***************************** Private ********************************/

		private object volume_lock = new object();
		private object page_lock = new object();
		private object file_lock = new object();

		private Web_resource_info Dequeue(Web_resource_info parent)
		{
			var item = Peek(parent);

			if (item == null) return null;

			item.State = Web_resource_state.Downloading;
			return item;
		}
		private Web_resource_info Dequeue(ObservableCollection<Web_resource_info> volumes)
		{
			var item = Peek(volumes);

			if (item == null) return null;

			item.State = Web_resource_state.Downloading;
			return item;
		}

		private Web_resource_info Peek(Web_resource_info parent)
		{
			if (parent == null || parent.Count == 0) return null;

			return parent.Children.FirstOrDefault(v =>
			{
				return (v.State != Web_resource_state.Downloading) &&
					(v.State != Web_resource_state.Downloaded);
			});
		}
		private Web_resource_info Peek(ObservableCollection<Web_resource_info> volumes)
		{
			if (volumes == null || volumes.Count == 0) return null;

			return volumes.FirstOrDefault(v =>
			{
				return (v.State != Web_resource_state.Downloading) &&
					(v.State != Web_resource_state.Downloaded);
			});
		}

		private Web_resource_info Peek_downloading(Web_resource_info parent)
		{
			if (parent == null || parent.Count == 0) return null;

			return parent.Children.FirstOrDefault(v => v.State == Web_resource_state.Downloading);
		}
		private Web_resource_info Peek_downloading(ObservableCollection<Web_resource_info> volumes)
		{
			if (volumes == null || volumes.Count == 0) return null;

			return volumes.FirstOrDefault(v => v.State == Web_resource_state.Downloading);
		}
	}
}
