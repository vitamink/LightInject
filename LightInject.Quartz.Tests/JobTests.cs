using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quartz;

namespace LightInject.Quartz.Tests
{
    [TestClass]
    public class JobTests
    {
        private IServiceContainer serviceContainer;
        private static bool _hasRun;
        private static string _message;

        [TestInitialize]
        public void Setup()
        {
            serviceContainer = new ServiceContainer();
            serviceContainer.EnableQuartz();
            serviceContainer.RegisterJobs();
            serviceContainer.Register<IFoo, Foo>();
        }

        [TestMethod]
        public void TestKernelCanInstantiateScheduler()
        {
            var scheduler = serviceContainer.TryGetInstance<IScheduler>();
            Assert.IsNotNull(scheduler);
        }

        [TestMethod]
        public void TestHarnessCanCreateJob()
        {
            var scheduler = serviceContainer.TryGetInstance<IScheduler>();

            IJobDetail testJob = JobBuilder
                                    .Create<TestJob>()
                                    .Build();

            ITrigger runOnce = TriggerBuilder.Create().WithSimpleSchedule(builder => builder.WithRepeatCount(0)).Build();
            
            scheduler.ScheduleJob(testJob, runOnce);

            scheduler.Start();

            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5.0));

            Assert.IsTrue(_hasRun);
            Assert.AreEqual("Foo for you!", _message);
        }

        public class TestJob : IJob
        {
            private readonly IFoo _foo;

            public TestJob(IFoo foo)
            {
                _foo = foo;
            }

            public void Execute(IJobExecutionContext context)
            {
                _hasRun = true;
                _message = _foo.GetMessage();
            }
        }

        public class Foo : IFoo
        {
            public string GetMessage()
            {
                return "Foo for you!";
            }
        }

        public interface IFoo
        {
            string GetMessage();
        }
    }
}
