using System;
using System.Collections.Concurrent;
namespace AviDefMark
{
    public interface IMachineLearningDispatcher
    {
        void InsertSingleCrossSectionRecord(CrossSectionRecord record);
        void InsertBulk(CrossSectionRecord[] records);
        bool IsConnectedToDataBase { get; }
        BlockingCollection<CrossSectionRecord> DispatchCollection { get; }
    }
}
