using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Tool
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string lastSelectedRoot = Program.toAbsolutePath(".\\");

        public MainWindow()
        {
           InitializeComponent();
           InitializeComponents();
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

        private ListViewItem getListItem(string text)
        {
            ListViewItem it = new ListViewItem();
            it.Content = text.ToLower();
            return it;
        }

        private bool PreValidate()
        { 
            // check the exe file exists
            return true; 
        }

        private void C_SELECTDIC_Unchecked(object sender, RoutedEventArgs e)
        {
            F_CUSDIC.IsEnabled = false;
            B_SELECTDIC.IsEnabled = false;
        }
        private void C_SELECTDIC_Checked(object sender, RoutedEventArgs e)
        {
            F_CUSDIC.IsEnabled = true;
            B_SELECTDIC.IsEnabled = true;
        }

        private void B_CHECK_ABBR_Click(object sender, RoutedEventArgs e)
        {
            validateAbbreviation(false);
        }

        private void B_SELECTDIC_Click(object sender, RoutedEventArgs e)
        {
            selectCustDict();
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
            
            if (openFileDialog.ShowDialog() == true)
            {
                lastSelectedRoot = root;
                F_CUSDIC.Text = openFileDialog.FileName;
            }
        }

        private void validateAbbreviation(bool final)
        {
            F_ABBREVIATION.Text = F_ABBREVIATION.Text.Trim();
            if (F_ABBREVIATION.Text.Length == 0)
            {
                if (final)
                {
                    throw new Exception("The abbreviation is a mandatory field!");
                }
                showWarning("The abbreviation is a mandatory field!");
                return;
            }

            string[][] dataFound = Program.checkInputFilesByAbbreviation(F_ABBREVIATION.Text);
            // no -orig file found
            if (dataFound[0][1] == "0")
            {
                if (final)
                {
                    throw new Exception("The text file '" + dataFound[0][0] + "' is missing for abbreviation!");
                }
                showWarning("The text file '" + dataFound[0][0] + "' is missing for abbreviation!");
                return;
            }

            // no audio found for abbreviation
            if (dataFound[1][1] == "0" && dataFound[2][1] == "0")
            {
                if (final)
                {
                    throw new Exception("No audio files found for abbreviation.\nPlease provide an MP3 file '" + dataFound[1][0] + "'\nor a WAV file '" + dataFound[2][0] + "'!");
                }
                showWarning("No audio files found for abbreviation.\nPlease provide an MP3 file '" + dataFound[1][0] + "'\nor a WAV file '" + dataFound[2][0] + "'!");
                return;
            }

            // No audio format selected
            if(CMB_AUDIO_FORMAT.SelectedIndex < 1)
            {
                if (final)
                {
                    throw new Exception("The audio format is a mandatory field!");
                }

                if (dataFound[1][1] == "1")
                {
                    // MP3 file found, autoset combo box to 1
                    CMB_AUDIO_FORMAT.SelectedIndex = 1;
                    showWarning("The audio file type was automatically reset to MP3 according to the found data, file '" + dataFound[1][0] + "'!");
                    return;
                }
                else if (dataFound[2][1] == "1")
                {
                    // WAV file found, autoset combo box to 2
                    CMB_AUDIO_FORMAT.SelectedIndex = 2;
                    showWarning("The audio file type was automatically reset to WAV according to the found data, file '" + dataFound[2][0] + "'!");
                    return;
                }
            }
            else
            {
                // selected format is WAV, but only MP3 found for abbreviation
                if (CMB_AUDIO_FORMAT.SelectedIndex == 2 && dataFound[1][1] == "1" && dataFound[2][1] == "0")
                {
                    if(final)
                    {
                        throw new Exception("No WAV audio file found for abbreviation (but an MP3 file exists)!");
                    }

                    CMB_AUDIO_FORMAT.SelectedIndex = 1;
                    showWarning("The audio file type was automatically reset to MP3 according to the found data, file '" + dataFound[1][0] + "'!");
                    return;
                }

                // selected format is MP3, but only WAV found for abbreviation
                if (CMB_AUDIO_FORMAT.SelectedIndex == 1 && dataFound[1][1] == "0" && dataFound[2][1] == "1")
                {
                    if (final)
                    {
                        throw new Exception("No MP3 audio file found for abbreviation (but a WAV file exists)!");
                    }

                    CMB_AUDIO_FORMAT.SelectedIndex = 2;
                    showWarning("The audio file type was automatically reset to WAV according to the found data, file '" + dataFound[2][0] + "'!");
                    return;
                }
            }
        }

        private void validateArgs()
        {
            // check mandatory settings
            validateAbbreviation(true);
            F_TITLE.Text = F_TITLE.Text.Trim();
            if (F_TITLE.Text.Length == 0) throw new Exception("The title is a mandatory field!");
            if (C_SELECTDIC.IsChecked == true)
            {
                F_CUSDIC.Text = F_CUSDIC.Text.Trim();
                if(F_CUSDIC.Text.Length == 0)
                {
                    throw new Exception("Please set the path to custom dictionary or uncheck the checkbox!");
                }
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
            int i = -1;
            args[++i] = Program.ARG_KEY_ABBREVIATION_SHORT + quote(F_ABBREVIATION.Text.ToUpper());
            args[++i] = Program.ARG_KEY_AUDIO_FORMAT_SHORT + quote(CMB_AUDIO_FORMAT.Text);
            args[++i] = Program.ARG_KEY_TITLE_SHORT + quote(F_TITLE.Text);
            args[++i] = Program.ARG_KEY_SHIFT_TITLE_LINES_SHORT + quote(CMB_TITLE_LINES_CNT.Text);
            args[++i] = Program.ARG_KEY_VERSES_SHORT + C_VERSES.IsChecked;
            if (C_SELECTDIC.IsChecked == true) {
                args[++i] = Program.ARG_KEY_CUSTOM_DIC_SHORT + quote(Program.toAbsolutePath(F_CUSDIC.Text).Replace("\\", "\\\\"));
            }
            else
            {
                args[++i] = Program.ARG_KEY_CUSTOM_DIC_SHORT + quote("");
            }

            args[++i] = Program.ARG_KEY_POST_LEMMATIZING_OFOS_SHORT + quote(CMB_POST_LEMMATIZING_OFOS.Text);
            args[++i] = Program.ARG_KEY_FFMPEG_OFOS_SHORT + quote(CMB_POST_FFMPEG_OFOS.Text);
            args[++i] = Program.ARG_KEY_SPEECH_API_OFOS_SHORT + quote(CMB_SPEECH_API_OFOS.Text);
            args[++i] = Program.ARG_KEY_LEMMATIZING_OFOS_SHORT + quote(CMB_LEMMATIZING_OFOS.Text);
            
            return args;
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
        private void showWarning(string messageBoxText)
        {
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Warning;
            MessageBox.Show(messageBoxText, "WARNING", button, icon, MessageBoxResult.OK);
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

            p.setGuiOutField(L_STATE);

            try
            {
                p.start(args);
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

        private void L_STATE_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {

        }
    }
}
