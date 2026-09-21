using System;

namespace TankGame.Core
{
    public sealed class StageProgression
    {
        public int StageCount { get; }
        public int CurrentIndex { get; private set; }
        public bool HasNext => CurrentIndex + 1 < StageCount;

        public StageProgression(int stageCount)
        {
            if (stageCount < 1) throw new ArgumentOutOfRangeException(nameof(stageCount));
            StageCount = stageCount;
        }

        public bool TryAdvance()
        {
            if (!HasNext) return false;
            CurrentIndex++;
            return true;
        }

        public void Restart() => CurrentIndex = 0;
    }
}
