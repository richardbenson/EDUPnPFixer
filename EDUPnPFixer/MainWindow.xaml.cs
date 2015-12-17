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
using System.Xml.Linq;

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
            MessageBoxResult carryOn = MessageBox.Show("This program will scan your ED versions for UPnP errors and apply a file to convert to fixed port mappings.\n\nIf the file is applied, you will have to manually set up port forwarding on your router.\n\nSee www.portforward.com for help on how to set up manual port mappings.\n\nContinue?", "ED UPnP Fixer", MessageBoxButton.OKCancel);
            if (carryOn == MessageBoxResult.Cancel)
            {
                Application.Current.Shutdown();
                return;
            }

            Dictionary<string, string> EDFolders = new Dictionary<string, string>();

            //List of the Elite SKUs
            EDFolders.Add("FORC-FDEV-D-1010", "Elite Dangerous");
            EDFolders.Add("FORC-FDEV-D-1008", "Elite Dangerous - Gamma");
            EDFolders.Add("FORC-FDEV-D-1003", "Elite Dangerous - Mercenary Edition");
            EDFolders.Add("FORC-FDEV-D-1002", "Elite Dangerous - Beta");
            EDFolders.Add("FORC-FDEV-D-1001", "Elite Dangerous - Premium Beta");
            EDFolders.Add("FORC-FDEV-D-1000", "Elite Dangerous - Alpha");
            EDFolders.Add("elite-dangerous-64", "Elite Dangerous - Horizons"); //Possibly? Or is it E:D 64-bit? Folder is the same for both... great work!

            EDConfig.checkInstallLocation();
            this.txtStatus.Text = "Looking in: " + Properties.Settings.Default.ED_INSTALL_DIR;

            foreach (KeyValuePair<string, string> entry in EDFolders)
            {
                if (Directory.Exists(Properties.Settings.Default.ED_INSTALL_DIR + @"Products\" + entry.Key))
                {
                    bool fixedVersion = false;
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
                            while (s.Peek() >= 0 && !fixedVersion)
                            {
                                Match lineMatch = this.upnpLineMatcher.Match(s.ReadLine());
                                if (lineMatch.Success)
                                {
                                    //If we have a match then we don't need to keep checking this product
                                    fixedVersion = true;
                                    
                                    //We have an error line
                                    //netLog.1509171920.01.log
                                    string[] fileNameParts = file.FullName.Split('.');
                                    DateTime parsedDate;
                                    DateTime parsedTime;
                                    DateTime.TryParseExact(fileNameParts[1], "yyMMddHHmm", null, System.Globalization.DateTimeStyles.None, out parsedDate);
                                    DateTime.TryParseExact(lineMatch.Groups[1].Value, "HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out parsedTime);
                                    DateTime eventDate = parsedDate.Date + parsedTime.TimeOfDay;

                                    this.txtStatus.Text += "\n" + eventDate.ToString() + " FOUND UPNP ERRORS ";

                                    MessageBoxResult result = MessageBox.Show("UPnP Errors found in " + entry.Value + " on " + eventDate.ToShortDateString() + " at " + eventDate.ToShortTimeString() + ".  Would you like to fix this version?", "Errors Found", MessageBoxButton.YesNo);

                                    if (result == MessageBoxResult.Yes)
                                    {
                                        //find the appconfiglocal.xml if it exists
                                        string localConfig = Properties.Settings.Default.ED_INSTALL_DIR + @"Products\" + entry.Key + @"\AppConfigLocal.xml";
                                        //open it
                                        XDocument ConfigFile;
                                        bool bolNewFile = false;

                                        //Check if there is an existing sync file, if so open it.
                                        if (File.Exists(localConfig))
                                        {
                                            ConfigFile = XDocument.Load(localConfig);
                                        }
                                        else
                                        {
                                            //No existing file, so let's create a blank one to work on.
                                            ConfigFile = new XDocument(
                                                new XElement("AppConfig")
                                            );
                                            bolNewFile = true;
                                        }

                                        //amend or add port mappings
                                        /*
                                        <AppConfig>
                                            <Network
                                            Port="5100"
                                            upnpenabled="0"
                                            LogFile="netLog"
                                            DatestampLog="1">
                                            </Network>
                                        </AppConfig>
                                        */
                                        if (ConfigFile.Root.Element("Network") == null)
                                        {
                                            
                                            ConfigFile.Root.Add(new XElement("Network"));
                                        }
                                        XElement NetworkElement = ConfigFile.Root.Element("Network");
                                        NetworkElement.SetAttributeValue("Port", "5100");
                                        NetworkElement.SetAttributeValue("upnpenabled", "0");
                                        if (NetworkElement.Attribute("LogFile") == null) NetworkElement.SetAttributeValue("LogFile", "netLog");
                                        if (NetworkElement.Attribute("DatestampLog") == null) NetworkElement.SetAttributeValue("DatestampLog", "1");

                                        ConfigFile.Save(localConfig);

                                        txtStatus.Text += "\nAppConfigLocal.xml created";

                                        MessageBox.Show("Local config file created. Set up port forwarding to this machine for UDP on port 5100");
                                    }



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
