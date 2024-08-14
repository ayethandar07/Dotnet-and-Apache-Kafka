using Confluent.Kafka;
using EmployeeApplicationWebApi.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EmployeeApplicationWebApi.Services.Producer;

public class KafkaProducerService
{
    private readonly KafkaSettings _kafkaSettings;

    public KafkaProducerService(IOptions<KafkaSettings> kafkaSettings)
    {
        _kafkaSettings = kafkaSettings.Value;
    }

    public async Task ProduceAsync(string key, object value)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            Acks = Enum.Parse<Acks>(_kafkaSettings.Acks!, true),
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        var message = new Message<string, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(value)
        };

        await producer.ProduceAsync(_kafkaSettings.Topic, message);
    }
}
