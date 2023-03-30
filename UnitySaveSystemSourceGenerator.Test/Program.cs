using Celezt.SaveSystem;
using UnityEngine;

var test = new Test();
Console.WriteLine(test.StringField);

public partial class Test : MonoBehaviour
{
	public string StringField => _stringField;

	[Save]
	private int _someInt;

	[Save]
	private string _stringField = string.Empty, _anotherStringField = "Hello, World!";

	public Test()
	{
		RegisterSaveObject();
	}
}