
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
        await Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
    }

    private void StartConsumerLoop(CancellationToken stoppingToken)
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
                            var employee = JsonSerializer.Deserialize<Employee>(consumeResult.Message.Value);
                            if (employee != null)
                            {
                                _logger.LogInformation("Consumed employee: {employee}", employee);

                                // Run database operations asynchronously
                                Task.Run(async () =>
                                {
                                    using (var scope = _serviceProvider.CreateScope())
                                    {
                                        var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeReportDbContext>();
                                        var employeeReport = new EmployeeReport(Guid.NewGuid(), employee.Id, employee.Name, employee.Surname);
                                        dbContext.Reports.Add(employeeReport);
                                        await dbContext.SaveChangesAsync(stoppingToken);
                                    }

                                    consumer.Commit(consumeResult);
                                }, stoppingToken).Wait();
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
