namespace GritTrack.PageModels
{
    // one course's line in the what-if simulator: the grade you have now and
    // the grade your simulated scores would land you at
    public class SimulatedCourseResult
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CurrentGrade { get; set; } = string.Empty;
        public string ProjectedGrade { get; set; } = string.Empty;
        public double ProjectedPercent { get; set; }

        public string Summary
        {
            get
            {
                string before = string.IsNullOrWhiteSpace(CurrentGrade) ? "no grade yet" : CurrentGrade;
                return $"{CourseCode}: {before} to {ProjectedGrade} ({ProjectedPercent:0}%)";
            }
        }
    }
}
