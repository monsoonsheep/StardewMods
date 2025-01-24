using StardewMods.CustomFarmerAnimations.Framework.SpriteEditing;

namespace StardewMods.CustomFarmerAnimations.Framework
{
    public class CustomFarmerAnimationModel
    {
        public string Name { get; set; } = null!;

        public List<string> Operations { get; set; } = [];

        private EditOperation[] editOperations = null!;
        internal EditOperation[] EditOperations
        {
            get
            {
                if (this.editOperations == null)
                {
                    this.editOperations = new EditOperation[this.Operations.Count];
                    for (int i = 0; i < this.Operations.Count; i++)
                    {
                        this.editOperations[i] = ParseOperation(this.Operations[i]) ?? throw new Exception();
                    }
                }

                return this.editOperations;
            }
        }

        private static EditOperation? ParseOperation(string operation)
        {
            string[] split = operation.Split(' ');

            return split[0] switch
            {
                "move" => Move.Parse(split),
                "copy" => Copy.Parse(split),
                "erase" => Erase.Parse(split),
                _ => null,
            };
        }
    }
}
