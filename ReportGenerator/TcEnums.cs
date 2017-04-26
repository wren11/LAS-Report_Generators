using System.ComponentModel;

namespace SAReport
{
    public enum TeXYNodeType
    {
        [Description("Root")]
        Root = 0,

        [Description("Building")]
        Building
    }
    //-----------------------------------------------------------------------------

    public enum TeLevlingNodeType
    {
        [Description("Root")]
        Root = 0,

        [Description("Cross Strip")]
        XStrip,

        [Description("Strip")]
        Strip
    }
    //-----------------------------------------------------------------------------

    public enum TeDeliveryPacketType
    {
        [Description("Navigation")]
        Sbet = 0,

        [Description("Lidar")]
        Sdc,

        [Description("Tac Register")]
        Tac,

        [Description("IIQ Image")]
        Image,

        [Description("Thumbnail")]
        Thumbnail,

        [Description("Coverage")]
        Coverage,

        [Description("LAS File")]
        LasFiles,

        [Description("LAS 1.2")]
        Las12Files,

        [Description("Polygon")]
        Polygon,

        [Description("EO File")]
        EOFile
    }
    //-----------------------------------------------------------------------------

    public enum TeDeliveryStatus
    {
        Started = 0,
        Progressing,
        Completed,
        Error,
        AreaStarted,
        AreaFinished
    }
    //-----------------------------------------------------------------------------

    public enum TeLevelingNodeStatus
    {
        [Description("Unknown Mode")]
        Unknown = -1,

        [Description("Clicked Node")]
        Clicked,

        [Description("Intersected with Clicked Node")]
        Intersected,

        [Description("Processed in Tree")]
        Processed,

        [Description("Unprocessed Node")]
        Unprocessed
    }
    //------------------------------------------------------------------

    public enum TeXYStatus
    {
        [Description("Unknown Mode")]
        Unknown = -1,

        [Description("Not Processed")]
        Unprocessed,

        [Description("Processed")]
        Processed,

        [Description("Acceptable")]
        Acceptable,

        [Description("Bad Quality")]
        BadQuality,

        [Description("Adjusted")]
        Adjusted
    }
    //------------------------------------------------------------------

    public enum TeDirection
    {
        [Description("Unknown Direction")]
        Unknown = 0,

        [Description("Left")]
        Left,

        [Description("Right")]
        Right,

        [Description("Up")]
        Up,

        [Description("Down")]
        Down
    }
    //------------------------------------------------------------------

    public enum TeLasFormat
    {
        [Description("LAS12_PDRF0")]
        LAS120 = 0,

        [Description("LAS12_PDRF0_GPS")]
        LAS121 = 1,

        [Description("LAS12_PDRF0_RGB")]
        LAS122 = 2,

        [Description("LAS12_PDRF1_RGB")]
        LAS123 = 3,

        [Description("LAS13_FORM1_WDP")]
        LAS134 = 4,

        [Description("LAS13_FORM2_WDP")]
        LAS135 = 5,

        [Description("LAS14_PDRF0_GPS")]
        LAS146 = 6,

        [Description("LAS14_PDRF6_RGB")]
        LAS147 = 7,

        [Description("LAS14_PDRF7_NIR")]
        LAS148 = 8,

        [Description("LAS14_PDRF6_WAVE")]
        LAS149 = 9,

        [Description("LAS14_PDRF7_WAVE")]
        LAS14X = 10
    }
    //-----------------------------------------------------------------------------

    public enum TeHeightNodeType
    {
        [Description("Root")]
        Root = 0,

        [Description("Rejected - Not Covered")]
        NotCovered = 1,

        [Description("Rejected - Not Flat")]
        NotFlat = 2,

        [Description("Rejected - Out of 3σ")]
        OutOfSigma = 3,

        [Description("Accepted")]
        Accepted = 4,

        [Description("Height")]
        Height = 5,

        [Description("Level LAS")]
        LAS = 6
    }
    //-----------------------------------------------------------------------------

    public enum TeUserQAType
    {
        [Description("None")]
        None = -1,

        [Description("LiDAR Coverage Confirmed")]
        CoverageConfirmed = 0,

        [Description("Image Coverage Confirmed")]
        ImageCoverageConfirmed = 1,

        [Description("XY Adjustment Confirmed")]
        XYConfirmed = 2,

        [Description("Strip Leveling Confirmed")]
        LevelingConfirmed = 3,

        [Description("Z Adjustment Confirmed")]
        HeightConfirmed = 4,

        [Description("QA Confirmed")]
        QA = 5,

        [Description("LAS Generation Confirmed")]
        LASConfirmed = 6
    }
    //-----------------------------------------------------------------------------

    public enum TeLasGenerationType
    {
        [Description("Quick")]
        Quick = 0,

        [Description("Full")]
        Full = 1
    }
    //-----------------------------------------------------------------------------

}
