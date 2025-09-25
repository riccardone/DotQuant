namespace DotQuant.Ai.Contracts;

public interface IAgentAnalysisResult
{
    List<(string Title, string Value)> ToSections();
}