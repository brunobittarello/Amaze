public class StageCreationSettings
{
    public int SizeMin { get; set; }
    public int SizeMax { get; set; }
    public int MovesMin { get; set; }
    public int MovesMax { get; set; }
    public float BifurcationChance { get; set; }
}

public enum StageDifficult
{
    Easy,
    Modarate,
    Hard,
    VeryHard
}