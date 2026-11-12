namespace GritTrack.PageModels
{
    // one upcoming assignment in the what-if GPA simulator. the slider on the
    // page moves ExpectedPoints, and OnScoreChanged tells the list page to
    // redo the projected GPA
    public class SimulatedAssignmentItem : BaseViewModel
    {
        public int AssignmentId { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double PointsPossible { get; set; }

        private double _expectedPoints;
        public double ExpectedPoints
        {
            get => _expectedPoints;
            set
            {
                // can't score above what the assignment is worth
                var clampedPoints = Math.Clamp(value, 0, PointsPossible);
                if (SetProperty(ref _expectedPoints, clampedPoints))
                {
                    OnPropertyChanged(nameof(ExpectedPercent));
                    OnScoreChanged?.Invoke();
                }
            }
        }

        public double ExpectedPercent => PointsPossible > 0 ? Math.Round((ExpectedPoints / PointsPossible) * 100) : 0;

        public Action? OnScoreChanged { get; set; }
    }
}
