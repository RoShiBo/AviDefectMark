using System;

namespace AviDefMark
{
    /// <summary>
    /// Represents a single CrossSection record for AviMachineLearinig table 
    /// </summary>
    public class CrossSectionRecord
    {
        //public Int64 ID { get; set; }
        public string Panel { get; set; }
        public string Stent { get; set; }
        public string Side { get; set; }
        public int Segment_Type { get; set; }
        public byte[] FirstVector { get; set; }
        public byte[] SecondVector { get; set; }
        public int SurfaceClassification { get; set; }
        public float Width { get; set; }
        public int NumOfPeaks { get; set; }
        public int PositionInSegment { get; set; }        
        public int GlobalPositionX { get; set; }
        public int GlobalPositionY { get; set; }
        public string FilePath { get; set; }
        public DateTime AnalyzeDate { get; set; }

        public int ManualSurfaceClassification { get; set; }

        public long ID { get; set; }

        public CrossSectionRecord Clone()
        {
            CrossSectionRecord csr = new CrossSectionRecord();
            csr.Panel = Panel;
            csr.Stent = Stent;
            csr.Side = Side;
            csr.Segment_Type = Segment_Type;
            csr.FirstVector = FirstVector;
            csr.SecondVector = SecondVector;
            csr.SurfaceClassification = SurfaceClassification;
            csr.Width = Width;
            csr.NumOfPeaks = NumOfPeaks;
            csr.PositionInSegment = PositionInSegment;
            csr.GlobalPositionX = GlobalPositionX;
            csr.GlobalPositionY = GlobalPositionY;
            csr.FilePath = FilePath;
            csr.AnalyzeDate = AnalyzeDate;
            csr.ManualSurfaceClassification = ManualSurfaceClassification;
            return csr;
        }

    }
}