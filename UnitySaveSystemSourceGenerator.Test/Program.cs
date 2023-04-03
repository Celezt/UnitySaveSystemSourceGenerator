using Celezt.SaveSystem;
using UnityEngine;
using Namespace;

var test = new Test();

namespace Namespace
{
	public partial class Test : MonoBehaviour
	{
		[Save]
		public float Speed { get; set; }

		[Save]
		private Vector3 _position;

		[Save]
		private string _stringField = string.Empty, _anotherStringField = "Hello, World!";

		public Test()
		{
			//RegisterSaveObject();
		}
	}

	public partial class Test2
	{
		public Guid Guid { get; set; }
		[Save]
		public float Speed { get; set;}
	}
}
