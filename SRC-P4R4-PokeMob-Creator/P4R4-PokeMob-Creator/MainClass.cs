﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace P4R4_PokeMob_Creator
{
    /// <summary>
    /// Main class of this project
    /// </summary>
    public class MainClass
    {
        /// <summary>
        /// Property to store the MainForm here
        /// </summary>
        public MainForm MainForm { get; set; }

        //CONSTS
        private const string BOT_FOLDER_NAME = "\\PokeMobBot";
        public const string CONFIG_FOLDER_NAME = "\\config\\config.json";
        public const string BOT_EXE_NAME = "PokeMobBot.exe";
        public const string AUTH_FOLDER_NAME = "\\config\\auth.json";

        //Regex to check if user entered right acc:pw
        private Regex _ptcAccReg;
        private Regex _googleAccReg;
        //Regexes for the proxies
        private Regex _proxyReg;
        private Regex _proxyUserPwReg;

        //Folder where bot is
        public string BotFolder { private get; set; }

        //Int to save the accs needed with a default value of -> 1
        public int NeededAccounts { private get; set; } = 1;

        //Folder to place each bot folder
        public string DirToPlaceFolders { private get; set; }

        //Filepath to the config.json
        public string configFilePath { private get; set; } = "";

        //Initialize a new list to save each account
        private List<string> _verifiedAccounts;

        //Initialize a new list to save each proxy
        private List<string> _verifiedProxies;

        //Initialize a new list to save each account
        private List<string> _nameFolders;

        //Create a boolean to check if it's a custom config
        public bool CustomConfig {get; set;} = false;
        //Create Array of booleans for further verifications
        public bool[] PathBooleans { get; set; } = new bool[2];

        /// <summary>
        /// Default constructor of the class
        /// </summary>
        public MainClass()
        {
            //Set the regexes
            _ptcAccReg = new Regex(@"^[a-zA-Z0-9_çéàüèöä+]{6,16}:+(.*){6,15}$");
            _googleAccReg = new Regex(@"(\W|^)[\w.+\-]*@gmail\.com:+(.*){8,37}$");
            _proxyReg = new Regex(@"^((([^:]+):([^@]+))@)?((\d{1,3}\.){3}\d{1,3})(:(\d{1,5}))$");
            _proxyUserPwReg = new Regex(@"^((([^:]+):([^@]+))@)?((\d{1,3}\.){3}\d{1,3})(:(\d{1,5})):+[a-zA-Z0-9_-]{5,30}:+(.*){5,30}$");

            //Set the new list for each lists
            _verifiedAccounts = new List<string>();
            _verifiedProxies = new List<string>();
            _nameFolders = new List<string>();
        }

        /// <summary>
        /// Method to start the creation when the button is clicked
        /// </summary>
        public void startCreation()
        {
            //Instanciate the logs class to append logs to the richTxtBox
            CreationLogs logClass = new CreationLogs(MainForm);

            //Clear the logs
            logClass.ClearLogs();

            //Add verifying paths to log
            logClass.AppendLog("Verifying paths...");

            //Call the method to check if all the path are filled
            if (!verifyPaths())
            {
                //Error for path/s missing
                MessageBox.Show("Path/s missing !");

                //Clear the logs
                logClass.ClearLogs();

                //Exits the function
                return;
            }

            //Add verifications to richtextboxes to log
            logClass.AppendLog("Verifying empties accounts/proxies lists...");

            //Check if the richtextboxes aren't empty
            if (MainForm.accsRichTxtBox.Text == "" || MainForm.proxiesRichTxtBox.Text == "")
            {
                //Error for empty accounts/proxies list
                MessageBox.Show("Empty accounts/proxies list !");

                //Clear the logs
                logClass.ClearLogs();

                //Exits the function
                return;
            }

            //Add verifications to manually deleted accs/proxies to log
            logClass.AppendLog("Checking for new manually added/deleted accounts/proxies...");

            //Check if the user deleted or added MANUALLY new accounts or proxies
            verifyNewLoadedProxAcc(MainForm.accsRichTxtBox.Lines, false, true);
            verifyNewLoadedProxAcc(MainForm.proxiesRichTxtBox.Lines, false, false);

            //Add check for sufficient accs and proxies to logs
            logClass.AppendLog("Checking for sufficient accounts and proxies...");

            //Check if the verified accounts are sufficient to match the number of folders we need to create
            //Same thing for the proxies
            if ((_verifiedAccounts.Count() < NeededAccounts) || (_verifiedProxies.Count() < NeededAccounts))
            {
                //Error for insufficient accounts
                MessageBox.Show("Please, ensure you loaded/added sufficient accounts/proxies.");

                //Clear the logs
                logClass.ClearLogs();

                //Exits the function
                return;
            }

            //Add creation of folders to logs
            logClass.AppendLog("Creating folders...");

            //Call the method to create the number of needed folders
            createFolders(Convert.ToInt32(MainForm.nbFoldersNum.Value), logClass);

            //Add parsing combolists to logs
            logClass.AppendLog("Parsing account:password list...");

            //Parse the combolist(the list of verified accs:pw)
            string[,] accsPw = parseCombolist();

            //Add filtering and clearing to the logs
            logClass.AppendLog("Clearing and filtering proxies list...");

            //Filter and clear the proxieslist
            string[,] proxiesList = clearAndFilterProxiesList();

            //Add creation of auth and config files to the logs
            logClass.AppendLog("Creating auth.json and config.json files for each folder...");

            //Call the method to do the auth.json file for each bot folder with each of the accounts
            makeAuthAndRndCfg(accsPw, proxiesList);

            //Clear the array with the names of the created folders for each bot
            _nameFolders.Clear();

            //Add creation done to logs
            logClass.AppendLog("Done !");

            //Messagebox to alert the user of successfully creation
            MessageBox.Show("Successfully created: " + NeededAccounts + " folders !");
        }

        /// <summary>
        /// Method to create the folders for the bots
        /// </summary>
        /// <param name="numberOfFolders">Get the number of folders to create</param>
        public void createFolders(int numberOfFolders, CreationLogs logClass)
        {
            //Loop to create the folders required
            for (int i = 1; i <= numberOfFolders; i++)
            {
                //Nb that will be added to the folder name if the folder already exists without broking the counter(i)
                //So if bot1 already exists we will increment this nb until we find a name of folder that is free to use.
                int folderNb = i;

                //Check if folder exists
                while (Directory.Exists(DirToPlaceFolders + BOT_FOLDER_NAME + folderNb))
                {
                    //Increment the counter
                    folderNb++;
                }
                //Create the folder
                Directory.CreateDirectory(DirToPlaceFolders + BOT_FOLDER_NAME + folderNb);

                //Add an entry in the log for the created folder
                logClass.AppendLog("Created: " + DirToPlaceFolders + BOT_FOLDER_NAME + folderNb);

                //Add the name of the folder to the list
                _nameFolders.Add(BOT_FOLDER_NAME + folderNb);

                //Copy folder structure from bot folder
                foreach (string sourceSubFolder in Directory.GetDirectories(BotFolder, "*", SearchOption.AllDirectories))
                {
                    //Create the BotX directory
                    Directory.CreateDirectory(sourceSubFolder.Replace(BotFolder, DirToPlaceFolders + BOT_FOLDER_NAME + folderNb));
                }

                //Copy bot subfolder and files
                foreach (string sourceFile in Directory.GetFiles(BotFolder, "*", SearchOption.AllDirectories))
                {
                    string destinationFile = sourceFile.Replace(BotFolder, DirToPlaceFolders + BOT_FOLDER_NAME + folderNb);
                    File.Copy(sourceFile, destinationFile, true);
                }

                //Rename each exe by adding the counter number to the name
                DirectoryInfo d = new DirectoryInfo(DirToPlaceFolders + BOT_FOLDER_NAME + folderNb);
                FileInfo[] infos = d.GetFiles(BOT_EXE_NAME);
                foreach (FileInfo f in infos)
                {
                    // Do the renaming here
                    File.Move(f.FullName, f.Directory.FullName + BOT_FOLDER_NAME + folderNb + f.Extension);
                }

                //Check if the user has chosen a custom config
                if (CustomConfig)
                {
                    //Copy the config file to each folder
                    File.Copy(configFilePath, DirToPlaceFolders + BOT_FOLDER_NAME + folderNb + CONFIG_FOLDER_NAME);
                }
                else
                {
                    //Copy the config.json file in resources if it doesn't exists -> custom config ?
                    File.WriteAllBytes(DirToPlaceFolders + BOT_FOLDER_NAME + folderNb + CONFIG_FOLDER_NAME, Properties.Resources.config);
                }
            }
        }

        /// <summary>
        /// Method used to make the verifications we need before creating folders and files
        /// </summary>
        /// <returns>Return a boolean if all paths are set or not</returns>
        public bool verifyPaths()
        {
            //Loop through each boolean in the array of the path to check if we got all the needed paths
            foreach (bool path in PathBooleans)
            {
                //If one path is missing we display a messagebox and return false
                if (path == false || (MainForm.customConfigChkBox.Checked && configFilePath == string.Empty))
                {
                    //Return false
                    return false;
                }
            }

            //Check if it's a customconfig and if the filepath is blank
            if (CustomConfig && configFilePath == "")
            {
                return false;
            }

            //Return true
            return true;
        }

        /// <summary>
        /// Method to check if the new manually added/loaded proxies/accs are valid and add/display them in the list/richtextbox.
        /// </summary>
        /// <param name="newProxies">Get an array of string</param>
        /// <param name="autoLoaded">Boolean to check if added manually or not</param>
        public void verifyNewLoadedProxAcc(string[] newProxAcc, bool autoLoaded, bool isAccs)
        {
            //Count to check the nb of the lines that doesn't match the regex
            int notFormatMatchCount = 0;

            //Counter for the new added accs/proxies
            int addedProxAcc = 0;

            //String to store the message to display in the richtextbox
            string loadedAccProx = "";

            //Richtextbox to assing the corresponding richtextbox later(if it's the accs richtxtbox or the proxies one)
            RichTextBox richTxtBox;

            //List to assing the corresponding list later(if it's the accsList or the proxies ones)
            List<string> listToUse;

            //Bool to store the corresponding regex later(accs regex or proxies regex)
            bool regToCheck;

            //General counter for the loop
            int i = 0;

            //Check if we're checking accs
            if(isAccs)
            {
                //Assing the accs regex
                regToCheck = _googleAccReg.IsMatch(newProxAcc[i]) || _ptcAccReg.IsMatch(newProxAcc[i]);

                //Assing the accs richtxtbox
                richTxtBox = MainForm.accsRichTxtBox;

                //Assign the list to use
                listToUse = _verifiedAccounts;

                //Check if the accounts are loaded or manually added to assign the corresponding message
                loadedAccProx = "New"+ (autoLoaded?"loaded" : "manually added") + "accounts: ";
            }
            else
            {
                //Assign the proxies regex
                regToCheck = _proxyReg.IsMatch(newProxAcc[i]) || _proxyUserPwReg.IsMatch(newProxAcc[i]);

                //Assign the proxies richtxtbox
                richTxtBox = MainForm.proxiesRichTxtBox;

                //Assign the list to use
                listToUse = _verifiedProxies;

                //Check if the proxies are loaded or manually added to assign the corresponding message
                loadedAccProx = "New" + (autoLoaded ? "loaded" : "manually added") + "proxies: ";
            }


            //Check each line in the array of strings
            for(i = 0; i < newProxAcc.Length;i++)
            {
                //Check if it matches is regex and if it's not empty
                if(!regToCheck || newProxAcc[i] == "")
                {
                    //Increment the counter of not matched format accounts
                    notFormatMatchCount++;
                }
                else
                {
                    //Check if's manually added
                    if (!autoLoaded)
                    {
                        //Check if the user deleted manually some proxies or accounts
                        checkManuallyDeleted(newProxAcc, isAccs);
                    }

                    //Check if  it is a duplicate
                    if (!checkIfAlreadyVerified(newProxAcc[i], isAccs))
                    {
                        //Add it to the list
                        listToUse.Add(newProxAcc[i]);

                        //Increment the counter of added proxies/accounts
                        addedProxAcc++;
                    }
                }
            }
            //Clear the corresponding richtextbox
            richTxtBox.Clear();

            //Display the final list of proxies/accounts
            foreach (string str in listToUse)
            {
                richTxtBox.AppendText(str + "\n");
            }

            //Check if we added more than 0 accounts/proxies
            if (addedProxAcc > 0)
            {
                //Display the message with the new added accs/proxies nb
                MessageBox.Show(loadedAccProx + addedProxAcc);
            }
        }

        /// <summary>
        /// Method to check if the acc or proxy is a duplicate
        /// </summary>
        /// <param name="str">Get the string</param>
        /// /// <param name="isAccs">If true we check the accs list,else we check the proxies list</param>
        /// <returns>Return a boolean if the acc or proxy is a duplicate or not</returns>
        public bool checkIfAlreadyVerified(string str,bool isAccs)
        {
            //Create a list to store the correct one later
            List<string> listToCheck;

            //Check if we're checking acc
            if(isAccs)
            {
                //Assign the corresponding list
                listToCheck = _verifiedAccounts;
            }
            else
            {
                //Assign the corresponding list
                listToCheck = _verifiedProxies;
            }

            //Loop through the already verified accs
            for (int i = 0; i < listToCheck.Count(); i++)
            {
                //Check if its a duplicate
                if (str == listToCheck[i])
                {
                    //Return true if it's duplicate
                    return true;
                }
            }
            //return false if it's not a duplicate
            return false;
        }

        /// <summary>
        /// Method to check if any acc or proxy in the richtextbox has been manually deleted. If true we need to delete it from the verified accs list.
        /// </summary>
        /// <param name="richTxtBoxAcc">Get the acc:pw string</param>
        /// <param name="isAcces">Check if we are checking accounts or proxies</param>
        /// <returns>Return a boolean if the acc is a duplicate or not</returns>
        public void checkManuallyDeleted(string[] richTxtBoxAcc,bool isAccs)
        {
            //List to store the corresponding list later
            List<string> listToCheck;

            //Check if we're checking accounts
            if (isAccs)
            {
                //Assign the corresponding list
                listToCheck = _verifiedAccounts;
            }
            else
            {
                //Assign the corresponding list
                listToCheck = _verifiedProxies;
            }

            //General counter to remove from the list of acc/proxies without touching the i counter
            int counter = 0;

            //Store the size of the list we're checking
            //When we remove an item in the list they're moved in the list, so the size of the list changes and the loop doesn't see that, so we've to store it before the loop
            int listSize = listToCheck.Count();

            //Loop through the already verified accs/proxies list
            for (int i = 0; i < listSize; i++)
            {
                //Check if the acc in the list isn't present in the richtextbox
                if (!Array.Exists(richTxtBoxAcc, x => x == listToCheck[counter]))
                {
                    //Remove it from the list
                    listToCheck.RemoveAt(counter);
                }
                else
                {
                    //Increment the counter
                    counter++;
                }
            }
        }

        /// <summary>
        /// Method used to split the acc:pw list in an array
        /// </summary>
        public string[,] parseCombolist()
        {
            //Clear the richtextbox
            MainForm.accsRichTxtBox.Clear();

            //Array to save each acc with it's password
            string[,] accsPw = new string[NeededAccounts, 2];

            //Loop that will loop the neededAcconts var times
            for (int i = 0; i < NeededAccounts; i++)
            {
                //Instances a tempArray for the splitted strings
                string[] tempArray = _verifiedAccounts[0].Split(':');

                //Assign the acc password with the correct index
                accsPw[i, 0] = tempArray[0];
                accsPw[i, 1] = tempArray[1];

                //Remove it from the list(so it will only be used once)
                _verifiedAccounts.RemoveAt(0);
            }

            //Display the new verifiedAccounts list without the used accs
            foreach (string str in _verifiedAccounts)
            {
                MainForm.accsRichTxtBox.AppendText(str + "\n");
            }

            return accsPw;
        }

        /// <summary>
        /// Method to filter out the proxies with user:pw and withouth, and keep only the ones that we'll not use for further use.
        /// </summary>
        /// <returns>Return a list with the final proxies to use</returns>
        public string[,] clearAndFilterProxiesList()
        {
            //Clear the richTxtBox
            MainForm.proxiesRichTxtBox.Clear();

            //Array to save each acc with it's password
            string[,] proxiesWithUserPw = new string[NeededAccounts, 3];

            //Declare an array to temporary place the proxies that we'll use
            string[] tempProxiesList = new string[NeededAccounts];

            //Get the range of proxies that we'll use, copy them to the proxiesList, then delete them from the verified proxies list
            _verifiedProxies.CopyTo(0, tempProxiesList, 0, tempProxiesList.Length);
            _verifiedProxies.RemoveRange(0, tempProxiesList.Length);

            //Counter to don't touch the "i" counter and avoid being out of index
            int counter = 0;

            //Loop that will loop the neededAcconts var times
            for (int i = 0; i < NeededAccounts; i++)
            {
                //Check if the proxy has a user and pw
                if (_proxyUserPwReg.IsMatch(tempProxiesList[i]))
                {

                    //Instances a tempArray for the splitted strings
                    string[] tempArray = tempProxiesList[i].Split(':');

                    //Assign the proxy,account and password with the correct indexes
                    proxiesWithUserPw[counter, 0] = tempArray[0] + ":" + tempArray[1];
                    proxiesWithUserPw[counter, 1] = tempArray[2];
                    proxiesWithUserPw[counter, 2] = tempArray[3];
                    counter++;
                }
                else
                {
                    //Assign the proxy and port + null to username and password(default values)
                    proxiesWithUserPw[counter, 0] = tempProxiesList[i];
                    proxiesWithUserPw[counter, 1] = null;
                    proxiesWithUserPw[counter, 2] = null;
                    counter++;
                }
            }

            //Write the proxies that we didn't use to the textbox
            foreach (string str in _verifiedProxies)
            {
                MainForm.proxiesRichTxtBox.Text += str + "\n";
            }

            return proxiesWithUserPw;
        }

        /// <summary>
        /// Method used to make the auth.json file for each botfolder with each of the verified accs
        /// </summary>
        /// <param name="accsPw">Get the array with the accs and passwords</param>
        public void makeAuthAndRndCfg(string[,] accsPw,string[,] proxiesList)
        {
            //Loop the number of entries in nameFolders list(Array with the name of the created folders)
            for (int i = 0; i < _nameFolders.Count(); i++)
            {
                //********MAKE AUTH FILES***************//
                //Copy the auth.json file to each of the bot folders
                File.WriteAllBytes(DirToPlaceFolders + _nameFolders[i] + AUTH_FOLDER_NAME, Properties.Resources.auth);

                //Save the auth.json file in a string
                string json = File.ReadAllText(DirToPlaceFolders + _nameFolders[i] + AUTH_FOLDER_NAME);
                dynamic jsonObj = JsonConvert.DeserializeObject(json);

                //Check wether is a google account or a PTC acc
                if (accsPw[i, 0].Contains("@gmail.com"))
                {
                    //Set authtype, username and password in the auth.json
                    jsonObj["AuthType"] = "google";
                    jsonObj["GoogleUsername"] = "" + accsPw[i, 0] + "";
                    jsonObj["GooglePassword"] = "" + accsPw[i, 1] + "";
                }
                else
                {
                    //Set the authtype, username and password in the auth.json
                    jsonObj["AuthType"] = "ptc";
                    jsonObj["PtcUsername"] = "" + accsPw[i, 0] + "";
                    jsonObj["PtcPassword"] = "" + accsPw[i, 1] + "";
                }

                //Set the proxies
                jsonObj["UseProxy"] = true;
                jsonObj["ProxyUri"] = "" + proxiesList[i,0] + "";

                //Check if the proxies doesn't have user/pass, if they don't we'll keep the default value, if they do we set the user and pass.
                if (proxiesList[i, 1] != null && proxiesList[i, 2] != null)
                {
                    jsonObj["ProxyLogin"] = "" + proxiesList[i, 1] + "";
                    jsonObj["ProxyPass"] = "" + proxiesList[i, 2] + "";
                }


                //Convert back to json
                string output = JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);

                //Copy the file to a bot folder
                File.WriteAllText(DirToPlaceFolders + _nameFolders[i] + AUTH_FOLDER_NAME, output);

                //******MODIFY CONFIG FILES WITH RANDOM DEVICE FINGERPRINT*******//
                //Generate a random device
                DeviceSettings device = new DeviceSettings();

                //Save the auth.json file in a string
                string jsonCfg = File.ReadAllText(DirToPlaceFolders + _nameFolders[i] + CONFIG_FOLDER_NAME);
                dynamic jsonObjCfg = JsonConvert.DeserializeObject(jsonCfg);

                //Set the username and password in the auth.json
                jsonObjCfg["DeviceSettings"]["DeviceId"] = device.DeviceId;
                jsonObjCfg["DeviceSettings"]["AndroidBoardName"] = device.AndroidBoardName;
                jsonObjCfg["DeviceSettings"]["AndroidBootLoader"] = device.AndroidBootLoader;
                jsonObjCfg["DeviceSettings"]["DeviceBrand"] = device.DeviceBrand;
                jsonObjCfg["DeviceSettings"]["DeviceModel"] = device.DeviceModel;
                jsonObjCfg["DeviceSettings"]["DeviceModelIdentifier"] = device.DeviceModelIdentifier;
                jsonObjCfg["DeviceSettings"]["DeviceModelBoot"] = device.DeviceModelBoot;
                jsonObjCfg["DeviceSettings"]["HardwareManufacturer"] = device.HardwareManufacturer;
                jsonObjCfg["DeviceSettings"]["HardWareModel"] = device.HardWareModel;
                jsonObjCfg["DeviceSettings"]["FirmwareBrand"] = device.FirmwareBrand;
                jsonObjCfg["DeviceSettings"]["FirmwareTags"] = device.FirmwareTags;
                jsonObjCfg["DeviceSettings"]["FirmwareType"] = device.FirmwareType;
                jsonObjCfg["DeviceSettings"]["FirmwareFingerprint"] = device.FirmwareFingerprint;

                //Convert back to json
                string outputCfg = JsonConvert.SerializeObject(jsonObjCfg, Newtonsoft.Json.Formatting.Indented);

                //Copy the file to a bot folder
                File.WriteAllText(DirToPlaceFolders + _nameFolders[i] + CONFIG_FOLDER_NAME, outputCfg);
            }
        }
    }
}
