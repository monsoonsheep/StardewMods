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
            if (operation.StartsWith("move"))
            {
                return Move.Parse(operation);
            }
            else if (operation.StartsWith("copy"))
            {
                return Copy.Parse(operation);
            }
            else if (operation.StartsWith("erase"))
            {
                return Erase.Parse(operation);
            }
            else
            {
                return null;
            }
        }
    }
}
