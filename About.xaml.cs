using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;

namespace WpfAnnotations
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {

        /// <summary>
        /// This constructor tries to mirror the one used in the WinformsDemos
        /// </summary>
        /// <param name="dialogLabel"></param>
        /// <param name="demoName"></param>
        public About(string dialogLabel, string demoName)
        {
            InitializeComponent();

            // This is the window Title
            Title = dialogLabel;

            // This is the name of the demo at the top of the About Page
            DemoNameField.Content = demoName;

            // This is the Demo Gallery Link Label
            SetDemoGalleryLinkLabel(demoName);
        }


        #region Alt Constructors
        /// <summary>
        /// Simplified constructor for when you just want mostly defaults
        /// </summary>
        /// <param name="demoName"></param>
        public About(string demoName)
        {
            InitializeComponent();

            // This is the window Title
            Title = "About " + demoName;

            // This is the name of the demo at the top of the About Page
            DemoNameField.Content = demoName;

            // This is the Demo Gallery Link Label
            SetDemoGalleryLinkLabel(demoName);
        }

        /// <summary>
        /// An all-in-one version of the constructor for when you want to set everything
        /// </summary>
        /// <param name="demoName">Name of the Demo</param>
        /// <param name="demoLink">Link to the demo's home page</param>
        /// <param name="demoDesc">Full Description of the demo (separate graphs with two crlf</param>
        public About(string demoName, string demoLink, string demoDesc): this("About " + demoName, demoName, demoLink, demoDesc)
        {
        }

        /// <summary>
        /// Full-monte - when you want to set all the params up front in the constructor
        /// </summary>
        /// <param name="dialogDesc">The title for the about window</param>
        /// <param name="demoName">Name of the Demo</param>
        /// <param name="demoLink">Link to the demo's home page</param>
        /// <param name="demoDesc">Full Description of the demo (separate graphs with two crlf</param>
        public About(string dialogDesc, string demoName, string demoLink, string demoDesc)
        {
            InitializeComponent();

            // This is the window Title
            Title = dialogDesc;

            // This is the name of the demo at the top of the About Page
            DemoNameField.Content = demoName;

            // This is the Demo Gallery Link Label
            SetDemoGalleryLinkLabel(demoName);

            // set the hyperlink
            SetDemoGalleryLink(demoLink);

            // set the description
            SetDescription(demoDesc);
        }

        public About()
        {
            InitializeComponent();
        }
        #endregion Alt Constructors


        #region Properties
        public string Link
        {
            get { return this.demoGalleryLink.NavigateUri.ToString(); }
            set { SetDemoGalleryLink(value); }
        }

        //public string Title
        //{
        //    get { return this.Title; }
        //    set { this.Title = value; }
        //}

        //public string Name
        //{
        //    get { return this.DemoNameField.Content.ToString(); }
        //    set { this.DemoNameField.Content = value; }
        //}

        public string Description
        {
            get { return this.DemoDescription.Text; }
            set { SetDescription(value); } 
        }
        #endregion Properties


        #region Private Methods
        private void SetDemoGalleryLinkLabel(string demoName)
        {
            // This is the Demo Gallery Link Label
            if (demoName.Contains("Demo"))
            {
                demoGalleryLinkLabel.Text = demoName + " Home";
            }
            else
            {
                demoGalleryLinkLabel.Text = demoName + " Demo Home";
            }
        }
        #endregion Private Methods


        #region Public Methods
        public void SetDemoGalleryLink(string uri)
        {
            if (!uri.Contains("http://") && !uri.Contains("https://"))
            {
                demoGalleryLink.NavigateUri = new Uri("http://" + uri);
            }
            else
            {
                demoGalleryLink.NavigateUri = new Uri(uri);
            }
        }

        public void SetDescription(string desc)
        {
            this.DemoDescription.Text = desc;
        }
        #endregion Public Methods


        #region Event Handlers
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Uri.OriginalString))
            {
                Process.Start(e.Uri.OriginalString);
            }
        }
        #endregion Event Handlers
    }   
}
