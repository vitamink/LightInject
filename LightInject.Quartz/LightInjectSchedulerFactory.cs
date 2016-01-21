using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using LightInject;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace LightInject.Quartz
{
    public static class QuartzJobExtensions
    {
        /// <summary>
        /// Enables dependency injection in a Quartz.NET environment.
        /// </summary>
        /// <param name="serviceContainer">The target <see cref="IServiceContainer"/>.</param>
        public static void EnableQuartz(this IServiceContainer serviceContainer)
        {
            serviceContainer.Register<LightInjectJobFactory>(factory => new LightInjectJobFactory(serviceContainer));
            serviceContainer.Register<ISchedulerFactory, LightInjectSchedulerFactory>();
            serviceContainer.Register<IScheduler>(factory => factory.TryGetInstance<ISchedulerFactory>().GetScheduler(), new PerContainerLifetime());
        }


        public static void RegisterJobs(this IServiceRegistry serviceRegistry, params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var jobTypes = assembly.GetTypes().Where(t => !t.IsAbstract && typeof(IJob).IsAssignableFrom(t));
                foreach (var controllerType in jobTypes)
                {
                    serviceRegistry.Register(controllerType, new PerRequestLifeTime());
                }
            }
        }

        public static void RegisterJobs(this IServiceRegistry serviceRegistry)
        {
            RegisterJobs(serviceRegistry, Assembly.GetCallingAssembly());            
        }
    }
    public class LightInjectSchedulerFactory : StdSchedulerFactory
	{
		private readonly LightInjectJobFactory jobFactory;

		public LightInjectSchedulerFactory(LightInjectJobFactory jobFactory)
		{
			this.jobFactory = jobFactory;
		}

		protected override IScheduler Instantiate(global::Quartz.Core.QuartzSchedulerResources rsrcs, global::Quartz.Core.QuartzScheduler qs)
		{
			qs.JobFactory = this.jobFactory;
			return base.Instantiate(rsrcs, qs);
		}
	}

    public class LightInjectJobFactory : IJobFactory
    {
        private readonly IServiceContainer serviceContainer;

		private static readonly ILog log = LogManager.GetLogger(typeof(LightInjectJobFactory));

		public LightInjectJobFactory(IServiceContainer serviceContainer)
		{
			this.serviceContainer = serviceContainer;
		}
		
		/// <summary>
		/// Called by the scheduler at the time of the trigger firing, in order to
		/// produce a <see cref="IJob" /> instance on which to call Execute.
		/// Instance creation is delegated to the Ninject Kernel.
		/// </summary>
		/// <remarks>
		/// It should be extremely rare for this method to throw an exception -
		/// basically only the the case where there is no way at all to instantiate
		/// and prepare the Job for execution.  When the exception is thrown, the
		/// Scheduler will move all triggers associated with the Job into the
		/// <see cref="TriggerState.Error" /> state, which will require human
		/// intervention (e.g. an application restart after fixing whatever
		/// configuration problem led to the issue wih instantiating the Job.
		/// </remarks>
		/// <param name="bundle">The TriggerFiredBundle from which the <see cref="IJobDetail" />
		///   and other info relating to the trigger firing can be obtained.</param>
		/// <param name="scheduler"></param>
		/// <returns>the newly instantiated Job</returns>
		/// <throws>  SchedulerException if there is a problem instantiating the Job. </throws>
		public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
		{
			IJobDetail jobDetail = bundle.JobDetail;
			Type jobType = jobDetail.JobType;
			try
			{
				if (log.IsDebugEnabled)
				{
					log.Debug(string.Format(CultureInfo.InvariantCulture, "Producing instance of Job '{0}', class={1}", jobDetail.Key, jobType.FullName));
				}

				return this.serviceContainer.TryGetInstance(jobType) as IJob;
			}
			catch (Exception e)
			{
				SchedulerException se = new SchedulerException(string.Format(CultureInfo.InvariantCulture, "Problem instantiating class '{0}'", jobDetail.JobType.FullName), e);
				throw se;
			}
		}

		/// <summary>
		/// Allows the the job factory to destroy/cleanup the job if needed. 
		/// No-op when using SimpleJobFactory.
		/// </summary>
		public void ReturnJob(IJob job)
		{
			
		}
    }
}
