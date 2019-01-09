using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace KakiAISpeaker.Client.Behaviors
{
    /// <summary>
    ///     미디어 비헤이비어
    /// </summary>
    public class MediaBehavior : Behavior<MediaElement>
    {
        /// <summary>
        ///     스트림 프로퍼티
        /// </summary>
        public static readonly DependencyProperty StreamProperty =
            DependencyProperty.Register("Stream", typeof(object), typeof(MediaBehavior)
                , new PropertyMetadata(null, StreamChanged));

        public object Stream
        {
            get => GetValue(StreamProperty);
            set => SetValue(StreamProperty, value);
        }

        private static void StreamChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (MediaBehavior) d;
            behavior.ExecuteStreamChanged();
        }

        private void ExecuteStreamChanged()
        {
            if (Stream == null) return;
            AssociatedObject.AutoPlay = true;
            AssociatedObject.SetSource(Stream as IRandomAccessStream, "audio/mpeg");
            AssociatedObject.Play();
        }


        protected override void OnAttached()
        {
            AssociatedObject.MediaEnded += AssociatedObject_MediaEnded;
        }

        private void AssociatedObject_MediaEnded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.AutoPlay = false;
            AssociatedObject.Stop();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MediaEnded -= AssociatedObject_MediaEnded;
        }
    }
}
