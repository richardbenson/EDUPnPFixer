using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using System.Xml.Linq;
using VDF;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;

namespace EDUPnPFixer
{
    static class EDConfig
    {
        const string ED_PRODUCT_DIR = @"Products\FORC-FDEV-D-1003\";
        const string ED_PRODUCT_DIR_ALT = @"Products\FORC-FDEV-D-1010\";

        const string ED_CONFIG_FILE = "AppConfig.xml";
        const string ED_LOGS_DIR = @"Logs\";
        const string ED_SCREENSHOTS_FOLDER = @"\Frontier Developments\Elite Dangerous\";

        const string ED_INSTALL_SUBKEY = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{696F8871-C91D-4CB1-825D-36BE18065575}_is1";
        const string ED_INSTALL_KEY = "InstallLocation";

        const string STEAM_INSTALL_SUBKEY = @"SOFTWARE\Wow6432Node\Valve\Steam";
        const string STEAM_INSTALL_KEY = "InstallPath";


        public static void checkInstallLocation()
        {
            if (Properties.Settings.Default.ED_INSTALL_DIR == "")
            {
                EDConfig.findInstallLocation();
            }
        }

        public static void findInstallLocation()
        {
            string loc = "";
            RegistryKey edInstall = Registry.LocalMachine.OpenSubKey(ED_INSTALL_SUBKEY);
            if (edInstall != null)
            {
                //This is a non-Steam version, easy to get install location
                loc = edInstall.GetValue(ED_INSTALL_KEY).ToString();
            }
            else
            {
                //Urgh, a Steam install, this will take a few steps.
                //First find Steam install location
                string steamDir = Registry.LocalMachine.OpenSubKey(STEAM_INSTALL_SUBKEY).GetValue(STEAM_INSTALL_KEY).ToString();
                //Grab list of library folders
                VDFFile conf = new VDFFile(steamDir + @"\SteamApps\libraryfolders.vdf");
                List<string> libraryFolders = new List<string>();
                foreach (string name in conf.Elements["LibraryFolders"].Children.Keys)
                {
                    //library locations are numbers
                    int number;
                    if (int.TryParse(name, out number))
                    {
                        libraryFolders.Add(conf.Elements["LibraryFolders"].Children[name].Value);
                    }
                }
                //look for Elite subfolder in each library
                foreach (string folder in libraryFolders)
                {
                    //Console.WriteLine(folder);
                    if (Directory.Exists(folder.Replace(@"\\", @"\") + @"\SteamApps\common\Elite Dangerous"))
                    {
                        loc = folder.Replace(@"\\", @"\") + @"\SteamApp\common\Elite Dangerous";
                        break;
                    }
                }

            }



            if (loc == "")
            {
                MessageBoxResult messageBoxResult = MessageBox.Show("Cannot find your ED launcher location, please choose now");
                bool selectedCorrectly = false;

                while (!selectedCorrectly)
                {
                    //Not found in registry or in Steam Library? give up and ask the user
                    var dlg = new CommonOpenFileDialog();
                    dlg.Title = "Choose your Elite launcher install location";
                    dlg.IsFolderPicker = true;

                    dlg.AddToMostRecentlyUsedList = false;
                    dlg.AllowNonFileSystemItems = false;
                    dlg.EnsureFileExists = true;
                    dlg.EnsurePathExists = true;
                    dlg.EnsureReadOnly = false;
                    dlg.EnsureValidNames = true;
                    dlg.Multiselect = false;
                    dlg.ShowPlacesList = true;

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        loc = dlg.FileName + @"\";
                        if (Directory.Exists(loc + "EDLaunch.exe")) selectedCorrectly = true;
                    }
                }
            }

            Properties.Settings.Default.ED_INSTALL_DIR = loc;
            Properties.Settings.Default.Save();
        }

        public static string getConfigFilePath(string SKU)
        {
            EDConfig.checkInstallLocation();
            return Path.Combine(Properties.Settings.Default.ED_INSTALL_DIR, SKU, ED_CONFIG_FILE);
        }

        public static string getLogsFolderPath(string SKU)
        {
            EDConfig.checkInstallLocation();
            return Path.Combine(Properties.Settings.Default.ED_INSTALL_DIR, SKU, ED_LOGS_DIR);
        }

        /*public static void enableVerboseLogging()
        {
            XDocument Config = XDocument.Load(EDConfig.getConfigFilePath());

            XElement networkConfig = Config.Element("AppConfig").Element("Network");
            networkConfig.SetAttributeValue("VerboseLogging", 1);
            Config.Save(EDConfig.getConfigFilePath());
        }*/

    }
}
