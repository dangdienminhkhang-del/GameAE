using System;
using System.Collections.Generic;

namespace Game.Core.Challenge
{
    [Serializable]
    public class ChallengeMatch
    {
        public string matchId;
        public string opponentId;
        public string opponentName;
        public ChallengeResult result;
        public double myHpRemaining;
        public double opponentHpRemaining;
        public int durationSeconds;
        public long timestampUnix;
    }

    [Serializable]
    public class ChallengeHistory
    {
        public const int MaxMatches = 100;
        public List<ChallengeMatch> matches = new List<ChallengeMatch>();

        public void AddMatch(ChallengeMatch match)
        {
            matches.Insert(0, match);
            if (matches.Count > MaxMatches)
            {
                matches.RemoveRange(MaxMatches, matches.Count - MaxMatches);
            }
        }
    }
}
