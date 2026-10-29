using System;
using System.Windows;
using System.Windows.Controls;

namespace MCERemote.Controller.Controls
{
    /// <summary>
    /// Sizes item based scrolling lists to show only whole items.
    /// </summary>
    public static class ListFitter
    {
        /// <summary>
        /// Sets the list height to fit a whole number of items within the available area,
        /// so no item is ever shown cut off.
        /// </summary>
        /// <param name="list">The list, aligned to the top of the area.</param>
        /// <param name="area">The element whose height is available to the list.</param>
        /// <param name="itemHeight">Height of one item including its margin. Measured from a
        /// realized item when 0 or less, and kept for later calls.</param>
        public static void FitToWholeItems(ItemsControl list, FrameworkElement area, ref double itemHeight)
        {
            if (itemHeight <= 0 && list.Items.Count > 0)
            {
                list.UpdateLayout();
                var container = list.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
                if (container != null && container.ActualHeight > 0)
                    itemHeight = container.ActualHeight + container.Margin.Top + container.Margin.Bottom;
            }

            if (itemHeight <= 0)
                return;

            var count = Math.Max(1, Math.Floor(area.ActualHeight / itemHeight));
            list.Height = count * itemHeight;
        }
    }
}
