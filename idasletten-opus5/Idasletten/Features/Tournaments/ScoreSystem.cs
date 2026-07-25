namespace Idasletten.Features.Tournaments;

public enum ScoreSystem
{
    /// <summary>Classic Elo. A team rating is the average rating of its players.</summary>
    Elo = 0,

    /// <summary>Microsoft TrueSkill via the Moserware.Skills library. Score is the conservative rating.</summary>
    TrueSkill = 1,

    /// <summary>Everyone starts with 3 lives and loses one for every lost match.</summary>
    Lives = 2,

    /// <summary>Score is simply the number of won matches, goal difference breaks ties.</summary>
    WinCount = 3,
}

public static class ScoreSystemInfo
{
    public static string Title(this ScoreSystem system) => system switch
    {
        ScoreSystem.Elo => "Elo",
        ScoreSystem.TrueSkill => "TrueSkill",
        ScoreSystem.Lives => "Liv",
        ScoreSystem.WinCount => "Antal sejre",
        _ => system.ToString(),
    };

    public static string Description(this ScoreSystem system) => system switch
    {
        ScoreSystem.Elo =>
            "Klassisk skakrating. Alle starter på 1200 og tager rating fra modstanderen når de vinder. " +
            "Et hold rates efter gennemsnittet af spillernes rating, så det giver mest at slå et stærkt hold.",
        ScoreSystem.TrueSkill =>
            "Microsofts TrueSkill (Moserware.Skills). Hver spiller har et skøn over sit niveau og en usikkerhed; " +
            "scoren er den konservative rating (gennemsnit - 3 x afvigelse), som stiger i takt med at systemet bliver sikkert på dig.",
        ScoreSystem.Lives =>
            "Den sidste viking der står tilbage vinder. Alle starter med 3 liv og mister et for hver tabt kamp. " +
            "Scoren er antallet af liv der er tilbage.",
        ScoreSystem.WinCount =>
            "Helt enkelt antallet af vundne kampe. Målforskellen afgør ved pointlighed.",
        _ => string.Empty,
    };

    /// <summary>The unit shown next to the score in the scoreboard.</summary>
    public static string ScoreLabel(this ScoreSystem system) => system switch
    {
        ScoreSystem.Elo => "Elo",
        ScoreSystem.TrueSkill => "Niveau",
        ScoreSystem.Lives => "Liv",
        ScoreSystem.WinCount => "Sejre",
        _ => "Score",
    };

    /// <summary>Number of decimals to render for a score of this system.</summary>
    public static int Decimals(this ScoreSystem system) => system switch
    {
        ScoreSystem.TrueSkill => 1,
        _ => 0,
    };
}
