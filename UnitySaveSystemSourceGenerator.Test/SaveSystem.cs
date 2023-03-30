using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Celezt.SaveSystem
{
	public static class SaveSystem
	{
		/// <summary>
		/// Get or add <see cref="EntryKey"/>. Used to add sub entries. Get from existing <see cref="SaveBehaviour"/>. If none exist, returns null.
		/// </summary>
		/// <param name="monoBehaviour">GetComponentInParent for <see cref="SaveBehaviour"/>.</param>
		/// <returns><see cref="EntryKey"/>.</returns>
		public static EntryKey GetEntryKey(MonoBehaviour monoBehaviour)
		{
			return new EntryKey();
		}

		/// <summary>
		/// Get or add <see cref="EntryKey"/>. Used to add sub entries.
		/// </summary>
		/// <param name="guid">Identifier.</param>
		/// <returns><see cref="EntryKey"/>.</returns>
		public static EntryKey GetEntryKey(Guid guid)
		{
			return new EntryKey();
		}
	}
}
