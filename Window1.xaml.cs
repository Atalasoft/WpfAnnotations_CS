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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Atalasoft.Annotate.Wpf;
using Atalasoft.Annotate;
using Atalasoft.Annotate.UI;
using Atalasoft.Annotate.Icons;
using System.IO;
using Atalasoft.Imaging.Codec;
using System.Reflection;
using Atalasoft.Imaging;

namespace WpfAnnotations
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private Dictionary<string, Color> _colors = new Dictionary<string,Color>();
        private WpfVisualScaleMode _printScaleMode = WpfVisualScaleMode.ScaleToFit;
        private bool _mirroredTextEditing;
        private bool _rotatedTextEditing;
        private Atalasoft.Annotate.Pdf.PdfMarkupType _lastPdfMarkup = Atalasoft.Annotate.Pdf.PdfMarkupType.Highlight;
        private Button _pdfMarkupButton;
        private bool _pdfReadSupport = false;

        static Window1()
        {
            AtalaDemos.HelperMethods.PopulateDecoders(RegisteredDecoders.Decoders);
        }

        public Window1()
        {
            InitializeComponent();

            FillColorCombos();

            this.AnnotationViewer.Annotations.HotSpotClicked += new EventHandler<EventArgs<WpfAnnotationUI>>(Annotations_HotSpotClicked);
            this.AnnotationViewer.Annotations.SelectedAnnotationsChanged += new EventHandler(Annotations_SelectedAnnotationsChanged);
            this.AnnotationViewer.ImageViewer.IsCentered = true;

            this.AnnotationViewer.Annotations.Factories.Add(new WpfAnnotationUIFactory<TriangleAnnotation, TriangleData>());

            PrepareToolbar();

            try
            {
                // This requires a PDF Reader license.
                Atalasoft.Imaging.Codec.Pdf.PdfDecoder decoder = new Atalasoft.Imaging.Codec.Pdf.PdfDecoder();
                decoder.RenderSettings.AnnotationSettings = Atalasoft.Imaging.Codec.Pdf.AnnotationRenderSettings.None;
                Atalasoft.Imaging.Codec.RegisteredDecoders.Decoders.Add(decoder);
                _pdfReadSupport = true;
            }
            catch
            {
            }
        }

        #region Toolbar

        private void PrepareToolbar()
        {
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Callout", AnnotateIcon.Callout));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Ellipse", AnnotateIcon.Ellipse));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Embedded Image", AnnotateIcon.EmbeddedImage));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Freehand", AnnotateIcon.Freehand));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Hot Spot", AnnotateIcon.RectangleHotspot));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Hot Spot Freehand", AnnotateIcon.FreehandHotspot));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Line", AnnotateIcon.Line));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Lines", AnnotateIcon.Lines));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Polygon", AnnotateIcon.Polygon));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Rectangle", AnnotateIcon.Rectangle));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Referenced Image", AnnotateIcon.ReferencedImage));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Rubber Stamp", AnnotateIcon.RubberStamp));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("Text", AnnotateIcon.Text));
            this.AnnotationToolbar.Items.Add(CreateToolbarButton("PDF Line", AnnotateIcon.PdfLine));
            _pdfMarkupButton = CreateToolbarButton("PDF Markup (Highlight)", AnnotateIcon.PdfMarkup);
            this.AnnotationToolbar.Items.Add(_pdfMarkupButton);
        }

        private Button CreateToolbarButton(string tooltip, AnnotateIcon icon)
        {
            Image img = new Image();
            img.Source = ExtractAnnotationIcon(icon, AnnotateIconSize.Size24);

            Button btn = new Button();
            btn.Content = img;
            btn.ToolTip = tooltip;
            btn.Click += new RoutedEventHandler(AnnotationToolbar_Click);
            
            return btn;
        }

        void AnnotationToolbar_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            switch ((string)btn.ToolTip)
            {
                case "Callout":
                    AddCallout();
                    break;
                case "Ellipse":
                    AddEllipse();
                    break;
                case "Embedded Image":
                    AddEmbeddedImage();
                    break;
                case "Freehand":
                    AddFreehand();
                    break;
                case "Hot Spot":
                    AddHotSpot();
                    break;
                case "Hot Spot Freehand":
                    AddHotSpotFreehand();
                    break;
                case "Line":
                    AddLine();
                    break;
                case "Lines":
                    AddLines();
                    break;
                case "Polygon":
                    AddPolygon();
                    break;
                case "Rectangle":
                    AddRectangle();
                    break;
                case "Referenced Image":
                    AddReferencedImage();
                    break;
                case "Rubber Stamp":
                    AddRubberStamp();
                    break;
                case "Text":
                    AddText();
                    break;
                case "PDF Line":
                    AddPdfLine();
                    break;
                case "PDF Markup (Highlight)":
                case "PDF Markup (StrikeOut)":
                case "PDF Markup (Underline)":
                case "PDF Markup (Squiggly)":
                    AddPdfMarkup(_lastPdfMarkup);
                    break;
            }
        }

        private BitmapSource ExtractAnnotationIcon(AnnotateIcon icon, AnnotateIconSize size)
        {
            BitmapSource returnSource = WpfObjectConverter.ConvertBitmap((System.Drawing.Bitmap)IconResource.ExtractAnnotationIcon(icon, size));

            if (returnSource == null)
            {
                Assembly assm = Assembly.LoadFrom(@"Atalasoft.dotImage.dll");
                if (assm != null)
                {
                    System.IO.Stream stream = assm.GetManifestResourceStream("Atalasoft.Imaging.Annotate.Icons._" + size.ToString().Substring(4) + "." + icon.ToString() + ".png");
                    returnSource = WpfObjectConverter.ConvertBitmap((System.Drawing.Bitmap)System.Drawing.Image.FromStream(stream));
                }
                // if it's STILL null, then give up and make placeholders
                if (returnSource == null)
                {
                    switch(size.ToString()) 
                    {
                        case "size16":
                            returnSource = WpfObjectConverter.ConvertBitmap(new AtalaImage(16, 16, Atalasoft.Imaging.PixelFormat.Pixel24bppBgr, System.Drawing.Color.White).ToBitmap());
                            break;
                        case "size24":
                            returnSource = WpfObjectConverter.ConvertBitmap(new AtalaImage(24, 24, Atalasoft.Imaging.PixelFormat.Pixel24bppBgr, System.Drawing.Color.White).ToBitmap());
                            break;
                        case "size32":
                            returnSource = WpfObjectConverter.ConvertBitmap(new AtalaImage(32, 32, Atalasoft.Imaging.PixelFormat.Pixel24bppBgr, System.Drawing.Color.White).ToBitmap());
                            break;
                    }
                }
            }

            return returnSource;
        }

        #endregion

        #region Color Combobox

        private void FillColorCombos()
        {
            _colors.Add("AliceBlue", Colors.AliceBlue);
            _colors.Add("Black", Colors.Black);
            _colors.Add("Blue", Colors.Blue);
            _colors.Add("DarkBlue", Colors.DarkBlue);
            _colors.Add("Green", Colors.Green);
            _colors.Add("Lime", Colors.Lime);
            _colors.Add("Maroon", Colors.Maroon);
            _colors.Add("Orange", Colors.Orange);
            _colors.Add("Red", Colors.Red);
            _colors.Add("Transparent", Colors.Transparent);
            _colors.Add("White", Colors.White);
            _colors.Add("Yellow", Colors.Yellow);

            this.FillComboBox.ItemsSource = _colors;
            this.OutlineComboBox.ItemsSource = _colors;
        }

        private void SelectAddComboBoxItem(ComboBox comboBox, Color color)
        {
            if (!_colors.ContainsValue(color))
                _colors.Add(color.ToString(), color);

            int count = comboBox.Items.Count;
            for (int i = 0; i < count; i++)
            {
                KeyValuePair<string, Color> item = (KeyValuePair<string, Color>)comboBox.Items[i];
                if (item.Value == color)
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        #endregion

        #region Annotation Events

        void Annotations_SelectedAnnotationsChanged(object sender, EventArgs e)
        {
            this.statusInfo.Content = "Selection Changed: {Count=" + this.AnnotationViewer.Annotations.SelectedAnnotations.Count.ToString() + "}";
            
            // Update the property fields.
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            
            bool enabled = (ann != null);
            this.textLocation.IsEnabled = enabled;
            this.textSize.IsEnabled = enabled;
            this.FillComboBox.IsEnabled = enabled;
            this.OutlineComboBox.IsEnabled = enabled;
            this.PenSizeSlider.IsEnabled = enabled;
            this.ShadowCheckBox.IsEnabled = enabled;
            this.OffsetSlider.IsEnabled = enabled;

            if (enabled)
            {
                this.textLocation.Text = ann.Location.X.ToString() + ", " + ann.Location.Y.ToString();
                this.textSize.Text = ann.Size.Width.ToString() + ", " + ann.Size.Height.ToString();

                AnnotationBrush b = (AnnotationBrush)ann.GetValue(WpfAnnotationUI.FillProperty);
                if (b != null) SelectAddComboBoxItem(this.FillComboBox, WpfObjectConverter.ConvertColor(b.Color));

                AnnotationPen p = (AnnotationPen)ann.GetValue(WpfAnnotationUI.OutlineProperty);
                if (p != null)
                {
                    SelectAddComboBoxItem(this.OutlineComboBox, WpfObjectConverter.ConvertColor(p.Color));
                    this.PenSizeSlider.Value = p.Width;
                }

                b = (AnnotationBrush)ann.GetValue(WpfAnnotationUI.ShadowProperty);
                this.ShadowCheckBox.IsChecked = (b != null);

                if (b != null)
                {
                    Point pt = (Point)ann.GetValue(WpfAnnotationUI.ShadowOffsetProperty);
                    this.OffsetSlider.Value = pt.X;
                }
            }
        }

        void Annotations_HotSpotClicked(object sender, EventArgs<WpfAnnotationUI> e)
        {
            this.statusInfo.Content = "Hot Spot clicked!";
        }

        #endregion

        #region File Menu

        private void FileCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RoutedCommand rc = e.Command as RoutedCommand;
            if (rc == null) return;

            switch (rc.Name)
            {
                case "Open":
                    OpenFileDialog dlg = new OpenFileDialog();
                    dlg.Filter = AtalaDemos.HelperMethods.CreateDialogFilter(true);

                    if (dlg.ShowDialog().Value)
                        this.AnnotationViewer.Open(dlg.FileName, 0, null);
                    break;
                case "SaveAs":
                    SaveFileDialog save = new SaveFileDialog();
                    save.Filter = "JPEG (*.jpg)|*.jpg|TIFF (*.tif)|*.tif|PDF (*.pdf)|*.pdf";
                    if (save.ShowDialog().Value)
                    {
                        if (save.FilterIndex == 1)
                            SaveImage(save.FileName, new Atalasoft.Imaging.Codec.JpegEncoder());
                        else if (save.FilterIndex == 2)
                            SaveImage(save.FileName, new Atalasoft.Imaging.Codec.TiffEncoder());
                        else
                        {
                            if (Atalasoft.Imaging.AtalaImage.Edition == Atalasoft.Imaging.LicenseEdition.Document)
                            {
                                SaveImage(save.FileName, new Atalasoft.Imaging.Codec.Pdf.PdfEncoder());
                                SavePdfAnnotations(save.FileName);
                            }
                            else
                                MessageBox.Show("A 'DotImage Document Imaging' license is required to save as PDF.", "License Required", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    break;
                case "Print":
                    PrintDialog pdlg = new PrintDialog();
                    if (pdlg.ShowDialog().Value)
                    {
                        System.Printing.PageMediaSize sz = pdlg.PrintTicket.PageMediaSize;
                        pdlg.PrintVisual(this.AnnotationViewer.CreateVisual(new Size(sz.Width.HasValue ? sz.Width.Value : 0, sz.Height.HasValue ? sz.Height.Value : 0), _printScaleMode, new Thickness(10)), "WPF Annotation Printing");
                    }
                    break;
            }
        }

        private void FileCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            RoutedCommand rc = e.Command as RoutedCommand;
            if (rc == null) return;

            switch (rc.Name)
            {
                case "Open":
                    e.CanExecute = true;
                    break;
                case "SaveAs":
                    e.CanExecute = (this.AnnotationViewer.ImageViewer.Image != null);
                    break;
                case "Print":
                    // You can print the annotations without an image.
                    e.CanExecute = (this.AnnotationViewer.ImageViewer.Image != null || this.AnnotationViewer.Annotations.CountAnnotations() > 0);
                    break;
            }
        }

        private void SaveImage(string fileName, Atalasoft.Imaging.Codec.ImageEncoder encoder)
        {
            this.AnnotationViewer.Save(fileName, encoder, null);
        }

        private void SavePdfAnnotations(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                Atalasoft.Annotate.Exporters.PdfAnnotationDataExporter exp = new Atalasoft.Annotate.Exporters.PdfAnnotationDataExporter();

                System.Drawing.Size sz = this.AnnotationViewer.ImageViewer.Image.Size;
                System.Drawing.SizeF pageSize = new System.Drawing.SizeF(sz.Width, sz.Height);
                Atalasoft.Imaging.Dpi resolution = this.AnnotationViewer.ImageViewer.Image.Resolution;
                Atalasoft.Annotate.LayerData data = (Atalasoft.Annotate.LayerData)this.AnnotationViewer.Annotations.CurrentLayer.CreateDataSnapshot();

                exp.ExportOver(fs, pageSize, AnnotationUnit.Pixel, resolution, data, 0);
            }
        }

        private void AutoLoadXmp_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.AutoLoadXmp = ((MenuItem)sender).IsChecked;
        }

        private void AutoLoadWang_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.AutoLoadWang = ((MenuItem)sender).IsChecked;
        }

        private void AutoSaveXmp_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.AutoSaveXmp = ((MenuItem)sender).IsChecked;
        }

        private void AutoSaveWang_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.AutoSaveWang = ((MenuItem)sender).IsChecked;
        }

        private void Burn_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.Burn();
            this.AnnotationViewer.Annotations.Layers.Clear();
        }

        private void PrintScaleMode_Click(object sender, RoutedEventArgs e)
        {
            this.PrintScaleModeFill.IsChecked = false;
            this.PrintScaleModeFit.IsChecked = false;
            this.PrintScaleModeNone.IsChecked = false;
            this.PrintScaleModeStretch.IsChecked = false;
            
            MenuItem item = sender as MenuItem;
            item.IsChecked = true;
            
            string header = (string)item.Header;
            switch (header)
            {
                case "None":
                    _printScaleMode = WpfVisualScaleMode.None;
                    break;
                case "Scale To Fit":
                    _printScaleMode = WpfVisualScaleMode.ScaleToFit;
                    break;
                case "Scale To Fill":
                    _printScaleMode = WpfVisualScaleMode.ScaleToFill;
                    break;
                case "Stretch To Fill":
                    _printScaleMode = WpfVisualScaleMode.StretchToFill;
                    break;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Edit Menu

        private void EditCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            RoutedCommand rc = e.Command as RoutedCommand;
            if (rc == null) return;

            switch (rc.Name)
            {
                case "Cut":
                    this.AnnotationViewer.Annotations.Cut();
                    break;
                case "Copy":
                    this.AnnotationViewer.Annotations.Copy();
                    break;
                case "Paste":
                    this.AnnotationViewer.Annotations.Paste();
                    break;
            }
        }

        private void EditCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            RoutedCommand rc = e.Command as RoutedCommand;
            if (rc == null) return;

            switch (rc.Name)
            {
                case "Cut":
                case "Copy":
                    e.CanExecute = (this.AnnotationViewer.Annotations.SelectedAnnotations.Count > 0);
                    break;
                case "Paste":
                    e.CanExecute = this.AnnotationViewer.Annotations.CanPaste;
                    break;
            }
        }

        private void RotateDocument_Click(object sender, RoutedEventArgs e)
        {
            switch ((string)((MenuItem)sender).Header)
            {
                case "90":
                    this.AnnotationViewer.RotateDocument(DocumentRotation.Rotate90);
                    break;
                case "180":
                    this.AnnotationViewer.RotateDocument(DocumentRotation.Rotate180);
                    break;
                case "270":
                    this.AnnotationViewer.RotateDocument(DocumentRotation.Rotate270);
                    break;
            }
        }

        #endregion

        #region Viewer Menu

        private void CenterImage_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.ImageViewer.IsCentered = ((MenuItem)sender).IsChecked;
        }

        private void SnapRotation_Click(object sender, RoutedEventArgs e)
        {
            // Make it easy for end-users to hit a 45 degree angle when rotating.
            // Setting the RotationSnapInterval to 0 will disable this feature.
            this.AnnotationViewer.Annotations.RotationSnapInterval = (((MenuItem)sender).IsChecked ? 45 : 0);
            this.AnnotationViewer.Annotations.RotationSnapThreshold = 6;
        }

        private void Zoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.zoomCombo.Text.Length == 0) return;

            ComboBoxItem item = (ComboBoxItem)e.AddedItems[0];
            double val = Int32.Parse(((string)item.Content).Replace("%", "")) / 100.0;
            this.AnnotationViewer.ImageViewer.Zoom = val;
        }

        #endregion

        #region Annotations Menu

        private void LoadAnnotationData_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "XMP Data (*.xml)|*.xml|WANG Data (*.wng)|*.wng";
            if (dlg.ShowDialog().Value)
            {
                if (dlg.FilterIndex == 1)
                    this.AnnotationViewer.Annotations.Load(dlg.FileName, new Atalasoft.Annotate.Formatters.XmpFormatter());
                else
                    this.AnnotationViewer.Annotations.Load(dlg.FileName, new Atalasoft.Annotate.Formatters.WangFormatter());
            }
        }

        private void SaveAnnotationData_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "XMP Data (*.xml)|*.xml|WANG Data (*.wng)|*.wng";
            if (dlg.ShowDialog().Value)
            {
                if (dlg.FilterIndex == 1)
                    this.AnnotationViewer.Annotations.Save(dlg.FileName, new Atalasoft.Annotate.Formatters.XmpFormatter());
                else
                    this.AnnotationViewer.Annotations.Save(dlg.FileName, new Atalasoft.Annotate.Formatters.WangFormatter());
            }
        }

        #region Add Annotations

        private void AddAnnotation_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item == null) return;

            switch ((string)item.Header)
            {
                case "Callout":
                    AddCallout();
                    break;
                case "Ellipse":
                    AddEllipse();
                    break;
                case "Embedded Image":
                    AddEmbeddedImage();
                    break;
                case "Freehand":
                    AddFreehand();
                    break;
                case "Hot Spot":
                    AddHotSpot();
                    break;
                case "Hot Spot Freehand":
                    AddHotSpotFreehand();
                    break;
                case "Line":
                    AddLine();
                    break;
                case "Lines":
                    AddLines();
                    break;
                case "Polygon":
                    AddPolygon();
                    break;
                case "Rectangle":
                    AddRectangle();
                    break;
                case "Referenced Image":
                    AddReferencedImage();
                    break;
                case "Rubber Stamp":
                    AddRubberStamp();
                    break;
                case "Text":
                    AddText();
                    break;
                case "PDF Line":
                    AddPdfLine();
                    break;
                case "PDF Highlight":
                    AddPdfMarkup(Atalasoft.Annotate.Pdf.PdfMarkupType.Highlight);
                    break;
                case "PDF StrikeOut":
                    AddPdfMarkup(Atalasoft.Annotate.Pdf.PdfMarkupType.StrikeOut);
                    break;
                case "PDF Underline":
                    AddPdfMarkup(Atalasoft.Annotate.Pdf.PdfMarkupType.Underline);
                    break;
                case "PDF Squiggly":
                    AddPdfMarkup(Atalasoft.Annotate.Pdf.PdfMarkupType.Squiggly);
                    break;
                case "Triangle (Custom)":
                    this.AnnotationViewer.Annotations.CreateAnnotation(new TriangleAnnotation(null, new AnnotationBrush(System.Drawing.Color.Orange), new AnnotationPen(System.Drawing.Color.Gold, 1)));
                    break;
            }
        }

        private void AddPdfMarkup(Atalasoft.Annotate.Pdf.PdfMarkupType markupType)
        {
            _lastPdfMarkup = markupType;
            _pdfMarkupButton.ToolTip = "PDF Markup (" + markupType.ToString() + ")";

            if (markupType == Atalasoft.Annotate.Pdf.PdfMarkupType.Highlight)
                this.AnnotationViewer.Annotations.CreateAnnotation(new Atalasoft.Annotate.Wpf.Pdf.WpfPdfMarkupAnnotation(null, markupType, new AnnotationBrush(System.Drawing.Color.FromArgb(120, System.Drawing.Color.Yellow)), null, "", System.Environment.UserName, DateTime.Now));
            else
                this.AnnotationViewer.Annotations.CreateAnnotation(new Atalasoft.Annotate.Wpf.Pdf.WpfPdfMarkupAnnotation(null, markupType, null, new AnnotationPen(System.Drawing.Color.Black, 1), "", System.Environment.UserName, DateTime.Now));
        }

        private void AddPdfLine()
        {
            this.AnnotationViewer.Annotations.CreateAnnotation(new Atalasoft.Annotate.Wpf.Pdf.WpfPdfLineAnnotation(new Point(), new Point(), true, null, new AnnotationPen(System.Drawing.Color.Black), "PDF Line", System.Environment.UserName, DateTime.Now));
        }

        private void AddCallout()
        {
            WpfCalloutAnnotation ann = new WpfCalloutAnnotation("Callout", new AnnotationFont("Verdana", (float)(10)), new AnnotationBrush(System.Drawing.Color.White), new AnnotationPen(System.Drawing.Color.Black, 1), 20, new AnnotationBrush(System.Drawing.Color.Navy), new AnnotationPen(System.Drawing.Color.Black, 1));
            ann.CanEditMirrored = _mirroredTextEditing;
            ann.CanEditRotated = _rotatedTextEditing;
            ann.Leader.EndCap = new AnnotationLineCap(AnnotationLineCapStyle.FilledArrow, new System.Drawing.SizeF(12, 12));
            this.AnnotationViewer.Annotations.CreateAnnotation(ann);
        }

        private void AddEllipse()
        {
            WpfEllipseAnnotation ann = new WpfEllipseAnnotation(new Rect(0, 0, 0, 0), new Atalasoft.Annotate.AnnotationBrush(System.Drawing.Color.Green), new Atalasoft.Annotate.AnnotationPen(System.Drawing.Color.Gold, 1));
            this.AnnotationViewer.Annotations.CreateAnnotation(ann);
        }

        private void AddEmbeddedImage()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Images|*.jpg;*.png;*.tif";
            if (dlg.ShowDialog().Value)
            {
                AnnotationImage image = new AnnotationImage(dlg.FileName);
                WpfEmbeddedImageAnnotation ann = new WpfEmbeddedImageAnnotation(image, new Point(0, 0), null, new Point(0, 0));
                this.AnnotationViewer.Annotations.CreateAnnotation(ann);
            }
        }

        private void AddFreehand()
        {
            WpfFreehandAnnotation ann = new WpfFreehandAnnotation(new AnnotationPen(System.Drawing.Color.Red, 1), WpfFreehandLineType.Bezier, false);
            ann.GripMode = AnnotationGripMode.Rectangular;
            this.AnnotationViewer.Annotations.CreateAnnotation(ann);
        }

        private void AddHotSpot()
        {
            this.AnnotationViewer.Annotations.CreateAnnotation(new WpfHotSpotAnnotation(new AnnotationBrush(System.Drawing.Color.Firebrick), null));
        }

        private void AddHotSpotFreehand()
        {
            this.AnnotationViewer.Annotations.CreateAnnotation(new WpfHotSpotFreehandAnnotation(new AnnotationBrush(System.Drawing.Color.Firebrick), WpfFreehandLineType.Bezier));
        }

        private void AddLine()
        {
            WpfLineAnnotation line = new WpfLineAnnotation(new Point(0, 0), new Point(0, 0), new AnnotationPen(System.Drawing.Color.Red, 2));
            line.Outline.StartCap = new AnnotationLineCap(AnnotationLineCapStyle.Ellipse, new System.Drawing.SizeF(10, 10));
            line.Outline.EndCap = new AnnotationLineCap(AnnotationLineCapStyle.Ellipse, new System.Drawing.SizeF(10, 10));
            this.AnnotationViewer.Annotations.CreateAnnotation(line);
        }

        private void AddLines()
        {
            WpfLinesAnnotation ann = new WpfLinesAnnotation(null, new AnnotationPen(System.Drawing.Color.Red, 2));
            ann.Outline.StartCap = new AnnotationLineCap(AnnotationLineCapStyle.ReversedArrow, new System.Drawing.SizeF(10, 10));
            ann.Outline.EndCap = new AnnotationLineCap(AnnotationLineCapStyle.FilledArrow, new System.Drawing.SizeF(10, 10));
            this.AnnotationViewer.Annotations.CreateAnnotation(ann);
        }

        private void AddPolygon()
        {
            WpfPolygonAnnotation ann = new WpfPolygonAnnotation(null, new AnnotationBrush(System.Drawing.Color.Orange), new AnnotationPen(System.Drawing.Color.Black, 1), null, new Point(0, 0));
            this.AnnotationViewer.Annotations.CreateAnnotation(ann);
        }

        private void AddRectangle()
        {
            WpfRectangleAnnotation ann = new WpfRectangleAnnotation(new Rect(0, 0, 0, 0), new Atalasoft.Annotate.AnnotationBrush(System.Drawing.Color.Red), new Atalasoft.Annotate.AnnotationPen(System.Drawing.Color.Maroon, 1));
            this.AnnotationViewer.Annotations.CreateAnnotation(ann);
        }

        private void AddReferencedImage()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Images|*.jpg;*.png;*.tif";
            if (dlg.ShowDialog().Value)
            {
                WpfReferencedImageAnnotation ann = new WpfReferencedImageAnnotation(dlg.FileName, new Point(0, 0), null, new Point(0, 0));
                this.AnnotationViewer.Annotations.CreateAnnotation(ann);
            }
        }

        private void AddRubberStamp()
        {
            this.AnnotationViewer.Annotations.CreateAnnotation(new WpfRubberStampAnnotation("TOP SECRET", new AnnotationFont("Verdana", 24 * 1), new AnnotationBrush(System.Drawing.Color.Firebrick), null, new AnnotationPen(System.Drawing.Color.Firebrick, 10 * 1), 30, 4));
        }

        private void AddText()
        {
            WpfTextAnnotation txt = new WpfTextAnnotation("Testing", new Atalasoft.Annotate.AnnotationFont("Verdana", 12), new Atalasoft.Annotate.AnnotationBrush(System.Drawing.Color.Red), new Atalasoft.Annotate.AnnotationBrush(System.Drawing.Color.White), new Atalasoft.Annotate.AnnotationPen(System.Drawing.Color.Black, 1));
            txt.Shadow = new Atalasoft.Annotate.AnnotationBrush(System.Drawing.Color.FromArgb(120, System.Drawing.Color.Silver));
            txt.ShadowOffset = new Point(6, 6);
            txt.CanEditMirrored = _mirroredTextEditing;
            txt.CanEditRotated = _rotatedTextEditing;
            this.AnnotationViewer.Annotations.CreateAnnotation(txt);
        }

        #endregion

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.Annotations.Layers.Clear();
        }

        private void ClipToDocument_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.Annotations.ClipToDocument = ((MenuItem)sender).IsChecked;
        }

        private void FlipHorizontal_Click(object sender, RoutedEventArgs e)
        {
            FlipAnnotation(true, false);
        }

        private void FlipHorizontalPivot_Click(object sender, RoutedEventArgs e)
        {
            FlipAnnotation(true, true);
        }

        private void FlipVertical_Click(object sender, RoutedEventArgs e)
        {
            FlipAnnotation(false, false);
        }

        private void FlipVerticalPivot_Click(object sender, RoutedEventArgs e)
        {
            FlipAnnotation(false, true);
        }

        private void FlipAnnotation(bool horizontal, bool pivot)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann == null) return;

            ann.Mirror((horizontal ? MirrorDirection.Horizontal : MirrorDirection.Vertical), !pivot);
        }

        private void GripMode_Click(object sender, RoutedEventArgs e)
        {
            WpfPointBaseAnnotation ann = this.AnnotationViewer.Annotations.ActiveAnnotation as WpfPointBaseAnnotation;
            if (ann != null)
            {
                MenuItem item = sender as MenuItem;
                if ((string)item.Header == "Rectangular")
                    ann.GripMode = AnnotationGripMode.Rectangular;
                else
                    ann.GripMode = AnnotationGripMode.Points;
            }
        }

        private void InteractMode_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            switch ((string)item.Header)
            {
                case "None":
                    this.AnnotationViewer.Annotations.InteractMode = AnnotateInteractMode.None;
                    break;
                case "Author":
                    this.AnnotationViewer.Annotations.InteractMode = AnnotateInteractMode.Author;
                    break;
                case "View":
                    this.AnnotationViewer.Annotations.InteractMode = AnnotateInteractMode.View;
                    break;
            }
        }

        private void Group_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.Annotations.Group();
        }

        private void Ungroup_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.Annotations.Ungroup();
        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            if (this.AnnotationViewer.Annotations.ActiveAnnotation != null)
                this.AnnotationViewer.Annotations.ActiveAnnotation.Remove();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            this.AnnotationViewer.Annotations.SelectAll(true);
        }

        private void MirroredTextEditing_Click(object sender, RoutedEventArgs e)
        {
            _mirroredTextEditing = ((MenuItem)sender).IsChecked;
            UpdateTextAnnotationEditingOptions();
        }

        private void RotatedTextEditing_Click(object sender, RoutedEventArgs e)
        {
            _rotatedTextEditing = ((MenuItem)sender).IsChecked;
            UpdateTextAnnotationEditingOptions();
        }

        private void UpdateTextAnnotationEditingOptions()
        {
            foreach (WpfLayerAnnotation layer in this.AnnotationViewer.Annotations.Layers)
            {
                foreach (WpfAnnotationUI ann in layer.Items)
                {
                    WpfTextAnnotation txt = ann as WpfTextAnnotation;
                    if (txt != null)
                    {
                        txt.CanEditMirrored = _mirroredTextEditing;
                        txt.CanEditRotated = _rotatedTextEditing;
                        if (txt.EditMode)
                        {
                            txt.EditMode = false;
                            txt.EditMode = true;
                        }
                        continue;
                    }

                    WpfCalloutAnnotation call = ann as WpfCalloutAnnotation;
                    if (call != null)
                    {
                        call.CanEditMirrored = _mirroredTextEditing;
                        call.CanEditRotated = _rotatedTextEditing;
                        if (call.EditMode)
                        {
                            call.EditMode = false;
                            call.EditMode = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Annotation Property Box

        private void ProperyExpander_Expanded(object sender, RoutedEventArgs e)
        {
            this.ProperyExpander.Width = 185;
            // Add clipping so the grips will not render over the expander.
            this.AnnotationViewer.Annotations.GripClip = new RectangleGeometry(new Rect(160, 0, this.AnnotationViewer.Width - 160, this.AnnotationViewer.Height));
        }

        private void ProperyExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            this.ProperyExpander.Width = 24;
            this.AnnotationViewer.Annotations.GripClip = new RectangleGeometry(new Rect(0, 0, this.AnnotationViewer.Width, this.AnnotationViewer.Height));
        }

        private void textLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann == null) return;

            string txt = this.textLocation.Text;
            if (txt.Length == 0) return;

            string[] items = txt.Split(',');
            if (items.Length != 2) return;

            double x = 0;
            double y = 0;
            if (double.TryParse(items[0].Trim(), out x) && double.TryParse(items[1].Trim(), out y))
                ann.Location = new Point(x, y);
        }

        private void textSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann == null) return;

            string txt = this.textSize.Text;
            if (txt.Length == 0) return;

            string[] items = txt.Split(',');
            if (items.Length != 2) return;

            double width = 0;
            double height = 0;
            if (double.TryParse(items[0].Trim(), out width) && double.TryParse(items[1].Trim(), out height))
                ann.Size = new Size(width, height);
        }

        private void PenSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann == null) return;

            if (this.PenSizeSlider.Value == 0)
                ann.SetValue(WpfAnnotationUI.OutlineProperty, null);
            else
            {
                AnnotationPen pen = (AnnotationPen)ann.GetValue(WpfAnnotationUI.OutlineProperty);
                if (pen == null)
                {
                    Color clr = Colors.Black;
                    if (this.OutlineComboBox.SelectedIndex > -1)
                        clr = ((KeyValuePair<string, Color>)this.OutlineComboBox.SelectedItem).Value;

                    pen = new AnnotationPen(WpfObjectConverter.ConvertColor(clr), (float)this.PenSizeSlider.Value);
                    ann.SetValue(WpfAnnotationUI.OutlineProperty, pen);
                }
                else
                    pen.Width = (float)this.PenSizeSlider.Value;
            }
        }

        private void OutlineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann == null) return;

            KeyValuePair<string, Color> val = (KeyValuePair<string, Color>)this.OutlineComboBox.SelectedItem;
            
            AnnotationPen pen = (AnnotationPen)ann.GetValue(WpfAnnotationUI.OutlineProperty);
            if (pen == null)
            {
                if (this.PenSizeSlider.Value > 0)
                {
                    pen = new AnnotationPen(WpfObjectConverter.ConvertColor(val.Value), (float)this.PenSizeSlider.Value);
                    ann.SetValue(WpfAnnotationUI.OutlineProperty, pen);
                }
            }
            else
                pen.Color = WpfObjectConverter.ConvertColor(val.Value);
        }

        private void FillComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann == null) return;

            KeyValuePair<string, Color> val = (KeyValuePair<string, Color>)this.FillComboBox.SelectedItem;

            AnnotationBrush brush = (AnnotationBrush)ann.GetValue(WpfAnnotationUI.FillProperty);
            if (brush == null)
            {
                brush = new AnnotationBrush(WpfObjectConverter.ConvertColor(val.Value));
                ann.SetValue(WpfAnnotationUI.FillProperty, brush);
            }
            else
                brush.Color = WpfObjectConverter.ConvertColor(val.Value);
        }

        private void OffsetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann != null)
                ann.SetValue(WpfAnnotationUI.ShadowOffsetProperty, new Point(e.NewValue, e.NewValue));
        }

        private void ShadowCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann != null)
                ann.SetValue(WpfAnnotationUI.ShadowProperty, new AnnotationBrush(System.Drawing.Color.FromArgb(120, System.Drawing.Color.Silver)));
        }

        private void ShadowCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            WpfAnnotationUI ann = this.AnnotationViewer.Annotations.ActiveAnnotation;
            if (ann != null)
                ann.SetValue(WpfAnnotationUI.ShadowProperty, null);
        }

        #endregion

        #region Multi-Select Annotations

        // Use the left CTRL key for multi-select.
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!e.IsRepeat)
                this.AnnotationViewer.Annotations.SelectionMode = (e.Key == Key.LeftCtrl ? WpfAnnotationSelectionMode.Multiple : WpfAnnotationSelectionMode.Single);
            
            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            this.AnnotationViewer.Annotations.SelectionMode = WpfAnnotationSelectionMode.Single;
            base.OnPreviewKeyUp(e);
        }

        #endregion

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.ProperyExpander.Height = e.NewSize.Height;
            this.AnnotationViewer.Height = e.NewSize.Height;
            this.AnnotationViewer.Width = e.NewSize.Width - 24;
        }

        private void HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            // Instantiate the dialog box
            About dlg = new About("About Atalasoft WPF Annotations Demo", "WPF Annotations Demo");
            dlg.Description = "This is a WPF version of our very popular and powerful DotAnnotate Demo.\r\n\r\n" +
                              "The source code should provide a good working example of how yo use our AtalaAnnotationViewer (the WPF version of our AnnotateViewer Winforms control) and our annotations in a WPF application.";
            dlg.Link = "www.atalasoft.com/Support/Sample-Applications";

            // Configure the dialog box
            dlg.Owner = this;

            // Open the dialog box modally 
            dlg.ShowDialog();

        }

    }
}
