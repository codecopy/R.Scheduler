﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using log4net;
using Newtonsoft.Json;
using Quartz;
using Quartz.Job;
using R.Scheduler.Contracts.JobTypes.Email.Model;
using R.Scheduler.Contracts.Model;
using R.Scheduler.Controllers;
using R.Scheduler.Interfaces;
using StructureMap;

namespace R.Scheduler.Email.Controllers
{
    public class EmailsController : BaseCustomJobController
    {
        private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        readonly ISchedulerCore _schedulerCore;

        protected EmailsController()
        {
            _schedulerCore = ObjectFactory.GetInstance<ISchedulerCore>();
        }

        // GET api/values 
        [Route("api/emails")]
        public IEnumerable<EmailJob> Get()
        {
            Logger.Info("Entered EmailsController.Get().");

            var jobDetails = _schedulerCore.GetJobDetails(typeof (SendMailJob));

            return jobDetails.Select(jobDetail =>
                                                    new EmailJob
                                                    {
                                                        JobName = jobDetail.Key.Name,
                                                        JobGroup = jobDetail.Key.Group,
                                                        SchedulerName = _schedulerCore.SchedulerName
                                                    }).ToList();

        }

        /// <summary>
        /// Schedules a temporary job for an immediate execution
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="jobGroup"></param>
        /// <returns></returns>
        [AcceptVerbs("POST")]
        [Route("api/emails/{jobName}/{jobGroup?}")]
        public QueryResponse Execute([FromBody]string jobName, [FromBody]string jobGroup = null)
        {
            Logger.InfoFormat("Entered EmailsController.Execute(). jobName = {0}, jobName = {1}", jobName, jobGroup);

            var response = new QueryResponse { Valid = true };

            try
            {
                _schedulerCore.ExecuteJob(jobName, jobGroup);
            }
            catch (Exception ex)
            {
                response.Valid = false;
                response.Errors = new List<Error>
                {
                    new Error
                    {
                        Code = "ErrorExecutingJob",
                        Type = "Server",
                        Message = string.Format("Error: {0}", ex.Message)
                    }
                };
            }

            return response;
        }

        /// <summary>
        /// Removes all triggers.
        /// </summary>
        /// <param name="jobName"></param>
        /// <param name="jobGroup"></param>
        /// <returns></returns>
        [AcceptVerbs("POST")]
        [Route("api/emails/unschedule/{jobName}/{jobGroup?}")]
        public QueryResponse Unschedule([FromBody]string jobName, [FromBody]string jobGroup = null)
        {
            Logger.InfoFormat("Entered EmailsController.Unschedule(). jobName = {0}, jobName = {1}", jobName, jobGroup);

            var response = new QueryResponse { Valid = true };

            try
            {
                _schedulerCore.RemoveJobTriggers(jobName, jobGroup);
            }
            catch (Exception ex)
            {
                response.Valid = false;
                response.Errors = new List<Error>
                {
                    new Error
                    {
                        Code = "ErrorUnschedulingJob",
                        Type = "Server",
                        Message = string.Format("Error: {0}", ex.Message)
                    }
                };
            }

            return response;
        }

        [AcceptVerbs("POST")]
        [Route("api/emails")]
        public QueryResponse Post([FromBody]EmailJob model)
        {
            Logger.InfoFormat("Entered EmailsController.Post(). Job Name = {0}", model.JobName);

            var response = new QueryResponse { Valid = true };

            try
            {
                _schedulerCore.CreateJob(model.JobName, model.JobGroup, typeof(SendMailJob));
            }
            catch (Exception ex)
            {
                response.Valid = false;
                response.Errors = new List<Error>
                {
                    new Error
                    {
                        Code = "ErrorCreatingJob",
                        Type = "Server",
                        Message = string.Format("Error: {0}", ex.Message)
                    }
                };
            }

            return response;
        }

        [Route("api/emails/{jobName}/{jobGroup?}")]
        public EmailJobDetails Get([FromBody]string jobName, [FromBody]string jobGroup = null)
        {
            Logger.InfoFormat("Entered EmailsController.Get(). jobName = {0}, jobName = {1}", jobName, jobGroup);

            IEnumerable<ITrigger> quartzTriggers = _schedulerCore.GetTriggersOfJob(jobName, jobGroup);

            IList<TriggerDetails> triggerDetails = new List<TriggerDetails>();

            foreach (ITrigger quartzTrigger in quartzTriggers)
            {
                var triggerType = string.Empty;
                if (quartzTrigger is ICronTrigger)
                {
                    triggerType = "Cron";
                }
                if (quartzTrigger is ISimpleTrigger)
                {
                    triggerType = "Simple";
                }
                var nextFireTimeUtc = quartzTrigger.GetNextFireTimeUtc();
                var previousFireTimeUtc = quartzTrigger.GetPreviousFireTimeUtc();
                triggerDetails.Add(new TriggerDetails
                {
                    Name = quartzTrigger.Key.Name,
                    Group = quartzTrigger.Key.Group,
                    JobName = quartzTrigger.JobKey.Name,
                    JobGroup = quartzTrigger.JobKey.Group,
                    Description = quartzTrigger.Description,
                    StartTimeUtc = quartzTrigger.StartTimeUtc.UtcDateTime,
                    EndTimeUtc =
                        (quartzTrigger.EndTimeUtc.HasValue) ? quartzTrigger.EndTimeUtc.Value.UtcDateTime : (DateTime?)null,
                    NextFireTimeUtc = (nextFireTimeUtc.HasValue) ? nextFireTimeUtc.Value.UtcDateTime : (DateTime?)null,
                    PreviousFireTimeUtc =
                        (previousFireTimeUtc.HasValue) ? previousFireTimeUtc.Value.UtcDateTime : (DateTime?)null,
                    FinalFireTimeUtc =
                        (quartzTrigger.FinalFireTimeUtc.HasValue)
                            ? quartzTrigger.FinalFireTimeUtc.Value.UtcDateTime
                            : (DateTime?)null,
                    Type = triggerType
                });
            }

            var retval = new EmailJobDetails
            {
                Name = jobName,
                TriggerDetails = new List<TriggerDetails>()
            };

            retval.TriggerDetails = triggerDetails;

            return retval;
        }

        [AcceptVerbs("DELETE")]
        [Route("api/emails/{jobName}/{jobGroup?}")]
        public QueryResponse Delete(string jobName, string jobGroup = null)
        {
            Logger.InfoFormat("Entered EmailsController.Delete(). jobName = {0}, jobName = {1}", jobName, jobGroup);

            var response = new QueryResponse { Valid = true };

            try
            {
                _schedulerCore.RemoveJob(jobName, jobGroup);
            }
            catch (Exception ex)
            {
                response.Valid = false;
                response.Errors = new List<Error>
                {
                    new Error
                    {
                        Code = "ErrorDeletingJob",
                        Type = "Server",
                        Message = string.Format("Error: {0}", ex.Message)
                    }
                };
            }

            return response;
        }

        [AcceptVerbs("POST")]
        [Route("api/emails/simpleTriggers")]
        public QueryResponse Post([FromBody]CustomJobSimpleTrigger model)
        {
            Logger.InfoFormat("Entered EmailsController.Post(). Name = {0}", model.TriggerName);

            var response = new QueryResponse { Valid = true };

            try
            {
                _schedulerCore.ScheduleTrigger(new SimpleTrigger
                {
                    Name = model.TriggerName,
                    Group = model.TriggerGroup,
                    JobName = model.JobName,
                    JobGroup = model.JobGroup,
                    RepeatCount = model.RepeatCount,
                    RepeatInterval = model.RepeatInterval,
                    StartDateTime = model.StartDateTime,
                });
            }
            catch (Exception ex)
            {
                response.Valid = false;
                response.Errors = new List<Error>
                {
                    new Error
                    {
                        Code = "ErrorSchedulingTrigger",
                        Type = "Server",
                        Message = string.Format("Error scheduling trigger {0}", ex.Message)
                    }
                };
            }

            return response;
        }


        [AcceptVerbs("POST")]
        [Route("api/emails/cronTriggers")]
        public QueryResponse Post([FromBody]CustomJobCronTrigger model)
        {
            Logger.InfoFormat("Entered EmailsController.Post(). Name = {0}", model.TriggerName);

            var response = new QueryResponse { Valid = true };

           try
            {
                _schedulerCore.ScheduleTrigger(new CronTrigger
                {
                    Name = model.TriggerName,
                    Group = model.TriggerGroup,
                    JobName = model.JobName,
                    JobGroup = model.JobGroup,
                    CronExpression = model.CronExpression,
                    StartDateTime = model.StartDateTime,
                });
            }
            catch (Exception ex)
            {
                string type = "Server";

                if (ex is FormatException)
                {
                    type = "Sender";
                }

                response.Valid = false;
                response.Errors = new List<Error>
                {
                    new Error
                    {
                        Code = "ErrorSchedulingTrigger",
                        Type = type,
                        Message = string.Format("Error scheduling CronTrigger {0}", ex.Message)
                    }
                };
            }

            return response;
        }

        private static Dictionary<string, object> GetDataMap(ICustomJob registeredJob)
        {
            var emailJob = JsonConvert.DeserializeObject<EmailJob>(registeredJob.Params);

            var dataMap = new Dictionary<string, object>();
            dataMap.Add("smtp_host", emailJob.SmtpHost);
            dataMap.Add("smtp_port", emailJob.SmtpPort);
            dataMap.Add("smtp_username", emailJob.Username);
            dataMap.Add("smtp_password", emailJob.Password);
            dataMap.Add("recipient", emailJob.Recipient);
            dataMap.Add("cc_recipient", emailJob.CcRecipient);
            dataMap.Add("sender", emailJob.Sender);
            dataMap.Add("reply_to", emailJob.ReplyTo);
            dataMap.Add("subject", emailJob.Subject);
            dataMap.Add("message", emailJob.Body);
            dataMap.Add("encoding", emailJob.Encoding);
            return dataMap;
        }

        private static EmailJob GetEmailJobFromDataMap(IJobDetail detail)
        {
            var emailJob = new EmailJob();
            emailJob.Body = detail.JobDataMap.GetString("message");
            emailJob.SmtpHost = detail.JobDataMap.GetString("smtp_host");
            emailJob.SmtpPort = detail.JobDataMap.GetString("smtp_port");
            emailJob.Username = detail.JobDataMap.GetString("smtp_username");
            emailJob.Password = detail.JobDataMap.GetString("smtp_password");
            emailJob.Recipient = detail.JobDataMap.GetString("recipient");
            emailJob.CcRecipient = detail.JobDataMap.GetString("cc_recipient");
            emailJob.Sender = detail.JobDataMap.GetString("sender");
            emailJob.ReplyTo = detail.JobDataMap.GetString("reply_to");
            emailJob.Subject = detail.JobDataMap.GetString("subject");
            emailJob.Body = detail.JobDataMap.GetString("message");
            emailJob.Encoding = detail.JobDataMap.GetString("encoding");
            return emailJob;
        }
    }
}
