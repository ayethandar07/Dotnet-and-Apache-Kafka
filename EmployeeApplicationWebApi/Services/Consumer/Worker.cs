using Confluent.Kafka;
using EmployeeApplicationWebApi.Database;
using EmployeeApplicationWebApi.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EmployeeApplicationWebApi.Services.Consumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _kafkaSettings;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IOptions<KafkaSettings> kafkaSettings)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _kafkaSettings = kafkaSettings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume asynchronously
        await Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private async void StartConsumerLoop(CancellationToken stoppingToken)
    {
        var consumerConfig = CreateConsumerConfig();

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_kafkaSettings.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessMessagesAsync(consumer, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker stopping due to cancellation request.");
        }
        finally
        {
            consumer.Close();
        }
    }

    private ConsumerConfig CreateConsumerConfig()
    {
        return new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.GroupId,
            AutoOffsetReset = _kafkaSettings.AutoOffsetReset,
            EnableAutoCommit = _kafkaSettings.EnableAutoCommit
        };
    }

    private async Task ProcessMessagesAsync(IConsumer<string, string> consumer, CancellationToken stoppingToken)
    {
        try
        {
            var consumeResult = consumer.Consume(stoppingToken);
            if (consumeResult == null) return;

            var employees = DeserializeMessage(consumeResult.Message.Value);
            if (employees != null)
            {
                await SaveEmployeesAsync(employees, stoppingToken);
                consumer.Commit(consumeResult);
            }
        }
        catch (ConsumeException ex)
        {
            _logger.LogError("Consume error: {Error}", ex.Error.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error: {Error}", ex.Message);
        }
    }

    private List<Employee>? DeserializeMessage(string message)
    {
        using var doc = JsonDocument.Parse(message);
        var rootElement = doc.RootElement;

        if (rootElement.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<Employee>>(message);
        }
        else if (rootElement.ValueKind == JsonValueKind.Object)
        {
            var employee = JsonSerializer.Deserialize<Employee>(message);
            return employee != null ? new List<Employee> { employee } : null;
        }

        _logger.LogWarning("Message is neither an array nor an object.");
        return null;
    }

    private async Task SaveEmployeesAsync(List<Employee> employees, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeReportDbContext>();

        foreach (var emp in employees)
        {
            var employeeReport = new EmployeeReport(Guid.NewGuid(), emp.Id, emp.Name, emp.Surname!);
            dbContext.Reports.Add(employeeReport);
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}
