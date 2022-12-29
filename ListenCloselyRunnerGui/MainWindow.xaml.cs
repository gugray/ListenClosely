using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Tool
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        private string lastSelectedRoot = Program.toAbsolutePath(".\\");
        private string [] lastArgs = null;

        public MainWindow()
        {
           InitializeComponent();
           InitializeComponents();

           this.SourceInitialized += (x, y) =>
           {
               this.HideMinimizeAndMaximizeButtons();
           };
        }
        
        private void InitializeComponents()
        {
            L_STATE.Text = "Not started...";
            F_CUSDIC.IsEnabled = false;
            B_SELECTDIC.IsEnabled = false;

            CMB_AUDIO_FORMAT.Items.Clear();
            CMB_TITLE_LINES_CNT.Items.Clear();
            CMB_POST_LEMMATIZING_OFOS.Items.Clear();
            CMB_POST_FFMPEG_OFOS.Items.Clear();
            CMB_SPEECH_API_OFOS.Items.Clear();
            CMB_LEMMATIZING_OFOS.Items.Clear();

            {
                // audio format select box
                CMB_AUDIO_FORMAT.Items.Add(getListItem(""));
                CMB_AUDIO_FORMAT.Items.Add(getListItem(Program.AUDIO_MP3));
                CMB_AUDIO_FORMAT.Items.Add(getListItem(Program.AUDIO_WAV));

                CMB_AUDIO_FORMAT.SelectedIndex = 0;
            }

            {
                // count of title lines select box
                for (int i=0; i <= 10; i++)
                {
                    CMB_TITLE_LINES_CNT.Items.Add(getListItem("" + i));
                }

                CMB_TITLE_LINES_CNT.SelectedIndex = 0;
            }

            {
                // after lemmatizing strategy
                CMB_POST_LEMMATIZING_OFOS.Items.Add(getListItem(Program.BREAK));
                CMB_POST_LEMMATIZING_OFOS.Items.Add(getListItem(Program.PROCESS));

                CMB_POST_LEMMATIZING_OFOS.SelectedIndex = 0;
            }

            {
                // audio convertion data strategy
                CMB_POST_FFMPEG_OFOS.Items.Add(getListItem(Program.SKIP));
                CMB_POST_FFMPEG_OFOS.Items.Add(getListItem(Program.OVERWRITE));
                CMB_POST_FFMPEG_OFOS.Items.Add(getListItem(Program.BACKUP));
                CMB_POST_FFMPEG_OFOS.Items.Add(getListItem(Program.BREAK)); 

                CMB_POST_FFMPEG_OFOS.SelectedIndex = 0;
            }

            {
                // speech recognition data strategy
                CMB_SPEECH_API_OFOS.Items.Add(getListItem(Program.SKIP));
                CMB_SPEECH_API_OFOS.Items.Add(getListItem(Program.OVERWRITE));
                CMB_SPEECH_API_OFOS.Items.Add(getListItem(Program.BACKUP));
                CMB_SPEECH_API_OFOS.Items.Add(getListItem(Program.BREAK));

                CMB_SPEECH_API_OFOS.SelectedIndex = 0;
            }

            {
                // lemmatizing data strategy
                CMB_LEMMATIZING_OFOS.Items.Add(getListItem(Program.SKIP));
                CMB_LEMMATIZING_OFOS.Items.Add(getListItem(Program.OVERWRITE));
                CMB_LEMMATIZING_OFOS.Items.Add(getListItem(Program.BACKUP));
                CMB_LEMMATIZING_OFOS.Items.Add(getListItem(Program.BREAK));

                CMB_LEMMATIZING_OFOS.SelectedIndex = 1;
            }
        }


        private void C_SELECTDIC_Unchecked(object sender, RoutedEventArgs e)
        {
            F_CUSDIC.IsEnabled = false;
            B_SELECTDIC.IsEnabled = false;
            DisableSave(sender, e);
        }
        private void C_SELECTDIC_Checked(object sender, RoutedEventArgs e)
        {
            F_CUSDIC.IsEnabled = true;
            B_SELECTDIC.IsEnabled = true;
            DisableSave(sender, e);
        }

        private void B_CHECK_ABBR_Click(object sender, RoutedEventArgs e)
        {
            checkLocalDataForSetAbbreviation();
        }

        private void B_SELECTDIC_Click(object sender, RoutedEventArgs e)
        {
            selectCustDict();
        }

        private void B_LOADRUN_Click(object sender, RoutedEventArgs e)
        {
            loadRunSettings();
        }

        private void B_SAVERUN_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveRunSettings();
            }
            catch (Exception ex) 
            {
                showError("ERROR BY SAVE RUN FILE", ex.Message);
            }
        }

        private void B_CLOSE_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void B_GOTO_Click(object sender, RoutedEventArgs e)
        {
            string outDirPath = Program.getOutDirPath();
            if (Directory.Exists(outDirPath)) { 
                Process.Start("explorer.exe", @outDirPath);
            }
        }

        /**
         * Load the settings from an existing *.run file
         */
        private void loadRunSettings()
        {
            string root = Program.getRunsDirPath();
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = root;
            openFileDialog.Filter = "Run files|*.run";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                string [] args = Program.readRunPropertiesArrayFromFile(openFileDialog.FileName);
                if (args[0] != null) F_ABBREVIATION.Text = args[0];
                if (args[1] != null) CMB_AUDIO_FORMAT.Text = args[1].ToLower();
                if (args[2] != null) F_TITLE.Text = args[2];
                if (args[3] != null) CMB_TITLE_LINES_CNT.Text = args[3];
                if (args[4] != null) {
                    try { C_VERSES.IsChecked = bool.Parse(args[4]); } 
                    catch (Exception ex) { C_VERSES.IsChecked = false;  }
                }
                if (args[5] != null)
                {
                    C_SELECTDIC.IsChecked = true; 
                    F_CUSDIC.Text = args[5];
                }
                if (args[6] != null) CMB_POST_LEMMATIZING_OFOS.Text = args[6].ToLower();
                if (args[7] != null) CMB_POST_FFMPEG_OFOS.Text = args[7].ToLower();
                if (args[8] != null) CMB_SPEECH_API_OFOS.Text = args[8].ToLower();
                if (args[9] != null) CMB_LEMMATIZING_OFOS.Text = args[9].ToLower();
            }
            // Open the run file and take over the settings
                

        }

        /**
         * Save the current run settings into a new *.run file
         */
        private void saveRunSettings()
        {
            if(lastArgs == null)
            {
                showError("ERROR BY SAVE RUN SETTINGS", "No collected arguments found");
                return;
            }

            // Define the file location. If the run file exists - ask what to do: replace or append. Else create a new one.
            string runsDir = Program.getRunsDirPath();
            if (!Directory.Exists(runsDir))
            {
                Directory.CreateDirectory(runsDir);
            }

            // TODO next steps
            string fileName = runsDir + "\\" + lastArgs[0] + ".run";
            string fileNameTmp = fileName + ".tmp";

            FileInfo fi = new FileInfo(fileName);

            StreamWriter sw = null;
            string line;
            try
            {
                // write a temporary run file
                using (sw = new StreamWriter(fileNameTmp))
                {
                    // The run file found
                    if (File.Exists(fileName))
                    {
                        MessageBoxResult res = prompt("The file '" + fi.FullName + "' exists." + Environment.NewLine + "Yes for Overwrite / No for Append");
                        if (res == MessageBoxResult.Cancel)
                        {
                            return;
                        }

                        // Ovrewrite
                        if (res == MessageBoxResult.No)
                        {
                            using (StreamReader sr = new StreamReader(fileName))
                            {
                                while ((line = sr.ReadLine()) != null)
                                {
                                    if(line.Trim().Length == 0) 
                                    {
                                        line = "#";
                                    }
                                    else if(!line.StartsWith("#"))
                                    {
                                        line = "# " + line;
                                    }
                                    sw.WriteLine(line);
                                    sw.Flush();
                                }
                            }
                            sw.WriteLine("#");
                        }
                    }

                    // Add the current run data
                    String now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    sw.WriteLine("# RUN SAVED: " + now);
                    sw.WriteLine("# --------------------");
                    sw.WriteLine("#");
                    sw.Flush();

                    sw.WriteLine(Program.PROP_KEY_ABBREVIATION + "=" + lastArgs[0]);
                    sw.WriteLine(Program.PROP_KEY_AUDIO_FORMAT + "=" + lastArgs[1]);
                    sw.WriteLine(Program.PROP_KEY_TITLE + "=" + lastArgs[2]);
                    sw.WriteLine(Program.PROP_KEY_SHIFT_TITLE_LINES + "=" + lastArgs[3]);
                    sw.WriteLine(Program.PROP_KEY_VERSES + "=" + lastArgs[4]);
                    sw.WriteLine(Program.PROP_KEY_CUSTOM_DIC + "=" + lastArgs[5]);
                    sw.WriteLine(Program.PROP_KEY_POST_LEMMATIZING_OFOS + "=" + lastArgs[6]);
                    sw.WriteLine(Program.PROP_KEY_FFMPEG_OFOS + "=" + lastArgs[7]);
                    sw.WriteLine(Program.PROP_KEY_SPEECH_API_OFOS + "=" + lastArgs[8]);
                    sw.WriteLine(Program.PROP_KEY_LEMMATIZING_OFOS + "=" + lastArgs[9]);
                    sw.Flush();
                }

                // rename the temporary file to the run file
                File.Move(fileNameTmp, fileName, true);
              }
            finally
            {
                // same as if(sw!=null)
                sw?.Close();
            }
          }

        /**
         * Provide a file select dialog for select the customer dictionary file
         */
        private void selectCustDict()
        {
            string root = lastSelectedRoot;

            F_CUSDIC.Text = F_CUSDIC.Text.Trim();
            if (F_CUSDIC.Text.Length > 0)
            {
                FileInfo fi = new FileInfo(F_CUSDIC.Text);
                try
                {
                    if(Directory.Exists(fi.FullName))
                    {
                        root = fi.FullName;
                    }
                    else
                    {
                        DirectoryInfo di = fi.Directory;
                        if(di.Exists)
                        {
                            root = di.FullName;
                        }
                    }
                }
                catch(Exception ex) {
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.InitialDirectory = root;
            openFileDialog.Filter = "Custom dictionary files|*.txt";
            openFileDialog.FilterIndex = 0;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                lastSelectedRoot = root;
                F_CUSDIC.Text = openFileDialog.FileName;
            }
        }

        /**
         * Starts a select dialog by provide a collection of the potentially usable abbreviations, based on the current data set.
         */
        private void checkLocalDataForSetAbbreviation()
        {
            F_ABBREVIATION.Text = F_ABBREVIATION.Text.Trim();
            string selectedAbbreviation = F_ABBREVIATION.Text.ToUpper();

            List<Program.LocalWorkDataBundle> inputFiles = null;
            SelectAbbreviationWindow sabw = null;
            try
            { 
                inputFiles = Program.getInputFiles();
                // show a selection for select an existing abbreviation

                if(inputFiles.Count > 0)
                {
                    sabw = new SelectAbbreviationWindow(inputFiles, selectedAbbreviation);
                    sabw.Top = (this.Top + this.Height / 2) - sabw.Height/2;
                    sabw.Left = (this.Left + this.Width / 2) - sabw.Width / 2;
                    sabw.ShowDialog();
                    if(sabw.isCommit() && sabw.getAbbreviation() != null)
                    {
                        selectedAbbreviation = sabw.getAbbreviation();
                    }
                }
                else
                {
                    showInfo("No work files found");
                }
            }
            catch(Exception ex)
            {
                showWarning(ex.Message);
                return;
            }

            if (selectedAbbreviation.Length == 0) { return; }

            F_ABBREVIATION.Text = selectedAbbreviation;

            foreach (Program.LocalWorkDataBundle db in inputFiles)
            {
                if(selectedAbbreviation == db.Abbreviation)
                {
                    if (db.Mp3Path != null)
                    {
                        CMB_AUDIO_FORMAT.SelectedIndex = 1;
                    }
                    else if (db.WavPath != null)
                    {
                        CMB_AUDIO_FORMAT.SelectedIndex = 2;
                    }
                }
            }
        }

        private void validateArgs()
        {
            // check mandatory settings
            F_ABBREVIATION.Text = F_ABBREVIATION.Text.Trim();
            if (F_ABBREVIATION.Text.Length == 0) throw new Exception("The abbreviation is a mandatory field!");
            F_TITLE.Text = F_TITLE.Text.Trim();
            if (F_TITLE.Text.Length == 0) throw new Exception("The title is a mandatory field!");
            if (C_SELECTDIC.IsChecked == true)
            {
                F_CUSDIC.Text = F_CUSDIC.Text.Trim();
                if(F_CUSDIC.Text.Length == 0)
                {
                    throw new Exception("Please set the path to custom dictionary or uncheck the checkbox!");
                }
                F_CUSDIC.Text = Program.toAbsolutePath(F_CUSDIC.Text);
                if(!File.Exists(F_CUSDIC.Text))
                {
                    // TODO clone the sample under the given name???
                    throw new Exception("Please set the correct path to an existing custom dictionary or uncheck the checkbox!");
                }
            }
        }

        private string[] buildArgs()
        {
            validateArgs();

            string[] args = new string[10];
            lastArgs = new string[10];

            Array.Fill(args, null);
            Array.Fill(lastArgs, null);

            lastArgs[0] = F_ABBREVIATION.Text.ToUpper();
            args[0] = Program.ARG_KEY_ABBREVIATION_SHORT + quote(lastArgs[0]);
            lastArgs[1] = CMB_AUDIO_FORMAT.Text;
            args[1] = Program.ARG_KEY_AUDIO_FORMAT_SHORT + quote(lastArgs[1]);
            lastArgs[2] = F_TITLE.Text;
            args[2] = Program.ARG_KEY_TITLE_SHORT + quote(lastArgs[2]);
            lastArgs[3] = CMB_TITLE_LINES_CNT.Text;
            args[3] = Program.ARG_KEY_SHIFT_TITLE_LINES_SHORT + quote(lastArgs[3]);
            lastArgs[4] = "" +C_VERSES.IsChecked;
            args[4] = Program.ARG_KEY_VERSES_SHORT + lastArgs[4];
            if (C_SELECTDIC.IsChecked == true) {
                lastArgs[5] = Program.toAbsolutePath(F_CUSDIC.Text).Replace("\\\\", "\\");
                args[5] = Program.ARG_KEY_CUSTOM_DIC_SHORT + quote(lastArgs[5].Replace("\\", "\\\\"));
            }
            lastArgs[6] = CMB_POST_LEMMATIZING_OFOS.Text;
            args[6] = Program.ARG_KEY_POST_LEMMATIZING_OFOS_SHORT + quote(lastArgs[6]);
            lastArgs[7] = CMB_POST_FFMPEG_OFOS.Text;
            args[7] = Program.ARG_KEY_FFMPEG_OFOS_SHORT + quote(lastArgs[7]);
            lastArgs[8] = CMB_SPEECH_API_OFOS.Text;
            args[8] = Program.ARG_KEY_SPEECH_API_OFOS_SHORT + quote(lastArgs[8]);
            lastArgs[9] = CMB_LEMMATIZING_OFOS.Text;
            args[9] = Program.ARG_KEY_LEMMATIZING_OFOS_SHORT + quote(lastArgs[9]);

            return args;
        }

        private void readArgs(string[] args)
        {
            args[0] = Program.ARG_KEY_ABBREVIATION_SHORT + quote(F_ABBREVIATION.Text.ToUpper());
            args[1] = Program.ARG_KEY_AUDIO_FORMAT_SHORT + quote(CMB_AUDIO_FORMAT.Text);
            args[2] = Program.ARG_KEY_TITLE_SHORT + quote(F_TITLE.Text);
            args[3] = Program.ARG_KEY_SHIFT_TITLE_LINES_SHORT + quote(CMB_TITLE_LINES_CNT.Text);
            args[4] = Program.ARG_KEY_VERSES_SHORT + C_VERSES.IsChecked;
            if (C_SELECTDIC.IsChecked == true)
            {
                args[5] = Program.ARG_KEY_CUSTOM_DIC_SHORT + quote(Program.toAbsolutePath(F_CUSDIC.Text).Replace("\\", "\\\\"));
            }
            else
            {
                args[5] = Program.ARG_KEY_CUSTOM_DIC_SHORT + quote("");
            }

            args[6] = Program.ARG_KEY_POST_LEMMATIZING_OFOS_SHORT + quote(CMB_POST_LEMMATIZING_OFOS.Text);
            args[7] = Program.ARG_KEY_FFMPEG_OFOS_SHORT + quote(CMB_POST_FFMPEG_OFOS.Text);
            args[8] = Program.ARG_KEY_SPEECH_API_OFOS_SHORT + quote(CMB_SPEECH_API_OFOS.Text);
            args[9] = Program.ARG_KEY_LEMMATIZING_OFOS_SHORT + quote(CMB_LEMMATIZING_OFOS.Text);
        }


        private void B_RUN_Click(object sender, RoutedEventArgs e)
        {
            L_STATE.Text = "Not started...";

            Program p = null;
            string[] args = null;

            Cursor c = this.Cursor;
            this.Cursor = Cursors.Wait;
            this.IsEnabled = false;

            try
            {
                args = buildArgs();
            }
            catch (Exception ex)
            {
                showError("ERROR", ex.Message);
                L_STATE.Text = "Error by validation";
                this.Cursor = c;
                this.IsEnabled = true;
                return;
            }

            try
            {
                p = new Program();
            }
            catch (Exception ex)
            {
                showError("PROGRAM PREPARATION ERROR", ex.Message);
                L_STATE.Text = "Error by preparation";
                this.Cursor = c;
                this.IsEnabled = true;
                return;
            }

            L_STATE.Text = "Start...";

            p.setGuiOutField(L_STATE);

            try
            {
                p.start(args);
                EnableSave();
            }
            catch (Exception ex)
            {
                showError("PROGRAM EXECUTION ERROR", ex.Message);
                L_STATE.Text = "Error by execution";
                this.Cursor = c;
                this.IsEnabled = true;
                return;
            }

            this.Cursor = c;
            this.IsEnabled = true;
        }

        private ListViewItem getListItem(string text)
        {
            ListViewItem it = new ListViewItem();
            it.Content = text.ToLower();
            return it;
        }

        private void EnableSave()
        {
            B_SAVERUN.IsEnabled = true;
        }

        private void DisableSave(object sender, RoutedEventArgs e)
        {
            B_SAVERUN.IsEnabled = false;
        }

        private string quote(string line)
        {
            return "\"" + line + "\"";
        }

        private void showError(string caption, string messageBoxText)
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Error;
            MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.OK);
        }

        private void showInfo(string messageBoxText)
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            MessageBox.Show(messageBoxText, "INFORMATION", button, icon, MessageBoxResult.OK);
        }

        private void showWarning(string messageBoxText)
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBox.Show(messageBoxText, "WARNING", button, icon, MessageBoxResult.OK);
        }

        private MessageBoxResult prompt(string messageBoxText)
        {
            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;
            return MessageBox.Show(messageBoxText, "WARNING", button, icon, MessageBoxResult.Yes);
        }
    }

    /**
     * Some piece of foreign code... 
     * It is for disallow "minimize" and "maximize" buttns
     */
    internal static class WindowExtensions
    {
        // from winuser.h
        private const int GWL_STYLE = -16,
                          WS_MAXIMIZEBOX = 0x10000,
                          WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll")]
        extern private static int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        extern private static int SetWindowLong(IntPtr hwnd, int index, int value);

        internal static void HideMinimizeAndMaximizeButtons(this Window window)
        {
            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var currentStyle = GetWindowLong(hwnd, GWL_STYLE);

            SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
        }
    }
}
