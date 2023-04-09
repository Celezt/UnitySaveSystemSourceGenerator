using Celezt.SaveSystem;
using UnityEngine;
using Namespace;

var test = new Test();

namespace Namespace
{
	public partial class Test : MonoBehaviour, IIdentifiable
	{
		[Save]
		protected const double CONST_VALUE = double.MaxValue;

		public Guid Guid { get; } = Guid.NewGuid();

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
			RegisterSaveObject();
		}

		[Save]
		private void SetDrag(float value)
		{

		}

		[Save]
		private float GetDrag()
		{
			return 1;
		}
	}

	//public partial struct Test2
	//{
	//	[Save]
	//	public float Speed { get; set; }
	//}
}
