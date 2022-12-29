using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Tool
{

    public partial class SelectAbbreviationWindow : Window
    {
        private bool commit = false;

        public SelectAbbreviationWindow(List<Program.LocalWorkDataBundle> inputFiles, string selectedValue)
        {
            InitializeComponent();
            InitializeComponents(inputFiles, selectedValue);
            commit = false;

            this.SourceInitialized += (x, y) =>
            {
                this.HideMinimizeAndMaximizeButtons();
            };
        }

        private void InitializeComponents(List<Program.LocalWorkDataBundle> inputFiles, string selectedValue)
        {
            CMB_AELECTABBR.Items.Clear();
            CMB_AELECTABBR.Items.Add(getListItem(""));
            CMB_AELECTABBR.SelectedIndex = 0;

            int i = 1;
            foreach (Program.LocalWorkDataBundle db in inputFiles)
            {
                string abbr = db.Abbreviation.ToUpper();
                CMB_AELECTABBR.Items.Add(getListItem(db.Abbreviation.ToUpper()));
                if(abbr == selectedValue)
                {
                    CMB_AELECTABBR.SelectedIndex= i;
                }
                i++;
            }
        }

        private void B_OK_Click(object sender, RoutedEventArgs e)
        {
            commit = true;
            Close();
        }


        private void B_CANCEL_Click(object sender, RoutedEventArgs e)
        {
            commit = false;
            Close();
        }

        public bool isCommit()
        {
            return commit;
        }

        public string getAbbreviation()
        {
            return commit ? CMB_AELECTABBR.Text : null;
        }


        private ListViewItem getListItem(string text)
        {
            ListViewItem it = new ListViewItem();
            it.Content = text.ToUpper();
            return it;
        }

    }
}
