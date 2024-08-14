using Confluent.Kafka;

namespace EmployeeApplicationWebApi.Models;

public record KafkaSettings
{
    public string? BootstrapServers { get; set; }
    public string? Topic { get; set; }
    public string? GroupId { get; set; }
    public string? Acks { get; set; }
    public AutoOffsetReset AutoOffsetReset { get; set; }
    public bool EnableAutoCommit { get; set; }
}
