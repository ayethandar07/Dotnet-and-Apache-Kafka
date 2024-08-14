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
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.GroupId,
            AutoOffsetReset = _kafkaSettings.AutoOffsetReset,
            EnableAutoCommit = _kafkaSettings.EnableAutoCommit
        };

        using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
        {
            consumer.Subscribe(_kafkaSettings.Topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);

                        if (consumeResult != null)
                        {
                            List<Employee>? employees = null;
                            Employee? employee = null;

                            using (JsonDocument doc = JsonDocument.Parse(consumeResult.Message.Value))
                            {
                                var rootElement = doc.RootElement;

                                if (rootElement.ValueKind == JsonValueKind.Array)
                                {
                                    // JSON is an array, deserialize to List<Employee>
                                    employees = JsonSerializer.Deserialize<List<Employee>>(consumeResult.Message.Value);
                                    _logger.LogInformation("Consumed employees count: {Count}", employees?.Count);
                                }
                                else if (rootElement.ValueKind == JsonValueKind.Object)
                                {
                                    // JSON is a single object, deserialize to Employee
                                    employee = JsonSerializer.Deserialize<Employee>(consumeResult.Message.Value);
                                    _logger.LogInformation("Consumed single employee: {employee}", employee);
                                    employees = employee != null ? new List<Employee> { employee } : null;
                                }
                                else
                                {
                                    _logger.LogWarning("Message is neither an array nor an object.");
                                    continue; // Skip processing if the JSON structure is unexpected
                                }
                            }

                            if (employees != null)
                            {
                                await Task.Run(async () =>
                                {
                                    using (var scope = _serviceProvider.CreateScope())
                                    {
                                        var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeReportDbContext>();

                                        foreach (var emp in employees)
                                        {
                                            var employeeReport = new EmployeeReport(Guid.NewGuid(), emp.Id, emp.Name, emp.Surname!);
                                            dbContext.Reports.Add(employeeReport);
                                        }

                                        await dbContext.SaveChangesAsync(stoppingToken);
                                    }

                                    consumer.Commit(consumeResult);
                                }, stoppingToken);
                            }
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
    }
}
