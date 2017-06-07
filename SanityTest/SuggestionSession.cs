using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
//using Microsoft.VisualStudio.Text.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SanityTest
{
    internal class SuggestionSession
    {

        //public void Start()
        //{

        //}

        ITrackingPoint TriggerPoint { get; }
        public ITextView TextView { get; private set; }

        public ITrackingPoint GetTriggerPoint(ITextBuffer textBuffer)
        {
            var mappedTriggerPoint = GetTriggerPoint(textBuffer.CurrentSnapshot);

            if (!mappedTriggerPoint.HasValue)
            {
                return null;
            }

            //Peek points want to be preserved across 
            return mappedTriggerPoint.Value.Snapshot.CreateTrackingPoint(mappedTriggerPoint.Value, PointTrackingMode.Negative, TrackingFidelityMode.UndoRedo);
        }

        public SnapshotPoint? GetTriggerPoint(ITextSnapshot textSnapshot)
        {
            var triggerSnapshotPoint = this.TriggerPoint.GetPoint(TextView.TextSnapshot);
            var triggerSpan = new SnapshotSpan(triggerSnapshotPoint, 0);

            var mappedSpans = new List<SnapshotSpan>();
            MapDownToBufferNoTrack(triggerSpan, textSnapshot.TextBuffer, mappedSpans);

            if (mappedSpans.Count == 0)
            {
                return null;
            }
            else
            {
                return mappedSpans[0].Start;
            }
        }

        internal static void MapDownToBufferNoTrack(SnapshotSpan sourceSpan, ITextBuffer targetBuffer, IList<SnapshotSpan> mappedSpans, bool mapByContentType = false)
        {
            // Most of the time, the sourceSpan will map to the targetBuffer as a single span, rather than being split.
            // Since this method is called a lot, we'll assume first that we'll get a single span and don't need to
            // allocate a stack to keep track of unmapped spans. If that fails we'll fall back on the more expensive approach.
            // Scroll around for a while and this saves a bunch of allocations.
            SnapshotSpan mappedSpan = sourceSpan;
            while (true)
            {
                if (mappedSpan.Snapshot.TextBuffer == targetBuffer)
                {
                    mappedSpans.Add(mappedSpan);
                    return;
                }
                else
                {
                    IProjectionSnapshot mappedSpanProjectionSnapshot = mappedSpan.Snapshot as IProjectionSnapshot;
                    if (mappedSpanProjectionSnapshot != null &&
                        (!mapByContentType || mappedSpanProjectionSnapshot.ContentType.IsOfType("projection")))
                    {
                        var mappedDownSpans = mappedSpanProjectionSnapshot.MapToSourceSnapshots(mappedSpan);
                        if (mappedDownSpans.Count == 1)
                        {
                            mappedSpan = mappedDownSpans[0];
                            continue;
                        }
                        else if (mappedDownSpans.Count == 0)
                        {
                            return;
                        }
                        else
                        {
                            // the projection mapping resulted in more than one span
                            List<SnapshotSpan> unmappedSpans = new List<SnapshotSpan>(mappedDownSpans);
                            SplitMapDownToBufferNoTrack(unmappedSpans, targetBuffer, mappedSpans, mapByContentType);
                            return;
                        }
                    }
                    else
                    {
                        // either it's a projection buffer we can't look through, or it's
                        // an ordinary buffer that didn't match
                        return;
                    }
                }
            }
        }

        private static void SplitMapDownToBufferNoTrack(List<SnapshotSpan> unmappedSpans, ITextBuffer targetBuffer, IList<SnapshotSpan> mappedSpans, bool mapByContentType)
        {
            while (unmappedSpans.Count > 0)
            {
                SnapshotSpan span = unmappedSpans[unmappedSpans.Count - 1];
                unmappedSpans.RemoveAt(unmappedSpans.Count - 1);

                if (span.Snapshot.TextBuffer == targetBuffer)
                {
                    mappedSpans.Add(span);
                }
                else
                {
                    IProjectionSnapshot spanSnapshotAsProjection = span.Snapshot as IProjectionSnapshot;
                    if (spanSnapshotAsProjection != null &&
                        (!mapByContentType || span.Snapshot.TextBuffer.ContentType.IsOfType("projection")))
                    {
                        unmappedSpans.AddRange(spanSnapshotAsProjection.MapToSourceSnapshots(span));
                    }
                }
            }
        }

    }
}