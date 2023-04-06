using Celezt.SaveSystem;
using UnityEngine;
using Namespace;

var test = new Test();

namespace Namespace
{
	public partial class Test : IIdentifiable
	{
		public Guid Guid => throw new NotImplementedException();

		[Save]
		public float Speed { get; set; }

		[Save]
		public Vector3 Velocity { get; }

		[Save]
		private readonly Vector3 _position;

		[Save]
		private string _stringField = string.Empty, _anotherStringField = "Hello, World!";

		public Test()
		{
			//RegisterSaveObject();
		}
	}

	//public partial struct Test2
	//{
	//	[Save]
	//	public float Speed { get; set; }
	//}
}
