using System;

namespace PCDoctor
{
    // Canal statique pour propager le score de sante vers le badge du menu
    public static class AppState
    {
        public static event Action<int, int>? ScoreChanged;

        public static void NotifyScore(int ok, int total) =>
            ScoreChanged?.Invoke(ok, total);
    }
}
