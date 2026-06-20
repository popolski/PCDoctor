using System;

namespace PCDoctor
{
    // Canal statique pour propager le score de sante et les demandes de navigation
    public static class AppState
    {
        public static event Action<int, int>? ScoreChanged;
        public static event Action<string>?   NavigateRequested;

        public static void NotifyScore(int ok, int total) =>
            ScoreChanged?.Invoke(ok, total);

        public static void RequestNavigate(string pageTag) =>
            NavigateRequested?.Invoke(pageTag);
    }
}
