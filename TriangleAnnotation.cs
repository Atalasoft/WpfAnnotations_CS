using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Runtime.Serialization;
using System.Windows.Media;
using Atalasoft.Annotate;
using Atalasoft.Annotate.Wpf.Renderer;
using Atalasoft.Annotate.Wpf;

namespace WpfAnnotations
{
    /// <summary>
    /// This data class is used only for serialization in WPF.
    /// </summary>
    [Serializable]
    public class TriangleData : PointBaseData
    {
        private AnnotationBrush _fill = WpfAnnotationUI.DefaultFill;
        private AnnotationPen _outline = WpfAnnotationUI.DefaultOutline;

        public TriangleData()
        {
        }

        public TriangleData(Point[] points, AnnotationBrush fill, AnnotationPen outline)
            : base(new PointFCollection(WpfObjectConverter.ConvertPointF(points)))
        {
            _fill = fill;
            _outline = outline;
        }

        public TriangleData(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _fill = (AnnotationBrush)info.GetValue("Fill", typeof(AnnotationBrush));
            _outline = (AnnotationPen)info.GetValue("Outline", typeof(AnnotationPen));
        }

        public AnnotationBrush Fill
        {
            get { return _fill; }
            set { _fill = value; }
        }

        public AnnotationPen Outline
        {
            get { return _outline; }
            set { _outline = value; }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Fill", _fill);
            info.AddValue("Outline", _outline);
        }

        public override object Clone()
        {
            TriangleData data = new TriangleData();
            base.CloneBaseData(data);

            if (_fill != null) data._fill = _fill.Clone();
            if (_outline != null) data._outline = _outline.Clone();

            return data;
        }
    }

    /// <summary>
    /// This class is the actual annotation that will be used in WPF.
    /// </summary>
    /// <remarks>
    /// Don't forget to add a UI factory for this annotation. An example would be:
    /// this.AnnotationViewer.Annotations.Factories.Add(new WpfAnnotationUIFactory<TriangleAnnotation, TriangleData>());
    /// </remarks>
    public class TriangleAnnotation : WpfPointBaseAnnotation
    {
        static TriangleAnnotation()
        {
            WpfAnnotationRenderers.Add(typeof(TriangleAnnotation), new TriangleAnnotationRenderingEngine());
        }

        public TriangleAnnotation()
            : this(null, WpfAnnotationUI.DefaultFill, WpfAnnotationUI.DefaultOutline)
        {
        }

        public TriangleAnnotation(Point[] points, AnnotationBrush fill, AnnotationPen outline)
            : base(3, points)
        {
            // WpfAnnotationUI already has dependency properties for fill and outline.
            this.SetValue(FillProperty, fill);
            this.SetValue(OutlineProperty, outline);
        }

        public TriangleAnnotation(TriangleData data)
            : base(3, data)
        {
            this.SetValue(FillProperty, data.Fill);
            this.SetValue(OutlineProperty, data.Outline);
        }

        public AnnotationBrush Fill
        {
            get { return (AnnotationBrush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public AnnotationPen Outline
        {
            get { return (AnnotationPen)GetValue(OutlineProperty); }
            set { SetValue(OutlineProperty, value); }
        }

        public override bool SupportsMultiClickCreation
        {
            get { return true; }
        }

        // This is used for rendering and hit testing.
        public override Geometry Geometry
        {
            get
            {
                return WpfPointBaseAnnotation.GeometryFromPoints(this.Points.ToArray(), WpfFreehandLineType.Straight, true, (this.Fill != null));
            }
        }

        protected override WpfAnnotationUI CloneOverride(WpfAnnotationUI clone)
        {
            TriangleAnnotation ann = clone as TriangleAnnotation;
            if (ann == null) return null;

            AnnotationBrush fill = this.Fill;
            ann.Fill = (fill == null ? null : fill.Clone());

            AnnotationPen outline = this.Outline;
            ann.Outline = (outline == null ? null : outline.Clone());

            if (this.Points.Count > 0)
                ann.Points.AddRange(this.Points.ToArray());

            ann.GripMode = this.GripMode;

            return ann;
        }

        // This is used when serializing the annotation to XMP.
        protected override AnnotationData CreateDataSnapshotOverride()
        {
            Point[] points = this.Points.ToArray();
            AnnotationBrush fill = this.Fill;
            AnnotationPen outline = this.Outline;

            if (fill != null) fill = fill.Clone();
            if (outline != null) outline = outline.Clone();

            return new TriangleData(points, fill, outline);
        }

    }

    public class TriangleAnnotationRenderingEngine : WpfAnnotationRenderingEngine<TriangleAnnotation>
    {
        public TriangleAnnotationRenderingEngine()
        {
        }

        protected override void OnRenderAnnotation(TriangleAnnotation annotation, WpfRenderEnvironment environment)
        {
            Pen pen = WpfObjectConverter.ConvertAnnotationPen(annotation.Outline);
            Brush brush = WpfObjectConverter.ConvertAnnotationBrush(annotation.Fill);
            environment.DrawingContext.DrawGeometry(brush, pen, annotation.Geometry);
        }
    }
}
