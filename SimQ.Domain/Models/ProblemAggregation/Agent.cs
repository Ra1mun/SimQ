using MongoDB.Bson.Serialization.Attributes;

namespace SimQ.Domain.Models.ProblemAggregation;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(Source), typeof(ServiceBlock), typeof(Buffer))]
public abstract class Agent
{
    [BsonElement("id")]
    public string Id { get; set; }

    [BsonElement("type")]
    public AgentType Type { get; protected set; }
    
    [BsonElement("reflectionType")]
    public string ReflectionType { get; set; }
    
    [BsonElement("params")]
    public AgentParams Parameters { get; set; }
}

[BsonDiscriminator("ServiceBlock")]
public class ServiceBlock : Agent
{
    public ServiceBlock()
    {
        Type = AgentType.SERVICE_BLOCK;
    }
    
    [BsonElement]
    public List<Buffer> BindedBuffer { get; set; } = new();

    public void BindBuffer(Buffer agent)
    {
        if (agent.Type != AgentType.BUFFER)
        {
            return;
        }
        
        BindedBuffer.Add(agent);
    }
}

[BsonDiscriminator("Source")]
public class Source : Agent
{
    public Source()
    {
        Type = AgentType.SOURCE;
    }
}

[BsonDiscriminator("Buffer")]
public class Buffer : Agent
{
    public Buffer()
    {
        Type = AgentType.BUFFER;
    }
}

public enum AgentType {
    SOURCE,         // Источник входящих заявок
    SERVICE_BLOCK,  // Блок приборов
    BUFFER,         // Очередь
    CALL,           // Заявка
    ORBIT           // Орбита
}