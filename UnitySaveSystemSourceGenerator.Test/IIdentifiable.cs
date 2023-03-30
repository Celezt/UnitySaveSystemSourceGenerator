using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celezt.SaveSystem
{
	public interface IIdentifiable
	{
		public Guid Guid { get; }
	}
}
