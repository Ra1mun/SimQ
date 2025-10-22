using MongoDB.Bson;
using MongoDB.Driver;
using SimQ.Core.Modeller.Models;
using SimQ.Core.Modeller.Models.UserModels;
using SimQ.Core.Models.Distributions;
using SimQ.Domain.Models.Migration;
using SimQ.Domain.Models.ProblemAggregation;
using Buffer = SimQ.Domain.Models.ProblemAggregation.Buffer;
using Source = SimQ.Domain.Models.ProblemAggregation.Source;

namespace SimQ.WebApi.Migrations;

public class AddProblemsMigration : IMigration
{
    public int Version => 2;

    public async Task ApplyAsync(IMongoDatabase database)
    {
        var problems = new List<Problem>
        {
            CreateProblem(1, 2, 1, 0),        // M/M/1/0
            CreateProblem(1, 1.5, 2, 5),      // M/M/2/5
            CreateProblem(0.8, 2, int.MaxValue, 0), // M/M/∞
            CreateProblem(1.2, 3, 3, int.MaxValue)  // M/M/3/∞
        };

        var collection = database.GetCollection<Problem>("Problems");
        
        // Вставляем только если коллекция пуста (чтобы не дублировать при повторном запуске)
        var count = await collection.CountDocumentsAsync(_ => true);
        if (count == 0)
        {
            await collection.InsertManyAsync(problems);
        }
    }
    
    private static Problem CreateProblem(double la, double mu, int s, int q)
    {
        var agents = new List<Agent>();
        var links = new Dictionary<string, string[]>();

        // --- Source ---
        var source = new Source
        {
            Id = Guid.NewGuid().ToString(),
            ReflectionType = "Source",
            Parameters = new AgentParams
            {
                Distribution = new DistributionParams
                {
                    ReflectionType = nameof(ExponentialDistribution),
                    Arguments = [new BsonDouble(la)]
                }
            }
        };
        agents.Add(source);

        // --- ServiceBlocks ---
        var serviceBlock = new ServiceBlock
        {
            Id = Guid.NewGuid().ToString(),
            ReflectionType = s == int.MaxValue ? nameof(InfServiceBlocks) : nameof(FinServiceBlocks),
            Parameters = new AgentParams
            {
                Arguments = s == int.MaxValue
                    ? null
                    : [new BsonInt32(s)],
                Distribution = new DistributionParams
                {
                    ReflectionType = nameof(ExponentialDistribution),
                    Arguments = [new BsonDouble(mu)]
                }
            }
        };
        agents.Add(serviceBlock);

        // --- Buffers ---
        Buffer? buffer = null;
        if (q > 0 && q != int.MaxValue)
        {
            buffer = new Buffer
            {
                Id = Guid.NewGuid().ToString(),
                ReflectionType = nameof(QueueBuffer),
                Parameters = new AgentParams
                {
                    Arguments = [new BsonInt32(q)]
                }
            };
            agents.Add(buffer);
            serviceBlock.BindedBuffer.Add(buffer);
        }

        links[source.Id] = [serviceBlock.Id];

        string name;
        if (s == int.MaxValue)
            name = $"Example M={la}/M={mu}/Inf";
        else if (q == int.MaxValue)
            name = $"Example M={la}/M={mu}/n={s}/Inf";
        else
            name = $"Example M={la}/M={mu}/n={s}/c={q}";

        return new Problem
        {
            Name = name,
            Agents = agents,
            Links = links,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task RollbackAsync(IMongoDatabase database)
    { }
}