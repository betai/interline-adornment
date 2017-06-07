using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace SanityTest
{
    //The inter-line adornment service is a mechanism for letting 3rd parties easily create adornments that go above or below a line of text,
    //inserting whitespace as needed. The overall implementation is similar to that of text marker and inter-space adornments but with a few
    //twists that are unique to the inter-line adornment service:
    //  We don’t want to move the text of the caret line when an adornment is placed above the line. 
    //    Reason: it gets annoying to have the text of the caret line jump around as adornments are added & removed when the user types.

    //  We don’t want to remove/replace adornments on lines that are modified but for which the adornment hasn’t changed.
    //    Reason: some of these adornments are big (c.f. peek adornment) so we don’t want to flash the screen by removing and then restoring
    //    the adornment (think remote desktop).

    //  The height of the adornment needs to be animatable.
    //    Reason: it makes it much more obvious that the peek is not covering code if you can see it get shifted away.

    //  There need to be different horizontal positioning modes.
    //    Reason: CodeLens wants adorments placed wrt the text, Peek wants them with respect to left edge of the viewport, Diff wants left edge of the view.

    //Performance is a concern here: we do not want to force additional layouts unless inter-line adornments are added or removed. We also need to
    //have good performance in scenarios where a huge file has tens of thousands of inter-line adornments. That said, we can assume that the number
    //of visible inter-line adornments will be fairly small (and, as it stands, performance is
    //  O(Number of lines in the view * (Number of visible inter-line adornments)^2).
    //It wouldn’t be hard to change the implementation later to improve performance if needed but doesn’t seem to be worth the additional complexity
    //at the moment.

    //The current implementation lets 3rd parties create inter-line adornments by defining an IViewTaggerProvider for interLineAdornmentTags.
    //This follows the pattern used by the TextMarkerAdornment and the interTextAdornments but there are a couple of things that are different
    //enough that a new approach might be appropriate. In particular:
    //    inter-line adornments are located at a point. Taggers returns spans. The current implementation simply uses the start of the span but
    //    this is still confusing. It shouldn’t be an issue if everyone providing interLineAdornmentTags associates them with a 0-length span.

    //    Having an ITaggerProvider for an interLineAdornmentTag is a bad idea: those taggers are created for buffers and are shared by the views
    //    that use the buffer (which would be bad since you could end up trying to display the same UIElement in different views). This isn’t an issue
    //    if you only define IViewTaggerProviders or are careful in your implementation of an ITaggerProvider.

    //    We call into the taggers part-way through a layout (when we are getting the line transforms for the new ITextViewLines). The view is
    //    in a slightly weird state here: it has advanced to the new text snapshot but hasn’t updated its TextViewLineCollection yet. This isn’t a
    //    problem as long as you are aware of the issue.

    //The interLineTextAdornment has several members but the two critical ones are:
    //    A factory for creating UIElements, called just before adding the adornment to the adornment layer.
    //    The height of the adornment and whether it is positioned above or below the line.
    //This approach is very efficient since there are (rare) cases where we need to get the tags for a line that is never actually displayed in the
    //view so you don’t want to create UIElements in those cases. The height of the adornment is a (WPF) DependencyProperty so it can be animated.

    //Implementation:
    //This service is mostly an implementation of an ILineTransformSource (and associated factory). It also hooks the view’s LayoutChanged event and
    //the taggers BatchTagsChanged event.

    //ILineTransformSource.GetLineTransform(ITextViewLine line, …) does:
    //    Checks to see if the view’s text snapshot has changed. If it has:
    //       All existing tags are translated to the new snapshot.
    //       We set the _delayChanges flag (which is cleared later in the LayoutChanged event handler).
    //    When _delayChanges is set, we ignore newly created tags and do not delete old or stale tags immediately so we don’t end up
    //    moving the text of the caret line.

    //    If the line has been reformatted or crossed a span invalidated by a tags change event,
    //        We get a list of the existing tags on the line.
    //        We get the new tags on the line from the tag aggregator.
    //        We check to see if any of the new tags are instances of the pre-existing tags and, if they are, we discard them immediately.
    //    Otherwise we add the tag and set its DelayTagCreation flag if _delayChanges is set.
    //    We delete or flag for deletion any of the old tags that didn’t have a new instance that was deleted.

    //The layout changed event:
    //    Removes any tag that are no longer on visible lines.
    //    If a tag is no longer on visible text (because of hidden regions), deletes or marks it for deletion.
    //    If we’re not delaying changes, it adds adornments for all tags that haven’t added their adornments yet.
    //    If there were changes that were delayed, it queues up another layout.

    //The batched tags changed event:
    //    Converts all the changed spans to a NormalizedSpanCollection (this is the list of invalidated spans used by GetLineTransform).
    //    Forces a layout to happen

    sealed class InterLineAdornmentManager : ILineTransformSource
    {
        private readonly IWpfTextView _view;
        private ITagAggregator<InterLineAdornmentTag> _tagAggregator;
        private readonly IAdornmentLayer _layer;

        private NormalizedSpanCollection _invalidatedSpans = null;
        internal bool _needsLayout;
        private bool _delayTagChanges;
        private ITextSnapshot _currentSnapshot;
        internal readonly List<ActiveTag> _tags = new List<ActiveTag>();

        public static InterLineAdornmentManager Create(IWpfTextView view, InterLineAdornmentManagerFactory factory)
        {
            return view.Properties.GetOrCreateSingletonProperty<InterLineAdornmentManager>(delegate
            {
                return new InterLineAdornmentManager(view, factory);
            });
        }

        public InterLineAdornmentManager(IWpfTextView view, InterLineAdornmentManagerFactory factory)
        {
            _view = view;

            _tagAggregator = factory.TagAggregatorFactoryService.CreateTagAggregator<InterLineAdornmentTag>(view, (TagAggregatorOptions)TagAggregatorOptions2.DeferTaggerCreation);
            _layer = view.GetAdornmentLayer(PredefinedAdornmentLayers.InterLine);

            _currentSnapshot = view.TextSnapshot;

            _tagAggregator.BatchedTagsChanged += this.OnBatchedTagsChanged;
            _view.LayoutChanged += this.OnLayoutChanged;
            _view.Closed += this.OnTextViewClosed;
        }

        #region ILineTransformSource Members
        public LineTransform GetLineTransform(ITextViewLine line, double yPosition, ViewRelativePosition placement)
        {
            //GetLineTransform is called before the LayoutChangedEvent so we need to detect (& remember) when the view's TextSnapshot changes.
            //
            //  If the text snapshot has changed then we want to get -- but not use -- the new tags. The rationale is that a changing text snapshot
            //  is probably the result of user edits and we do not want to create/remove adornments in this layout (we'll do it in a subsequent
            //  layout). We can't skip getting the new tags since we need to know if there are new tags so we can -- in OnLayoutChanged -- queue
            //  up a new layout.
            //
            //  If the text snapshot hasn't changed then we want to get and immediately use the new tags & remove the old ones.
            if (_currentSnapshot != line.Snapshot)
            {
                //The snapshot for a line only goes to higher version numbers so it should always be on a latter snapshot than _currentSnapshot.
                Debug.Assert(line.Snapshot.Version.VersionNumber > _currentSnapshot.Version.VersionNumber);

                //If _invalidatedSpans, then we are being called from OnBatchTagsChanged and the invalidated spans are on the correct snapshot.
                Debug.Assert(_invalidatedSpans == null);
                _delayTagChanges = true;

                //Migrate all the existing tags without deleting any to the new snapshot if we haven't done it yet.
                foreach (ActiveTag t in _tags)
                {
                    t.Position = Tracking.TrackPositionForwardInTime(PointTrackingMode.Negative, t.Position,
                                                                     _currentSnapshot.Version, line.Snapshot.Version);
                }
                _currentSnapshot = line.Snapshot;
            }

            //Find any tags on the line and get their space requirements (unless the tag was added inside a text change).
            NormalizedSnapshotSpanCollection visibleSpansOnLine = line.ExtentAsMappingSpan.GetSpans(_currentSnapshot);

            //Get the tags on the line if the line is new or reformatted.
            if ((line.Change == TextViewLineChange.NewOrReformatted) ||
                ((_invalidatedSpans != null) && _invalidatedSpans.IntersectsWith(line.Extent)))
            {
                //All tags on the old line are marked as NeedsToBeDeleted (we'll clear the flag later if we get
                //the tag back).
                foreach (var t in _tags)
                {
                    if (line.ContainsBufferPosition(new SnapshotPoint(_currentSnapshot, t.Position)))
                    {
                        t.NeedsToBeDeleted = true;
                    }
                }

                //Get the new tags on the line.
                foreach (var t in _tagAggregator.GetTags(visibleSpansOnLine))
                {
                    var spans = t.Span.GetSpans(line.Snapshot);
                    if (spans.Count > 0)
                    {
                        //InterLineAdornments need to be placed at a point rather than on a span.
                        Debug.Assert((spans.Count == 1) && (spans[0].Length == 0));

                        int position = spans[0].Start;

                        //If we got a zero-length span using the check above then it should intersect with the visible spans.
                        Debug.Assert(((NormalizedSpanCollection)visibleSpansOnLine).IntersectsWith(new Span(position, 0)));

                        var originalTag = _tags.Find((tag) => { return t.Tag == tag.Tag; });
                        if (originalTag != null)
                        {
                            //We found a pre-existing ActiveTag with the same tag as what we just got. We want to continue to use
                            //the old tag so we don't want to delete it.
                            originalTag.NeedsToBeDeleted = false;

                            originalTag.Position = position;    //Move the tag to its new position (generally it will be the same).
                        }
                        else
                        {
                            //A brand new tag that needs to be added (but mark it appropriate if we are delaying tag changes).
                            var tag = new ActiveTag(position, t.Tag);

                            tag.DelayTagCreation = _delayTagChanges;
                            _tags.Add(tag);
                        }
                    }
                }
            }

            //Look at all the tags on the line and generate the line transform from them.
            //Don't use tags whose creation is defered.
            //Don't use tags who are flagged for deletion unless we're delaying tag changes.
            //And, of course, only use the tags that actually fall on the line.
            double topSpace = 0.0;
            double bottomSpace = 0.0;
            foreach (ActiveTag t in _tags)
            {
                if ((!t.DelayTagCreation) && (_delayTagChanges || !t.NeedsToBeDeleted) &&
                    line.ContainsBufferPosition(new SnapshotPoint(_currentSnapshot, t.Position)))
                {
                    //If we're delaying changes, we need to use the cached line height since (otherwise) we might end up shifting the caret
                    //if a tag's height is being animated. If we're not delaying changes then we need to update the cached height.
                    if (!_delayTagChanges)
                        t.Height = t.Tag.Height;

                    if (t.Tag.IsAboveLine)
                    {
                        topSpace = Math.Max(topSpace, t.Height);
                    }
                    else
                    {
                        bottomSpace = Math.Max(bottomSpace, t.Height);
                    }
                }
            }

            return new LineTransform(topSpace, bottomSpace, 1.0);
        }
        #endregion

        #region Event handlers
        void OnTextViewClosed(object sender, System.EventArgs e)
        {
            _tagAggregator.BatchedTagsChanged -= this.OnBatchedTagsChanged;
            _view.LayoutChanged -= this.OnLayoutChanged;
            _view.Closed -= this.OnTextViewClosed;

            for (int i = _tags.Count - 1; (i >= 0); --i)
            {
                this.RemoveTagAt(i);
            }
            Debug.Assert(_tags.Count == 0);

            _tagAggregator.Dispose();
            _tagAggregator = null;
        }

        void OnBatchedTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            if (_view.IsClosed)
                return;         //The view was closed out from under us.

            if (e.Spans.Count > 0)
            {
                List<Span> invalidatedSpans = new List<Span>();

                foreach (var mappingSpan in e.Spans)
                {
                    foreach (var span in mappingSpan.GetSpans(_currentSnapshot))
                    {
                        invalidatedSpans.Add(span);
                    }
                }

                try
                {
                    _needsLayout = true;
                    _invalidatedSpans = new NormalizedSpanCollection(invalidatedSpans);

                    this.PerformLayout(_view.Caret.Position.BufferPosition);
                }
                finally
                {
                    _invalidatedSpans = null;
                }
            }
        }

        internal void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            _needsLayout = false;

            Debug.Assert(_currentSnapshot == e.NewSnapshot);
            Debug.Assert(_currentSnapshot == _view.TextSnapshot);

            //Remove tags that have disappeared, add ones that need to be added.
            for (int i = _tags.Count - 1; (i >= 0); --i)
            {
                ActiveTag tag = _tags[i];

                if (tag.NeedsToBeDeleted)
                {
                    if (_delayTagChanges)
                    {
                        //We need to delete the tag in a subsequent layout
                        _needsLayout = true;
                    }
                    else
                    {
                        //We can delete the tag now (& this doesn't trigger a subsequent layout)
                        this.RemoveTagAt(i);
                    }
                }
                else
                {
                    var position = new SnapshotPoint(_currentSnapshot, tag.Position);

                    var line = _view.TextViewLines.GetTextViewLineContainingBufferPosition(position);
                    if (line == null)
                    {
                        //The line that contained this tag is no longer visible. We can safely remove the tag immediately.
                        this.RemoveTagAt(i);
                    }
                    else
                    {
                        //The tag's line is visible. Make sure the tag's position is visible on the line.
                        if (tag.DelayTagCreation)
                        {
                            //A tag would be flagged as delayed creation iff we're delaying tag changes.
                            Debug.Assert(_delayTagChanges);
                            _needsLayout = true;
                        }
                        else
                        {
                            if (!tag.IsAddedToAdornmentLayer)
                            {
                                //Only try once to add the adornment to the adornment layer.
                                tag.IsAddedToAdornmentLayer = true;

                                if (tag.Tag.AdornmentFactory != null)
                                {
                                    UIElement adornment = tag.Tag.AdornmentFactory(tag.Tag, _view, position);
                                    if (adornment != null)
                                    {
                                        tag.Adornment = new AdornmentWrapper(adornment);
                                        tag.Tag.HorizontalOffsetChanged += this.OnHorizontalOffsetPropertyChanged;

                                        _layer.AddAdornment(AdornmentPositioningBehavior.OwnerControlled,
                                                            null,
                                                            null, tag.Adornment, null);
                                    }
                                }

                                //Hook to the height changed event after creating the adornment so we ignore any height changed
                                //events that might have been triggered as part of its creation.
                                tag.Tag.HeightChanged += this.OnHeightPropertyChanged;
                            }

                            if (tag.Adornment != null)
                            {
                                //Position & scale the adornment appropriately for this line.
                                this.PositionAndScaleTag(line, position, tag);
                            }
                        }
                    }
                }
            }

            if (_needsLayout)
            {
                _view.VisualElement.Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.PerformLayout(_view.Caret.Position.BufferPosition);
                }), DispatcherPriority.Normal);
            }
            else
            {
                //We don't need another layout so we don't need to delay anything.
                _delayTagChanges = false;

                this.AssertNothingDelayed();
            }
        }

        void OnHeightPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var tag = sender as InterLineAdornmentTag;
            if ((tag != null) && tag.IsAnimating)
            {
                //Change the height of the tag, but preserve the position of the line the tag is on instead of preserving the line containing
                //the caret.
                //
                //This may produce odd effects if someone changes the height of a tag in a text changed callback
                //(since we'll be doing layouts with conflicting goals (preserving the caret line and preserving the
                //tags line) but it won't crash since we're doing the TranslateTo below.
                foreach (var t in _tags)
                {
                    if (t.Tag == tag)
                    {
                        _needsLayout = true;
                        this.PerformLayout((new SnapshotPoint(_currentSnapshot, t.Position)).TranslateTo(_view.TextSnapshot, PointTrackingMode.Negative));
                        break;
                    }
                }
            }
        }

        void OnHorizontalOffsetPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //The current snapshot should, almost always, mirrow the view's TextSnapshot.
            //But it may not always: if someone changes the HorizontalOffset of a tag before the view does its layout
            //then we'll be out of sync. In that case, however, we know that the view will be doing a layout soon
            //and we can ignore the event (since the tag's horizontal position will be updated when the view
            //does its layout).
            if (_currentSnapshot == _view.TextSnapshot)
            {
                var tag = sender as InterLineAdornmentTag;
                if (tag != null)
                {
                    foreach (var t in _tags)
                    {
                        if (t.Tag == tag)
                        {
                            var position = new SnapshotPoint(_currentSnapshot, t.Position);
                            var line = _view.TextViewLines.GetTextViewLineContainingBufferPosition(position);
                            Debug.Assert(line != null);

                            this.PositionAndScaleTag(line, position, t);
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Helpers

        [Conditional("DEBUG")]
        private void AssertNothingDelayed()
        {
            //If we were not delaying changes then there shouldn't be any changes to delay.
            foreach (ActiveTag t in _tags)
            {
                Debug.Assert(!t.NeedsToBeDeleted);
                Debug.Assert(!t.DelayTagCreation);
            }
        }

        void PositionAndScaleTag(ITextViewLine line, SnapshotPoint position, ActiveTag tag)
        {
            Debug.Assert((line != null) && line.ContainsBufferPosition(position));

            //Position & scale the adornment appropriately for this line.
            double x = tag.Tag.HorizontalOffset;
            if (tag.Tag.HorizontalPositioningMode == HorizontalPositioningMode.TextRelative)
            {
                x += line.GetCharacterBounds(position).Left;
            }
            else if (tag.Tag.HorizontalPositioningMode == HorizontalPositioningMode.ViewRelative)
            {
                x += _view.ViewportLeft;
            }

            tag.Adornment.RenderTransform = new TranslateTransform(x, tag.Tag.IsAboveLine ? (line.TextTop - tag.Tag.Height)
                                                                                          : line.TextBottom);
            tag.Adornment.RenderTransform.Freeze();
        }

        //Handle all the clean-up associated with removing a tag.
        void RemoveTagAt(int i)
        {
            ActiveTag tag = _tags[i];

            _tags.RemoveAt(i);

            if (tag.IsAddedToAdornmentLayer)
            {
                tag.Tag.HeightChanged -= this.OnHeightPropertyChanged;

                if (tag.Adornment != null)
                {
                    UIElement child = tag.Adornment.Child;

                    //Fire the removed notification first so that anyone who wants to adjust focus can do so
                    //before it gets changed by WPF.
                    if (tag.Tag.RemovalCallback != null)
                    {
                        tag.Tag.RemovalCallback(tag.Tag, child);
                    }

                    tag.Adornment.Clear();
                    tag.Tag.HorizontalOffsetChanged -= this.OnHorizontalOffsetPropertyChanged;
                    _layer.RemoveAdornment(tag.Adornment);
                }
            }
        }

        //Force a layout that preserves the position of the line containing the caret position even as inter-line adornments are added and removed.
        void PerformLayout(SnapshotPoint trackingPoint)
        {
            if ((!_view.IsClosed) && _needsLayout && !_view.InLayout)
            {
                _needsLayout = false;

                if (_delayTagChanges)
                {
                    _delayTagChanges = false;

                    //Clean up all the various changes we were delaying.
                    for (int i = _tags.Count - 1; (i >= 0); --i)
                    {
                        ActiveTag tag = _tags[i];

                        if (tag.NeedsToBeDeleted)
                        {
                            //This tag was marked for deletion and was waiting for a layout in which there wasn't a text change.
                            //We can do this immediately since we forcing a layout that keeps the caret line in the desired position.
                            this.RemoveTagAt(i);
                        }
                        else
                        {
                            tag.DelayTagCreation = false;
                        }
                    }
                }
                else
                {
                    this.AssertNothingDelayed();
                }

                ITextViewLine anchorLine = _view.TextViewLines.GetTextViewLineContainingBufferPosition(trackingPoint);
                double anchorDistance;
                ViewRelativePosition anchorPlacement = ViewRelativePosition.Top;

                if (anchorLine == null)
                {
                    anchorLine = _view.TextViewLines.FirstVisibleLine;
                    anchorDistance = anchorLine.Top - _view.ViewportTop;
                }
                else
                {
                    //We are positioning with respect to the location of the line containing the caret. Keep the top of the text of the caret line the same.
                    Debug.Assert(anchorLine.VisibilityState != VisibilityState.Unattached);
                    anchorPlacement = (ViewRelativePosition)(ViewRelativePosition2.TextTop);
                    anchorDistance = anchorLine.TextTop - _view.ViewportTop;
                }

                _view.DisplayTextLineContainingBufferPosition(anchorLine.Start, anchorDistance, anchorPlacement);
            }
        }
        #endregion

        internal class ActiveTag
        {
            public readonly InterLineAdornmentTag Tag;
            public AdornmentWrapper Adornment;

            public int Position;    //This is always on the manager's _currentSnapshot

            public double Height;   //A snapshot of this.Tag.Height that is used when we don't want to risk changing the position of the caret line.

            public bool IsAddedToAdornmentLayer;
            public bool DelayTagCreation;
            public bool NeedsToBeDeleted;

            public ActiveTag(int position, InterLineAdornmentTag tag)
            {
                this.Position = position;
                this.Tag = tag;
            }
        }

        internal class AdornmentWrapper : Canvas
        {
            public UIElement Child { get { return this.Children[0]; } }

            public AdornmentWrapper(UIElement child)
            {
                this.Children.Add(child);
            }

            public void Clear()
            {
                this.Children.Clear();
            }
        }

        public enum ViewRelativePosition2
        {
            /// <summary>
            /// The offset with respect to the top of the view to the top of the line.
            /// </summary>
            /// <remarks>
            /// Must match ViewRelativePosition.Top.
            /// </remarks>
            Top = ViewRelativePosition.Top,

            /// <summary>
            /// The offset with respect to the bottom of the view to the bottom of the line.
            /// </summary>
            /// <remarks>
            /// Must match ViewRelativePosition.Bottom.
            /// </remarks>
            Bottom = ViewRelativePosition.Bottom,

            /// <summary>
            /// The offset with respect to the top of the view to the top of the text on the line.
            /// </summary>
            TextTop,

            /// <summary>
            /// The offset with respect to the bottom of the view to the bottom of the text on the line.
            /// </summary>
            TextBottom
        }

    /// <summary>
    /// Tag Aggregator options.
    /// </summary>
    [Flags]
    public enum TagAggregatorOptions2
    {
        /// <summary>
        /// Default behavior. The tag aggregator will map up and down through all projection buffers.
        /// </summary>
        None = TagAggregatorOptions.None,

        /// <summary>
        /// Only map through projection buffers that have the "projection" content type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Normally, a tag aggregator will map up and down through all projection buffers (buffers
        /// that implement <see cref="IProjectionBufferBase"/>).  This flag will cause the projection buffer
        /// to not map through buffers that are projection buffers but do not have a projection content type.
        /// </para>
        /// </remarks>
        /// <comment>This is used by the classifier aggregator, as classification depends on content type.</comment>
        MapByContentType = TagAggregatorOptions.MapByContentType,

        /// <summary>
        /// Delay creating the taggers for the tag aggregator.
        /// </summary>
        /// <remarks>
        /// <para>A tag aggregator will, normally, create all of its taggers when it is created. This option
        /// will cause the tagger to defer the creation until idle time tasks are done.</para>
        /// <para>If this option is set, a TagsChanged event will be raised after the taggers have been created.</para>
        /// </remarks>
        DeferTaggerCreation = 0x02,

        /// <summary>
        /// Do not create taggers on child buffers.
        /// </summary>
        /// <remarks>
        /// <para>A common reason to use this flag would for a tagger that is creating its own tag aggregator
        /// (for example, to translate one tag into another type of tag). In that case, you can expect another
        /// instance of your tagger to be created on the child buffers (which would create its own tag aggregators)
        /// so you don't want to have your tag aggregator include those buffers/
        /// </para>
        /// </remarks>
        NoProjection = 0x04
    }
}
}
