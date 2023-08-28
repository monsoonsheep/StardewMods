namespace FarmCafe.Framework.Models
{
	public class CustomerModel
	{
		public string Name { get; set; }
		public string TilesheetPath { get; set; }
		public string Portrait { get; set; }

		public new string ToString()
		{
			return $"Model: [Name: {Name}, Tilesheet: {TilesheetPath}, Portrait name: {Portrait}]";

		}
	}
}
