using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celezt.SaveSystem
{
	public struct EntryKey
	{
		public EntryKey SetSubEntry(string id, Action<object> onLoad, bool loadPreviousSave = true)
		{
			return new EntryKey();
		}

		public EntryKey SetSubEntry(string id, Func<object> onSave, Action<object> onLoad, bool loadPreviousSave = true)
		{
			return new EntryKey();
		}

		public EntryKey SetSubEntry(string id, Func<object> onSave)
		{
			return new EntryKey();
		}
	}
}
