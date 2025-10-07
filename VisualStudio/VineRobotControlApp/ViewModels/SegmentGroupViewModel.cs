using System.Collections.ObjectModel;
using VineRobotControlApp.Models;

namespace VineRobotControlApp.ViewModels;

public class SegmentGroupViewModel
{
    public SegmentGroupViewModel(SegmentSide side, IEnumerable<SegmentSetpointViewModel> segments)
    {
        Side = side;
        SideLabel = side == SegmentSide.Left ? "Left" : "Right";
        Segments = new ObservableCollection<SegmentSetpointViewModel>(segments);
    }

    public SegmentSide Side { get; }
    public string SideLabel { get; }
    public ObservableCollection<SegmentSetpointViewModel> Segments { get; }
}
