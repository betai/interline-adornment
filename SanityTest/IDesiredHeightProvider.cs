using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanityTest
{
    public interface IDesiredHeightProvider
    {
        /// <summary>
        /// The desired height in pixels.
        /// </summary>
        double DesiredHeight { get; }

        /// <summary>
        /// Raised when the container should requery DesiredHeight.
        /// </summary>
        event EventHandler<EventArgs> DesiredHeightChanged;
    }
}
