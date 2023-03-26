var test = new Test();
Console.WriteLine(test.StringField);

public partial class Test
{
	public string StringField => _stringField;

	[Save]
	private string _stringField, _anotherStringField = "Hello, World!";

	public Test()
	{
		RegisterSaveObject();
	}
}