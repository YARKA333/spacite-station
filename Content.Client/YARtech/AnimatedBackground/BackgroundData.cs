namespace Content.Client.YARtech.AnimatedBackground
{
    public sealed class BackgroundData
    {
        public readonly Frame[] Frames;

        public int Counter;

        public float Timer;

        public BackgroundData(Frame[] frames)
        {
            Frames = frames;
        }

        public Frame Current()
        {
            return Frames[Counter];
        }

        public void Next()
        {
            Counter = (Counter + 1) % Frames.Length;
        }
    }

}
