# UnitySaveSystemSourceGenerator

Source generator using [Roslyn](https://github.com/dotnet/roslyn) used in [Unity Save System](https://github.com/Celezt/UnitySaveSystem). It generates code when the 'Save' attribute is present and analyses the code for misuse of this feature.

```cs
public partial class Example : MonoBehaviour
{
     [Save]
     public const string EXAMPLE_CONST = "This is a const string";

     [Save]
     public Guid ExampleGuid { get; set; } = Guid.NewGuid();

     [Save]
     private int _exampleValue;

     [Save]
     private void SetExampleValue(int value) => _exampleValue = value;

     private void Awake()
     {
          RegisterSaveObject();
     }
}

// Auto generated code.
public partial class Example
{
     /// ... ///
     protected void RegisterSaveObject()
     {
          global::Celezt.SaveSystem.SaveSystem.GetEntryKey(this)
               .SetSubEntry("example_const", () => EXAMPLE_CONST)
               .SetSubEntry("example_guid", () => ExampleGuid, value => ExampleGuid = (Guid)value)
               .SetSubEntry("example_value", () => _exampleValue, value => SetExampleValue((int)value));
     }
}
```
