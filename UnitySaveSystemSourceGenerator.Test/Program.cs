using Celezt.SaveSystem;
using UnityEngine;
using Namespace;

var test = new Test();

namespace Namespace
{
	public partial class Test : MonoBehaviour, IIdentifiable
	{
		[Save(Identifier = "new_constant_value")]
		protected const double CONST_VALUE = double.MaxValue;

		public Guid Guid { get; } = Guid.NewGuid();

		[Save]
		public float Speed { get; set; }

		[Save]
		public Vector3 Velocity { get; }

		[Save(SaveSetting.Persistent)]
		private readonly Vector3 _position;

		[Save]
		private string _stringField = string.Empty, _anotherStringField = "Hello, World!";

		private void Awake()
		{
			RegisterSaveObject();
		}

		[Save]
		private void SetVelocity(Vector3 value)
		{

		}

		[Save]
		private Vector3 GetVelocity()
		{
			return default;
		}
	}

	//public partial struct Test2
	//{
	//	[Save]
	//	public float Speed { get; set; }
	//}
}
