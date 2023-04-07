using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celezt.SaveSystem
{
	public struct EntryKey
	{
		/// <summary>
		/// Subscribe to a sub entry.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		/// <returns>If it exist.</returns>
		public EntryKey SetSubEntry(string id, Action<object> onLoad, bool loadPreviousSave = true)
		{
			return new EntryKey();
		}


		/// <summary>
		/// Add or set sub entry.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onSave">Set value.</param>
		/// <param name="onLoad">Get value when loading.</param>
		/// <param name="loadPreviousSave">Call onLoad if a save exist.</param>
		public EntryKey SetSubEntry(string id, Func<object> onSave, Action<object> onLoad, bool loadPreviousSave = true)
		{
			return new EntryKey();
		}

		/// <summary>
		/// Add or set entry.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <param name="onSave">Set value when saving.</param>
		public EntryKey SetSubEntry(string id, Func<object> onSave)
		{
			return new EntryKey();
		}
	}
}
