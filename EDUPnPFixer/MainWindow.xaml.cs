using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.IO;
using System.Text.RegularExpressions;

namespace EDUPnPFixer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Regex upnpLineMatcher = new Regex(@"^\{(\d{2}:\d{2}:\d{2})\} failed to initialise upnp$");

        public MainWindow()
        {
            InitializeComponent();
        }

        public void StartUp()
        {
            Dictionary<string, string> EDFolders = new Dictionary<string, string>();

            //List of the Elite SKUs
            EDFolders.Add("FORC-FDEV-D-1010", "Elite Dangerous");
            EDFolders.Add("FORC-FDEV-D-1008", "Elite Dangerous - Gamma");
            EDFolders.Add("FORC-FDEV-D-1003", "Elite Dangerous - Mercenary Edition");
            EDFolders.Add("FORC-FDEV-D-1002", "Elite Dangerous - Beta");
            EDFolders.Add("FORC-FDEV-D-1001", "Elite Dangerous - Premium Beta");
            EDFolders.Add("FORC-FDEV-D-1000", "Elite Dangerous - Alpha");

            EDConfig.checkInstallLocation();
            this.txtStatus.Text = "Looking in: " + Properties.Settings.Default.ED_INSTALL_DIR;

            foreach (KeyValuePair<string, string> entry in EDFolders)
            {
                if (Directory.Exists(Properties.Settings.Default.ED_INSTALL_DIR + @"Products\" + entry.Key))
                {
                    this.txtStatus.Text += "\n" + entry.Value + ": FOUND";
                    //We found it, so look for the log file and check it's contents
                    string LogFolder = Properties.Settings.Default.ED_INSTALL_DIR + @"Products\" + entry.Key + @"\Logs\";
                    var directory = new DirectoryInfo(LogFolder);
                    foreach(FileInfo file in directory.GetFiles("netLog.*").OrderByDescending(f => f.LastWriteTime))
                    {
                        //Look in each one for the UPnP lines
                        Console.WriteLine(file.Name);
                        using (StreamReader s = new StreamReader(file.FullName))
                        {
                            while (s.Peek() >= 0)
                            {
                                Match lineMatch = this.upnpLineMatcher.Match(s.ReadLine());
                                if (lineMatch.Success)
                                {
                                    //We have a system line, form an event from it
                                    //netLog.1509171920.01.log
                                    string[] fileNameParts = file.FullName.Split('.');
                                    DateTime parsedDate;
                                    DateTime parsedTime;
                                    DateTime.TryParseExact(fileNameParts[1], "yyMMddHHmm", null, System.Globalization.DateTimeStyles.None, out parsedDate);
                                    DateTime.TryParseExact(lineMatch.Groups[1].Value, "HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out parsedTime);
                                    DateTime eventDate = parsedDate.Date + parsedTime.TimeOfDay;

                                    string[] positionParts = lineMatch.Groups[4].Value.Split(',');

                                    this.txtStatus.Text += "\n" + FOUND UPNP ERRORS";

                                }
                            }
                        }

                    }
                }
                else
                {
                    this.txtStatus.Text += "\n" + entry.Value + ": not found";
                }
            }


            //Check each folder if it exists
            //Check in log files for UPnP errors
            //If errors found ass AppConfigLocal.xml
            //Check if exists, add or append network settings
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.StartUp();
        }

    }
}
