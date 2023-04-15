namespace Celezt.SaveSystem
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public class SaveAttribute : Attribute
	{
		public string? Identifier { get; set; }
		public SaveSettings Setting => _setting;

		private SaveSettings _setting;
		
		public SaveAttribute(SaveSettings setting = SaveSettings.Default) 
		{
			_setting = setting;
		}
	}

	public enum SaveSettings
	{
		Default,
		Persistent,
	}
}