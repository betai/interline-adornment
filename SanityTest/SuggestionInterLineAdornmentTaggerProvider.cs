//using Microsoft.VisualStudio.Editor.Internal;
//using Microsoft.VisualStudio.Language.Intellisense;
//using Microsoft.VisualStudio.Text;
//using Microsoft.VisualStudio.Text.Editor;
//using Microsoft.VisualStudio.Text.Classification;
//using Microsoft.VisualStudio.Text.Outlining;
//using Microsoft.VisualStudio.Text.Tagging;
//using Microsoft.VisualStudio.Utilities;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.Composition;
//using Microsoft.VisualStudio.Text.Adornments;
////using Microsoft.VisualStudio.Editor.Implementation.Find;
//using System.Windows.Media;
//using Microsoft.VisualStudio.Text.Operations;
//using Microsoft.VisualStudio.Editor;
//using System.Windows;

namespace SanityTest
{
    //[Export(typeof(IViewTaggerProvider))]
    //[ContentType("any")]
    //[TextViewRole(PredefinedTextViewRoles.Interactive)]
    //[TagType(typeof(InterLineAdornmentTag))]
    //[TagType(typeof(IOverviewMarkTag))]
    internal class SuggestionInterLineAdornmentTaggerProvider //: IViewTaggerProvider
    {
        //[ImportMany]
        //private List<Lazy<ISuggestionResultPresenter, IOrderable>> _suggestionResultPresenters;

        //[Import]
        //internal IVsEditorAdaptersFactoryService VsEditorAdaptersFactoryService;

        //private IList<Lazy<ISuggestionResultPresenter, IOrderable>> _orderedSuggestionResultPresenters;
        //public IList<Lazy<ISuggestionResultPresenter, IOrderable>> SuggestionResultPresenters
        //{
        //    get
        //    {
        //        return (_orderedSuggestionResultPresenters = _orderedSuggestionResultPresenters ?? Orderer.Order(_suggestionResultPresenters));
        //    }
        //}

        /*
                internal IViewTagAggregatorFactoryService TaggerAggregatorFactoryService { get; set; }

                public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
                {
                    if (textView.TextBuffer == buffer)
                    {
                        return SuggestionInterLineAdornmentTagger.GetorCreate(this, textView) as ITagger<T>;
                    }

                    return null;
                }
            }

        //    public interface ISuggestionResultDisplayInfo : IDisposable
        //    {
        //        /// <summary>
        //        /// Defines the localized label used for displaying this result to the user.
        //        /// This value will be used to represent <see cref="IPeekResult"/> in the Peek control's result list.
        //        /// </summary>
        //        string Label { get; }

        //        /// <summary>
        //        /// Defines the localized label tooltip used for displaying this result to the user.
        //        /// </summary>
        //        /// <remarks>
        //        /// Supported content types are strings and <see cref="UIElement" /> instances.
        //        /// </remarks>
        //        object LabelTooltip { get; }

        //        /// <summary>
        //        /// Defines the localized title used for displaying this result to the user.
        //        /// </summary>
        //        string Title { get; }

        //        /// <summary>
        //        /// Defines the localized title tooltip used for displaying this result to the user.
        //        /// </summary>
        //        /// // <remarks>
        //        /// Supported content types are strings and <see cref="UIElement" /> instances.
        //        /// </remarks>
        //        object TitleTooltip { get; }
        //    }

        //    public interface ISuggestionResult : IDisposable
        //    {
        //        /// <summary>
        //        /// Determines properties used for displaying this result to the user.
        //        /// </summary>
        //        ISuggestionResultDisplayInfo DisplayInfo { get; }

        //        /// <summary>
        //        /// Determines whether this result has a place to navigate to.
        //        /// </summary>
        //        /// <returns>true if can navigate, false otherwise.</returns>
        //        bool CanNavigateTo { get; }

        //        /// <summary>
        //        /// This function will be called directly after navigation completes (if navigation was successful).
        //        /// </summary>
        //        /// <remarks>
        //        /// Argument 1: this <see cref="IPeekResult"/>.
        //        /// Argument 2: data that this <see cref="IPeekResult"/> decides to pass in from
        //        /// the results of its navigation (e.g. a handle to a newly opened text view).
        //        /// Note: Argument 2 can be null if this <see cref="IPeekResult"/> has nothing to pass in.
        //        /// Argument 3: data that is set by the caller of <see cref="NavigateTo(object)"/>.
        //        /// </remarks>
        //        Action<ISuggestionResult, object, object> PostNavigationCallback { get; }

        //        /// <summary>
        //        /// Navigate to the location of this result. If the navigation is succesful, then the PostNavigationCallback
        //        /// will be called.
        //        /// </summary>
        //        /// <param name="data">
        //        /// The data that is to be passed directly into the third argument of <see cref="PostNavigationCallback"/>, 
        //        /// if navigation is successful.
        //        /// </param>
        //        void NavigateTo(object data);

        //        /// <summary>
        //        /// Occurs when an <see cref="IPeekResult"/> is disposed.
        //        /// </summary>
        //        event EventHandler Disposed;
        //    }

        //    public sealed class IsOverlayLayerAttribute : SingletonBaseMetadataAttribute
        //    {
        //        /// <summary>
        //        /// Indicates whether an <see cref="AdornmentLayerDefinition"/> defines an overlay adornment layer or not.
        //        /// </summary>
        //        public bool IsOverlayLayer { get; private set; }

        //        /// <summary>
        //        /// Creates new insatnce of the <see cref="IsOverlayLayerAttribute"/> class.
        //        /// </summary>
        //        /// <param name="isOverlayLayer">Sets whether the adornment layer is an overlay layer.</param>
        //        public IsOverlayLayerAttribute(bool isOverlayLayer)
        //        {
        //            this.IsOverlayLayer = isOverlayLayer;
        //        }
        //    }

        //    [Export(typeof(EditorFormatDefinition))]
        //    [Name(FindHighlightDefinition.Name)]
        //    [UserVisible(true)]
        //    class FindHighlightDefinition : MarkerFormatDefinition
        //    {
        //        public const string Name = "MarkerFormatDefinition/FindHighlight";

        //#pragma warning disable 169

        //        [Export]
        //        [Name(VsPredefinedAdornmentLayers.Find)]
        //        [Order(After = PredefinedAdornmentLayers.Caret)]
        //        [IsOverlayLayer(true)]
        //        private AdornmentLayerDefinition _adornmentLayerDefinition;

        //        [Export(typeof(ErrorTypeDefinition))]
        //        [Order(After = PredefinedErrorTypeNames.Warning, Before = SuggestionMarkDefinition.Name)]
        //        [Name(FindHighlightDefinition.Name)]
        //        internal static ErrorTypeDefinition findTypeDefinition;

        //#pragma warning restore 169

        //        FindHighlightDefinition()
        //        {
        //            this.ForegroundCustomizable = false;

        //            this.BackgroundColor = Color.FromArgb((byte)(ClassificationFormatDefinition.DefaultBackgroundOpacity * 255), 244, 167, 33);
        //            this.DisplayName = "FindMatchHighlightFontsAndColors--custom";
        //            this.ZOrder = 5;
        //        }
        //    }


        //    [Export(typeof(EditorFormatDefinition))]
        //    [Name(SuggestionMarkDefinition.Name)]
        //    [UserVisible(true)]
        //    class SuggestionMarkDefinition : MarkerFormatDefinition
        //    {
        //        public const string Name = "PeekFormatDefinition/PeekMark";

        //        SuggestionMarkDefinition()
        //        {
        //            this.ForegroundCustomizable = false;

        //            this.BackgroundColor = Color.FromArgb((byte)0xff, 51, 153, 255);
        //            this.DisplayName = "PeekMark--custom";
        //        }

        //        [Export(typeof(ErrorTypeDefinition))]
        //        [Order(After = FindHighlightDefinition.Name)] //, Before = EOIMarkDefinition.Name
        //        [Name(SuggestionMarkDefinition.Name)]
        //        internal static ErrorTypeDefinition eoiMarkTypeDefinition;
        //    }


        //    public interface ISuggestionResultPresenter
        //    {
        //        /// <summary>
        //        /// Creates <see cref="IPeekResultPresentation"/> instance for the given <see cref="IPeekResult"/>.
        //        /// </summary>
        //        /// <param name="result">The Peek result for which to create a visual representation.</param>
        //        /// <returns>A valid <see cref="IPeekResultPresentation"/> instance or null if none could be created by this presenter.</returns>
        //        ISuggestionResultPresentation TryCreatePeekResultPresentation(ISuggestionResult result);
        //    }

        //    public interface ISuggestionResultPresentation : IDisposable
        //    {
        //        /// <summary>
        //        /// Tries to open another <see cref="IPeekResult"/> while keeping the same presentation.
        //        /// For example document result presentation might check if <paramref name="otherResult"/>
        //        /// represents a result in the same document and would reuse already open document.
        //        /// </summary>
        //        /// <param name="otherResult">Another result to be opened.</param>
        //        ///<returns><c>true</c> if succeeded in opening <paramref name="otherResult"/>, <c>false</c> otherwise.</returns>
        //        bool TryOpen(ISuggestionResult otherResult);

        //        /// <summary>
        //        /// Prepare to close the presentation.
        //        /// </summary>
        //        /// <returns>Returns <c>true</c> if the presentation is allowed to close; <c>false</c> otherwise.</returns>
        //        /// <remarks>
        //        /// <para>
        //        /// This method is called with the presentation is explicitly being closed to give the user, if the presentation
        //        /// corresponds to a modified document, the opportunity to save the document if desired.
        //        /// </para>
        //        /// <para>
        //        /// If this method returns <c>true</c>, the caller must close the presentation (typically by dismissing the
        //        /// containing peek session).
        //        /// </para>
        //        /// </remarks>
        //        bool TryPrepareToClose();

        //        /// <summary>
        //        /// Creates WPF visual representation of the Peek result.
        //        /// </summary>
        //        /// <remarks>
        //        /// An <see cref="IPeekResultPresentation"/> for an <see cref="IDocumentPeekResult"/> would
        //        /// for example open document and return a WPF control of the IWpfTextViewHost.
        //        /// </remarks>
        //        /// <param name="session">The <see cref="IPeekSession"/> containing the Peek result.</param>
        //        /// <param name="scrollState">The state that defines the desired scroll state of the result. May be null (in which case the default scroll state is used).</param>
        //        /// <returns>A valid <see cref="UIElement"/> representing the Peek result.</returns>
        //        UIElement Create(ISuggestionSession session, ISuggestionResultScrollState scrollState);

        //        /// <summary>
        //        /// Scrolls open representation of the Peek result into view.
        //        /// </summary>
        //        /// <param name="scrollState">The state that defines the desired scroll state of the result. May be null (in which case the default scroll state is used).</param>
        //        void ScrollIntoView(ISuggestionResultScrollState scrollState);

        //        /// <summary>
        //        /// Captures any information about the result prior to navigating to another frame (by using the peek navigation history
        //        /// or doing a recursive peek).
        //        /// </summary>
        //        ISuggestionResultScrollState CaptureScrollState();

        //        /// <summary>
        //        /// Closes the represenation of the Peek result.
        //        /// </summary>
        //        /// <remarks>
        //        /// An <see cref="ISuggestionResultPresentation"/> for an <see cref="IDocumentSuggestionResult"/> would
        //        /// for example close the document in this method.
        //        /// </remarks>
        //        void Close();

        //        /// <summary>
        //        /// Raised when the content of the presentation needs to be recreated. 
        //        /// </summary>
        //        event EventHandler<RecreateContentEventArgs> RecreateContent;

        //        /// <summary>
        //        /// Sets keyboard focus to the open representation of the Peek result.
        //        /// </summary>
        //        void SetKeyboardFocus();

        //        /// <summary>
        //        /// The ZoomLevel factor associated with the presentation.
        //        /// </summary>
        //        /// <remarks>
        //        /// Represented as a percentage (100.0 == default).
        //        /// </remarks>
        //        double ZoomLevel { get; set; }

        //        /// <summary>
        //        /// Gets a value indicating whether or not this presentation is dirty.
        //        /// </summary>
        //        bool IsDirty { get; }

        //        /// <summary>
        //        /// Raised when <see cref="IsDirty"/> changes.
        //        /// </summary>
        //        event EventHandler IsDirtyChanged;

        //        /// <summary>
        //        /// Gets a value indicating whether or not this presentation is read-only.
        //        /// </summary>
        //        bool IsReadOnly { get; }

        //        /// <summary>
        //        /// Raised when <see cref="IsReadOnly"/> changes.
        //        /// </summary>
        //        event EventHandler IsReadOnlyChanged;

        //        /// <summary>
        //        /// Can this presentation be saved?
        //        /// </summary>
        //        /// <param name="defaultPath">Location the presentation will be saved to by default (will be null if returning false).</param>
        //        bool CanSave(out string defaultPath);

        //        /// <summary>
        //        /// Save the current version of this presentation.
        //        /// </summary>
        //        /// <param name="saveAs">If true, ask the user for a save location.</param>
        //        /// <returns>true if the save succeeded.</returns>
        //        bool TrySave(bool saveAs);
        //    }

        //    public interface ISuggestionResultScrollState : IDisposable
        //    {
        //        /// <summary>
        //        /// Restore the presentation to the captured state.
        //        /// </summary>
        //        /// <param name="presentation">Result Presentation to scroll.</param>
        //        /// <remarks>
        //        /// <paramref name="presentation"/> will always be the presentation
        //        /// that created this via presentation.CaptureScrollState().</remarks>
        //        void RestoreScrollState(ISuggestionResultPresentation presentation);
        //    }

        //    public class RecreateContentEventArgs : EventArgs
        //    {
        //        /// <summary>
        //        /// Gets whether the Peek result's content presented by <see cref="ISuggestionResultPresentation"/> was deleted.
        //        /// </summary>
        //        public bool IsResultContentDeleted { get; private set; }

        //        /// <summary>
        //        /// Creates new instance of the <see cref="RecreateContentEventArgs"/> class.
        //        /// </summary>
        //        /// <param name="isResultContentDeleted">Indicates whether the Suggestion result's content presented by <see cref="ISuggestionResultPresentation"/> was deleted.</param>
        //        public RecreateContentEventArgs(bool isResultContentDeleted = false)
        //        {
        //            IsResultContentDeleted = isResultContentDeleted;
        //        }
        //    }*/
    }
}

