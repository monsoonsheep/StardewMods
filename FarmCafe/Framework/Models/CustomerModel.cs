namespace FarmCafe.Framework.Models
{
	public class CustomerModel
	{
		public string Name { get; set; }
		public string TilesheetPath { get; set; }
		public AnimationData Animation { get; set; }


		public string GetId()
		{
			return Name;
		}

		public new string ToString()
		{
			return $"Model: [Name: {Name}, Tilesheet: {TilesheetPath}]"
				   + $"Animation: {Animation.NumberOfFrames} frames, {Animation.Duration}ms each, Starting {Animation.StartingFrame}";

		}
	}
}
