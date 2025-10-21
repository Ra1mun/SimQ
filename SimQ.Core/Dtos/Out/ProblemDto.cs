using SimQCore.Modeller.Models;

namespace SimQ.Core.Dtos.Out;

public class ProblemResponse
{
    public string ProblemName { get; set; }
    public List<IModellingAgent> Agents { get; set; }
    public Dictionary<string, string[]> Links { get; set; }
    public List<ResultDto> Results { get; set; }
}

public class ProblemListReponse
{
    public List<ProblemResponse> Data { get; set; }
    public int Total { get; set; } // Сделать отдельный запрос, где можно получить общее количество
}