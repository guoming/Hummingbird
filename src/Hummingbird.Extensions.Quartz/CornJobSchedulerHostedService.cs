
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Hummingbird.Extensions.Quartz
{
    public class CornJobSchedulerHostedService : Microsoft.Extensions.Hosting.IHostedService
        {
            private readonly ILogger _logger;
            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly CornJobConfiguration _cornJobConfiguration;
            private readonly IScheduler _scheduler;

            private readonly IServiceProvider _serviceProvider;



            public CornJobSchedulerHostedService(
                IServiceProvider serviceProvider,
                IScheduler scheduler,
                CornJobConfiguration cornJobConfiguration,
                ILogger<CornJobSchedulerHostedService> logger)
            {
                _serviceProvider = serviceProvider;
                _scheduler = scheduler;
                _cornJobConfiguration = cornJobConfiguration;
                _cancellationTokenSource = new CancellationTokenSource();
                _logger = logger;
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                if (_cornJobConfiguration.Open)
                {
                    try
                    {



                        for (var i = 0; i < _cornJobConfiguration.CronTriggers.Length; i++)
                        {
                            try
                            {
                                var trigger = _cornJobConfiguration.CronTriggers[i];

                                if (trigger.Open)
                                {

                                    if (trigger.Configuration == null)
                                    {
                                        trigger.Configuration = new JobDataMap();
                                    }


                                    var jobType = Type.GetType(trigger.JobType);
                                    if (jobType != null)
                                    {
                                        var job = JobBuilder.Create(jobType)
                                             .WithIdentity(trigger.JobName, trigger.JobGroup)
                                            .SetJobData(trigger.Configuration).Build();    //创建一个任务                                
                                        var cronTrigger = TriggerBuilder.Create()
                                            .WithIdentity(trigger.Name, trigger.Group)
                                            .StartNow()
                                            .WithCronSchedule(trigger.Expression)
                                            .Build();

                                        await _scheduler.ScheduleJob(job, cronTrigger, cancellationToken);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, ex.Message);
                            }


                        }

                        await _scheduler.Start();



                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }

                }

            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                _cancellationTokenSource.Cancel();

                return Task.CompletedTask;
            }
        }
    }


