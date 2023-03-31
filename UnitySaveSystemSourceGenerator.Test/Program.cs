using Celezt.SaveSystem;
using UnityEngine;

var test = new Test();

public class Test : MonoBehaviour
{
	[Save]
	private Vector3 _position;

	private string _stringField = string.Empty, _anotherStringField = "Hello, World!";

	public Test()
	{

	}
}