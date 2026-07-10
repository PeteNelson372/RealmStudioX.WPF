using RealmStudioShapeRenderingLib;
using RealmStudioX.WPF.ViewModels.Infrastructure;

namespace RealmStudioX.WPF.Models.UserInterface
{
    public class LayoutOptions : ViewModelBase
    {
        // Position relative to the path

        private double _offset = 0;
        public double Offset
        {
            get => _offset;
            set => SetProperty(ref _offset, value);
        }

        // How objects are positioned along the path

        private PlacementStrategy _distribution = PlacementStrategy.Even;
        public PlacementStrategy Distribution
        {
            get => _distribution;
            set => SetProperty(ref _distribution, value);
        }

        // Used for Random distribution
        private double _spacing = 0;
        public double Spacing
        {
            get => _spacing;
            set => SetProperty(ref _spacing, value);
        }

        // Optional starting distance along path
        private double _startOffset = 0;
        public double StartOffset
        {
            get => _startOffset;
            set => SetProperty(ref _startOffset, value);
        }

        // Optional ending distance from end of path
        private double _endOffset = 0;
        public double EndOffset
        {
            get => _endOffset;
            set => SetProperty(ref _endOffset, value);
        }

        // Rotate objects to follow the path
        private bool _followPathRotation = true;
        public bool FollowPathRotation
        {
            get => _followPathRotation;
            set => SetProperty(ref _followPathRotation, value);
        }

        // Keep object upright
        private bool _keepUpright = false;
        public bool KeepUpright
        {
            get => _keepUpright;
            set => SetProperty(ref _keepUpright, value);
        }
    }
}
