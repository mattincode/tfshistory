using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TfsHistory
{

    // Input params: repo, branch, startdate
    // Start with filename, changeset, date

    // Print filename, backlog item, date of change

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel  _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new ViewModel();
            DataContext = _vm;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            TfsChangedFiles();
        }

        private void TfsChangedTasks()
        {
            TfsHelper TFS = new TfsHelper(StatusText);
            var startDate = DateTime.Parse(DateText.Text);
            //List<ChangeSetInfo> info = TFSChangelogGenerator.GetChangeinfo(teamProjectCollection, versionControlServer, buildServer, "$/Securitas GSP/Releases/Release1.28", "Ref", null, "T");
            var changes = TFS.GetChangedFiles(startDate, @"C:\Source Control\TFS\Securitas GSP\Main", "1",
                "http://tfs.stratiteq.se/tfs/Stratiteq",
                "true", "false");
            _vm.ChangedFiles = new ObservableCollection<TfsHistoryItem>(changes);
            StatusText.Text = "Done!";
        }

        private void TfsChangedFiles()
        {
            StatusText.Text = "Contacting TFS";            
            // Arguments 
            // args[0]</code> // local repository path
            // args[1]</code> // Change sheet #(you may go with change sheet number between 1-24000+ of a date) 
            // args[2]</code> // your remote TFS collections URL
            // args[3]</code> // true or false - get list of concatenated "File Name+Date+change Type"
            // args[4]</code> // true or false - absolute path of the file? true, else only file name

            TfsHelper TFS = new TfsHelper(StatusText);
            var startDate = DateTime.Parse(DateText.Text);
            //List<ChangeSetInfo> info = TFSChangelogGenerator.GetChangeinfo(teamProjectCollection, versionControlServer, buildServer, "$/Securitas GSP/Releases/Release1.28", "Ref", null, "T");
            var changes = TFS.GetChangedFiles(startDate, @"C:\Source Control\TFS\Securitas GSP\Main", "1",
                "http://tfs.stratiteq.se/tfs/Stratiteq",
                "true", "false");                       
            _vm.ChangedFiles = new ObservableCollection<TfsHistoryItem>(changes);
            StatusText.Text = "Done!";
        }

        private void Export_OnClick(object sender, RoutedEventArgs e)
        {
            var fileName = @"C:\Temp\TFS\TfsHistory.txt";
            StatusText.Text = "Starting export";            
            using (StreamWriter file = new StreamWriter(fileName, false))
            {
                file.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6}", "CheckInTime", "Filename", "ChangesetId", "Task", "WorkItem", "WorkItemType", "WorkItemTitle"));
                foreach (var changedFile in _vm.ChangedFiles)
                {
                    file.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6}", changedFile.CheckInTime.ToShortDateString(), changedFile.FileName, changedFile.ChangeSetId, changedFile.GetTaskText, changedFile.GetWorkItemInfo, changedFile.WorkItemType, changedFile.WorkItemTitle) );
                }                
            }
            StatusText.Text = string.Format("Export written to file: {0}", fileName);
        }

        private void SearchNoFiles_OnClick(object sender, RoutedEventArgs e)
        {
            TfsChangedTasks();
        }
    }

    public class TfsHistoryItem
    {
        public string FileName { get; set; }
        public DateTime CheckInTime { get; set; }
        public string CheckedInBy { get; set; }
        public int ChangeSetId { get; set; }
        public IEnumerable<AssociatedWorkItemInfo> WorkItems { get; set; }

        public WorkItem PrimaryWorkItem { get; set; }

        public string GetTaskText
        {
            get
            {
                var associatedWorkItemInfo = WorkItems.FirstOrDefault(x => x.WorkItemType == "Task");
                if (associatedWorkItemInfo != null)
                    return associatedWorkItemInfo.Title;
                else
                {
                    return "";
                }                 
            }
        }

        public string GetWorkItemInfo
        {
            get { return PrimaryWorkItem?.Id.ToString() ?? "No associated workitems"; }
            //get { return WorkItems != null ? String.Join(":", WorkItems.Select(x => x.Id).FirstOrDefault()) : "No associated workitems"; }
        }

        public string WorkItemType
        {
            get { return PrimaryWorkItem?.Type.Name ?? ""; }
            //get { return WorkItems != null ? WorkItems.Select(x => x.WorkItemType).FirstOrDefault() : ""; }
        }

        public string WorkItemTitle
        {
            get { return PrimaryWorkItem?.Title ?? ""; }
            //get { return WorkItems != null ? WorkItems.Select(x => x.Title).FirstOrDefault() : ""; }
        }
    }

    public class ViewModel : BaseViewModel
    {
        private ObservableCollection<TfsHistoryItem> _changedFiles;

        public ObservableCollection<TfsHistoryItem> ChangedFiles
        {
            get { return _changedFiles; }
            set { _changedFiles = value; RaisePropertyChanged(() => ChangedFiles); }
        }
    }

    public class TfsHelper
    {
        private TextBox _status;

        public TfsHelper(TextBox status)
        {
            _status = status;
        }

        private void Log(string message)
        {
            _status.Text = message;
        }

        public IList<TfsHistoryItem> GetChangedTasks(DateTime startDate, params string[] args)
        {
            List<TfsHistoryItem> changedFiles = new List<TfsHistoryItem>();
            string localPath = args[0];
            string versionFromString = args[1];
            TfsTeamProjectCollection tfs = null;
            if (args.Length > 2)
            {
                tfs = new TfsTeamProjectCollection(new Uri(args[2]));
            }
            else return null;
            VersionControlServer vcs = tfs.GetService<VersionControlServer>();
            String[] requestedWorkItemTypes = {"Product Backlog Item", "Bug"};
            Log("Login to TFS succesful");
            try
            {
                var workItemStore = tfs.GetService<Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItemStore>();
                //workItemStore.Query()


                var changeSetItems = vcs.QueryHistory(localPath,
                                        VersionSpec.ParseSingleSpec(
                                                          versionFromString,
                                                          null),
                                        0, RecursionType.Full, null,
                                        VersionSpec.ParseSingleSpec(
                                                          versionFromString,
                                                          null),
                                                          null, Int32.MaxValue, true, false);
                foreach (Changeset item in changeSetItems)
                {
                    if (item.Changes == null || !item.Changes.Any())
                    {
                        WorkItem workItem = null;
                        var primaryWorkItem = item.WorkItems?.FirstOrDefault(t => t.Type.Name != "Task");
                        if (primaryWorkItem != null)
                        {
                            workItem = primaryWorkItem;
                        }
                        if (item.WorkItems != null)
                        {
                            var task = item.WorkItems?.FirstOrDefault(t => t.Type.Name == "Task");
                            if (task != null)
                            {
                                workItem = GetTaskWorkItemParents(workItemStore, requestedWorkItemTypes, task).FirstOrDefault();
                            }
                        }

                        changedFiles.Add(new TfsHistoryItem() { FileName = "", CheckInTime = item.CreationDate, ChangeSetId = item.ChangesetId, WorkItems = item.AssociatedWorkItems?.ToList(), PrimaryWorkItem = workItem });
                    }                    
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return changedFiles;
        }
    

    public IList<TfsHistoryItem> GetChangedFiles(DateTime startDate, params string[] args)
        {            
            List<TfsHistoryItem> changedFiles = new List<TfsHistoryItem>();
            string localPath = args[0];
            string versionFromString = args[1];
            TfsTeamProjectCollection tfs = null;
            if (args.Length > 2)
            {
                tfs = new TfsTeamProjectCollection(new Uri(args[2]));
            }
            else return null;
            VersionControlServer vcs = tfs.GetService<VersionControlServer>();
            String[] requestedWorkItemTypes = { "Product Backlog Item", "Bug" };
            Log("Login to TFS succesful");
            try
            {
                var workItemStore = tfs.GetService<Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItemStore>();
                var changeSetItems = vcs.QueryHistory(localPath,
                                                      VersionSpec.ParseSingleSpec(
                                                                        versionFromString,
                                                                        null),
                                                      0, RecursionType.Full, null,
                                                      VersionSpec.ParseSingleSpec(
                                                                        versionFromString,
                                                                        null),
                                                                        null, Int32.MaxValue, true, false);
                Log("Query completed, starting to enumerate!");
                int itemCount = 0;
                foreach (Changeset item in changeSetItems)
                {
                    DateTime checkInDate = item.CreationDate;
                    if (checkInDate < startDate) continue;                    

                    string user = item.Committer;
                    foreach (Change changedItem in item.Changes)
                    {
                        string filename = changedItem.Item.ServerItem.Substring
                        (changedItem.Item.ServerItem.LastIndexOf('/') + 1);
                        // Your choice of filters. In this case I was not interested in the below files.
                        if (!filename.EndsWith(".dll")
                            && !filename.EndsWith(".pdb")
                            && !filename.EndsWith(".csproj")
                             && !filename.EndsWith(".pubxml")
                              && !filename.EndsWith(".sln")
                               && !filename.EndsWith(".config")
                               && !filename.EndsWith(".log")
                               && !filename.EndsWith(".resx")
                            && filename.IndexOf(".") > -1
                            && changedItem.ChangeType.Equals(ChangeType.Edit))
                        {
                             
                            WorkItem workItem = null;                            
                            var primaryWorkItem = item.WorkItems?.FirstOrDefault(t => t.Type.Name != "Task");
                            if (primaryWorkItem != null)
                            {
                                workItem = primaryWorkItem;
                            }
                            if (item.WorkItems != null)
                            {
                                var task = item.WorkItems?.FirstOrDefault(t => t.Type.Name == "Task");
                                if (task != null)
                                {
                                    workItem = GetTaskWorkItemParents(workItemStore, requestedWorkItemTypes, task).FirstOrDefault();
                                }
                            }
                            
                            changedFiles.Add(new TfsHistoryItem() {FileName = filename, CheckInTime = changedItem.Item.CheckinDate, ChangeSetId = changedItem.Item.ChangesetId, WorkItems = item.AssociatedWorkItems?.ToList(), PrimaryWorkItem = workItem});
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            if (changedFiles != null && changedFiles.Count > 0)
                Console.WriteLine
                ("-----------------------------------------\nTotal File count: " +
                changedFiles.Count);
            Console.WriteLine
            ("-----------------------------------------\nPress any key to close");
            //Console.ReadKey();
            return changedFiles;
        }


        private static List<WorkItem> GetTaskWorkItemParents(Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItemStore workItemStore, String[] requestedWorkItemTypes, Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            List<WorkItem> retval = new List<WorkItem>();

            // Find related Bug or Product Backlog Item
            foreach (var link in workItem.Links)
            {
                Microsoft.TeamFoundation.WorkItemTracking.Client.RelatedLink relLink = link as Microsoft.TeamFoundation.WorkItemTracking.Client.RelatedLink;
                if (relLink != null && relLink.LinkTypeEnd != null && relLink.LinkTypeEnd.Name == "Parent")
                {
                    Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem wi = workItemStore.GetWorkItem(relLink.RelatedWorkItemId);
                    if (wi != null && requestedWorkItemTypes.Contains(wi.Type.Name))
                    {
                        retval.Add(wi);
                    }
                }
            }

            return retval;
        }

    }
}
