using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Windows;

namespace SanityTest
{


    /// <summary>
    /// Represents a tag that provides adornments to be displayed above or below lines of text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The starting point of the tag's span is used to position the tag. The rest of the span is ignored.
    /// </para>
    /// <para>
    /// The adornments used by these tags should never be shared across multiple tags (since WPF does not allow UIElements to be displayed in multiple visual trees).
    /// </para>
    /// <para>
    /// These tags should never be created by an <see cref="ITaggerProvider"/> since that could attempt to display the same adornment in multiple views. Only
    /// <see cref="IViewTaggerProvider"/> can be used to create taggers that return this type of tag.
    /// </para>
    /// </remarks>
    public class InterLineAdornmentTag : DependencyObject, ITag
    {
        private readonly InterLineAdornmentFactory _adornmentFactory;
        private readonly bool _isAboveLine;
        private readonly HorizontalPositioningMode _horizontalPositioningMode;
        private readonly AdornmentRemovedCallback _removalCallback;
        private bool _isAnimating = true;

        /// <summary>
        /// Initializes a new instance of a <see cref="InterLineAdornmentTag"/>.
        /// </summary>
        /// <param name="adornmentFactory">A factory to create the adornment for this tag.</param>
        /// <param name="isAboveLine">Whether the adornment is displayed at the top or the bottom of a line.</param>
        /// <param name="initialHeight">Initial height of the adornment.</param>
        /// <param name="horizontalPositioningMode">Specifies how the adornment is positioned horizontally on the line.</param>
        /// <param name="horizontalOffset">Horizontal offset of the adornment with respect to the location defined by <paramref name="horizontalPositioningMode"/>.</param>
        /// <param name="removalCallback">Called when adornment is removed from the view. May be null. May not be null if <paramref name="adornmentFactory"/> is null.</param>
        public InterLineAdornmentTag(InterLineAdornmentFactory adornmentFactory,
                                     bool isAboveLine,
                                     double initialHeight,
                                     HorizontalPositioningMode horizontalPositioningMode,
                                     double horizontalOffset,
                                     AdornmentRemovedCallback removalCallback = null)
        {
            if ((adornmentFactory == null) && (removalCallback != null))
                throw new ArgumentException("Either adornmentFactory must be non-null or removalCallback must be null.");
            if (initialHeight < 0.0)
                throw new ArgumentException("initialHeight must be >= 0.");

            _adornmentFactory = adornmentFactory;

            _isAboveLine = isAboveLine;
            this.Height = initialHeight;

            _horizontalPositioningMode = horizontalPositioningMode;
            this.HorizontalOffset = horizontalOffset;

            _removalCallback = removalCallback;
        }

        /// <summary>
        /// Gets the factory to create the adornment for this tag. It may be null.
        /// </summary>
        /// <remarks>
        /// <para>This factory will be used to create the adornment displayed by this tag. The factory will only be called once
        /// when the tag first becomes visible. If the factory is called then this.RemovedCallback (if non-null) will be called
        /// when the adornment is removed from the view (the tag will also be removed at this time).</para>
        /// <para>
        /// The arguments to the factory are (this, v, p) where v is the view in which the adornment will be displayed and
        /// p is the location of the adornment in the view's TextSnapshot.
        /// </para>
        /// </remarks>
        public InterLineAdornmentFactory AdornmentFactory { get { return _adornmentFactory; } }

        /// <summary>
        /// Indicated whether the adornment is displayed on top of or at the bottom of the line.
        /// </summary>
        public bool IsAboveLine { get { return _isAboveLine; } }

        /// <summary>
        /// The height of of the space created for the adornment.
        /// </summary>
        public double Height
        {
            get { return (double)this.GetValue(InterLineAdornmentTag.HeightProperty); }
            set { this.SetValue(InterLineAdornmentTag.HeightProperty, value); }
        }

        /// <summary>
        /// Raised whenever the Height property of this tag is changed.
        /// </summary>
        /// <remarks>
        /// This is raised automatically when this.Height changes.
        /// </remarks>
        public event EventHandler<DependencyPropertyChangedEventArgs> HeightChanged;

        /// <summary>
        /// Indicates whether or not the tag's height is currently being animated.
        /// </summary>
        /// <remarks>
        /// If true, which is the default, the containing view will automatically do a layout
        /// that preserves the location of the line containing the tag when the tag's Height
        /// property is changed. If false, no layout will be done automatically.
        /// </remarks>
        public bool IsAnimating
        {
            get { return _isAnimating; }
            set { _isAnimating = value; }
        }

        /// <summary>
        /// Specifies how the adornment is positioned horizontally on the line (offset by this.HorizontalOffset).
        /// </summary>
        public HorizontalPositioningMode HorizontalPositioningMode { get { return _horizontalPositioningMode; } }

        /// <summary>
        /// Horizontal offset of the adornment with respect to the location defined by this.HorizontalPositioningMode.
        /// </summary>
        public double HorizontalOffset
        {
            get { return (double)this.GetValue(InterLineAdornmentTag.HorizontalOffsetProperty); }
            set { this.SetValue(InterLineAdornmentTag.HorizontalOffsetProperty, value); }
        }

        /// <summary>
        /// Raised whenever the HorizontalOffset of this tag is changed.
        /// </summary>
        /// <remarks>
        /// This is raised automatically when this.HorizontalOffset changes.
        /// </remarks>
        public event EventHandler<DependencyPropertyChangedEventArgs> HorizontalOffsetChanged;

        /// <summary>
        /// Called when adornment is removed from the view. It may be null.
        /// </summary>
        /// <remarks>
        /// <para>This method is only called if the tag's AdormentFactory was called to create an adornment.</para>
        /// <para>The arguments to this call will be (this, adornment).</para>
        /// </remarks>
        public AdornmentRemovedCallback RemovalCallback { get { return _removalCallback; } }

        public static readonly DependencyProperty HeightProperty = DependencyProperty.Register("Height",
                                                                                               typeof(double),
                                                                                               typeof(InterLineAdornmentTag),
                                                                                               new PropertyMetadata(0.0, InterLineAdornmentTag.HeightChangedCallback));

        public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register("HorizontalOffset",
                                                                                                         typeof(double),
                                                                                                         typeof(InterLineAdornmentTag),
                                                                                                         new PropertyMetadata(0.0, InterLineAdornmentTag.HorizontalOffsetChangedCallback));

        private static void HeightChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tag = d as InterLineAdornmentTag;
            if (tag != null)
            {
                var tmp = tag.HeightChanged;
                if (tmp != null)
                {
                    tmp(d, e);
                }
            }
        }

        private static void HorizontalOffsetChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tag = d as InterLineAdornmentTag;
            if (tag != null)
            {
                var tmp = tag.HorizontalOffsetChanged;
                if (tmp != null)
                {
                    tmp(d, e);
                }
            }
        }
    }

    // This file contain internal APIs that are subject to change without notice.
    // Use at your own risk.
    //

    /// <summary>
    /// Factory used to create adornments used by for the InterLineAdornmentTags.
    /// </summary>
    /// <param name="tag">The tag for which the adornment is being created.</param>
    /// <param name="view">The view in which the adornment is being created.</param>
    /// <param name="position">The position in the view where the adornment is being created.</param>
    /// <returns>The newly created adornment.</returns>
    public delegate UIElement InterLineAdornmentFactory(InterLineAdornmentTag tag, IWpfTextView view, SnapshotPoint position);

    public delegate void AdornmentRemovedCallback(object tag, UIElement element);

    public enum HorizontalPositioningMode
    {
        /// <summary>
        /// Adornment is positioned with respect to the left edge of the character at the tag's position.
        /// </summary>
        TextRelative,

        /// <summary>
        /// Adornment is positioned with respect to the left edge of the viewport.
        /// </summary>
        ViewRelative,

        /// <summary>
        /// Adornment is positioned with respect to the left edge of the view.
        /// </summary>
        Absolute
    }
}
