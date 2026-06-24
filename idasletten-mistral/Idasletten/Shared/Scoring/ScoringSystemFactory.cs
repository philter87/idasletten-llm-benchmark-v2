using Idasletten.Shared.Data.Enums;

namespace Idasletten.Shared.Scoring;

public class ScoringSystemFactory : IScoringSystemFactory
{
    private readonly IEloScoringSystem _eloScoringSystem;
    private readonly ITrueSkillScoringSystem _trueSkillScoringSystem;
    private readonly ILivesScoringSystem _livesScoringSystem;
    private readonly IWinCountScoringSystem _winCountScoringSystem;
    
    public ScoringSystemFactory(
        IEloScoringSystem eloScoringSystem,
        ITrueSkillScoringSystem trueSkillScoringSystem,
        ILivesScoringSystem livesScoringSystem,
        IWinCountScoringSystem winCountScoringSystem)
    {
        _eloScoringSystem = eloScoringSystem;
        _trueSkillScoringSystem = trueSkillScoringSystem;
        _livesScoringSystem = livesScoringSystem;
        _winCountScoringSystem = winCountScoringSystem;
    }
    
    public IScoringSystem GetScoringSystem(ScoreSystem scoreSystem)
    {
        return scoreSystem switch
        {
            ScoreSystem.Elo => _eloScoringSystem,
            ScoreSystem.TrueSkill => _trueSkillScoringSystem,
            ScoreSystem.Lives => _livesScoringSystem,
            ScoreSystem.WinCount => _winCountScoringSystem,
            _ => throw new ArgumentOutOfRangeException(nameof(scoreSystem), scoreSystem, null)
        };
    }
}
