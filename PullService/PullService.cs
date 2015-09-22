using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Linq.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PullService
{
    public partial class PullService : ServiceBase
    {
        // How long we'll wait before checking the database and dispatching pull tasks
        private static readonly TimeSpan PullCheckPeriod = new TimeSpan(0, 1, 0);
        private Timer PullCheck;
        private object PullCheckSync = new object();

        private GrooveyFMDataContext db;
        private Dictionary<string, Type> AvailableSourceTypes;

        public PullService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            MyOnStart(args);
        }
            
        public void MyOnStart(string[] args)
        {
            PullCheck = new Timer(PullCheckCallback, null, TimeSpan.Zero, PullCheckPeriod);
            db = new GrooveyFMDataContext("Server = astra-server; User Id = grooveyfm; Password = BWQCmdhY8tLvb7xJEJ");

            var types =
                from appdomain in AppDomain.CurrentDomain.GetAssemblies()
                from type in appdomain.GetTypes()
                where
                !type.IsAbstract && !type.IsNested && type.IsPublic &&
                type.IsSubclassOf(typeof(BaseSource))
                select type;

            AvailableSourceTypes = types.ToDictionary(t => t.Name);
        }

        private void PullCheckCallback(object state)
        {
            if (Monitor.TryEnter(PullCheckSync, 500))
            {
                var sources = 
                    from source in db.Sources
                    where
                        source.Enabled &&
                        source.Station1.Enabled /* &&
                        // Check MinPullDelay
                        (!source.LastPull.HasValue || !source.MinPullDelay.HasValue ||
                            SqlMethods.DateDiffMillisecond(DateTime.Now, source.LastPull.Value) >= source.MinPullDelay.Value.TotalMilliseconds) &&
                        // Check DailyPullTime
                        (!source.LastPull.HasValue || !source.DailyPullTime.HasValue ||
                            (source.LastPull.Value.Date != DateTime.Today && DateTime.Now.CompareTo(source.DailyPullTime.Value) >= 0)) */
                    select source;

                List<Task> tasks = new List<Task>();
                try
                {
                    foreach (var source in sources)
                    {
                        BaseSource checker = (BaseSource)Activator.CreateInstance(AvailableSourceTypes[source.SourceType.ClassName]);
                    }
                    Task.WaitAll(tasks.ToArray());
                }
                catch (Exception ex)
                {
                    throw;
                }

                Monitor.Exit(PullCheckSync);
            }
        }

        protected override void OnStop()
        {
            PullCheck.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); // Stop the time
            Monitor.Enter(PullCheckSync); // Wait for the current check (if any) to complete
            Monitor.Exit(PullCheckSync); // Clean up

            PullCheck.Dispose();
            PullCheck = null;

            db.Dispose();
            db = null;
        }
    }
}
