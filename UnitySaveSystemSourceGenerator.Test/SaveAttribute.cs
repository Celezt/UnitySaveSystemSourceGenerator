namespace Celezt.SaveSystem
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public class SaveAttribute : Attribute
	{
		public string? Identifier { get; set; }
		public SaveSetting Setting => _setting;

		private SaveSetting _setting;
		
		public SaveAttribute(SaveSetting setting = SaveSetting.Default) 
		{
			_setting = setting;
		}
	}

	public enum SaveSetting
	{
		Default,
		Persistent,
	}
}