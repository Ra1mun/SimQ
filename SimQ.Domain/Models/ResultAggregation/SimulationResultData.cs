using MongoDB.Bson.Serialization.Attributes;

namespace SimQ.Domain.Models.ResultAggregation;

public class SimulationResultData
{
    [BsonElement("endRealTime")]
    public double EndRealTime { get; set; }

    [BsonElement("maxRealTime")]
    public double MaxRealTime { get; set; }

    [BsonElement("currentEventsAmount")]
    public double CurrentEventsAmount { get; set; }

    [BsonElement("maxEventsAmount")]
    public double MaxEventsAmount { get; set; }

    [BsonElement("currentModelationTime")]
    public double CurrentModelationTime { get; set; }

    [BsonElement("maxModelationTime")]
    public double MaxModelationTime { get; set; }

    [BsonElement("currentGenerationError")]
    public double CurrentGenerationError { get; set; }

    [BsonElement("minGenerationError")]
    public double MinGenerationError { get; set; }

    [BsonElement("totalCalls")]
    public int TotalCalls { get; set; }

    [BsonElement("agentResults")]
    public List<AgentStatisticResult> AgentResults { get; set; } = new();

    [BsonElement("logs")]
    [BsonIgnoreIfNull]
    public List<SimulationLogEntry>? Logs { get; set; }

    public string ToText(string problemName)
    {
        var text = $"Информация по симуляционному моделированию задачи \"{problemName}\":";
        text += $"\nEndRealTime = {EndRealTime} (Max = {MaxRealTime})";
        text += $"\nCurrentEventsAmount = {CurrentEventsAmount} (Max = {MaxEventsAmount})";
        text += $"\nCurrentModelationTime = {CurrentModelationTime} (Max = {MaxModelationTime})";
        text += $"\nCurrentGenerationError = {CurrentGenerationError:E} (Min = {MinGenerationError:E})";

        if (AgentResults.Count == 0)
        {
            text += "\nДанные эмпирической функции распределения не определены.";
            return text;
        }

        foreach (var agent in AgentResults)
        {
            text += $"\nДанные эмпирической функции распределения {agent.AgentId}:";
            foreach (var (state, probability) in agent.StatesProbabilities)
            {
                text += string.Format("\n{0} {1:0.00000} ", state, probability);
            }
        }

        return text;
    }
}

