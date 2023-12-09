namespace VisitorFramework.Models
{
    public class AnimationData
    {
        public AnimationData()
        {
        }

        public AnimationData(int duration, int startingFrame, int numberOfFrames)
        {
            Duration = duration;
            StartingFrame = startingFrame;
            NumberOfFrames = numberOfFrames;
        }

        public int StartingFrame { get; set; }

        public int NumberOfFrames { get; set; }

        public int Duration { get; set; }

        public override string ToString()
        {
            return $"[StartingFrame: {StartingFrame} | NumberOfFrames: {NumberOfFrames} | Duration: {Duration}]";
        }
    }
}
