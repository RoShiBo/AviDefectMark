using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

namespace AviDefMark
{
    public class MachineLearningDispatcher : IMachineLearningDispatcher 
    {
        private SqlConnection _connection;
      
        private bool _isConnectedToDataBase;
        private const string _tableName = "SurfaceData";
        private object _lock = new object();
//        USE [AVIMachineLearning]
//GO

///****** Object:  Table [dbo].[SurfaceData]    Script Date: 11/12/2017 17:31:27 ******/
//SET ANSI_NULLS ON
//GO

//SET QUOTED_IDENTIFIER ON
//GO

//CREATE TABLE [dbo].[SurfaceData](
//    [ID] [bigint] IDENTITY(1,1) NOT NULL,
//    [Panel] [nvarchar](500) NULL,
//    [Side] [nvarchar](500) NULL,
//    [Stent] [nvarchar](500) NULL,
//    [Segment_Type] [int] NULL,
//    [FirstVector] [varbinary](max) NULL,
//    [SecondVector] [varbinary](max) NULL,
//    [SurfaceClassification] [int] NULL,
//    [Width] [float] NULL,
//    [NumOfPeaks] [int] NULL,
//    [PositionInSegment] [int] NULL,
//    [GlobalPositionX] [int] NULL,
//    [GlobalPositionY] [int] NULL,
//    [FilePath] [nvarchar](500) NULL,
//    [AnalyzeDate] [datetime] NULL
//) ON [PRIMARY] 

//GO
        private BlockingCollection<CrossSectionRecord> _dispatchCollection = new BlockingCollection<CrossSectionRecord>();

        public BlockingCollection<CrossSectionRecord> DispatchCollection
        {
            get { return _dispatchCollection; }            
        }


        public DataSet RecordsDataSet { get; set; }

        public SqlDataAdapter Adapter { get; set; }

        public MachineLearningDispatcher(string connectionString)
        {
            _connection = new SqlConnection(connectionString);

            try
            {
                _connection.Open();
                //using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                //{
                //    dataAdapter.SelectCommand = new SqlCommand("select TOP 1 * from " + _tableName, _connection);
                //    _dataAdapter = dataAdapter;
                //    DataSet dataSet = new DataSet();

                //    dataAdapter.Fill(dataSet);
                //    _dataSet = dataSet;
                //}
                IsConnectedToDataBase = true;
                //Task.Run(() => DispatchTask());
            }
            catch (Exception)
            {

                IsConnectedToDataBase = false;
            }
        }

        public bool IsConnectedToDataBase
        {
            get { return _isConnectedToDataBase; }
            private set { _isConnectedToDataBase = value; }
        }


        private void DispatchTask()
        {
            while (true)
            {
                //List<CrossSectionRecord> csrs = new List<CrossSectionRecord>();
                var csrFirst = _dispatchCollection.Take(); // for blocking
                Thread.Sleep(100); // allow producer some time to insert items into queue
                var csrList = _dispatchCollection.GetConsumingEnumerable().Take(_dispatchCollection.Count).ToList();
                csrList.Insert(0, csrFirst);
                InsertBulk(csrList.ToArray());
                //foreach (var item in _dispatchCollection.GetConsumingEnumerable())
                //{
                //   csrs.Add(item);
                //}
                //InsertBulk(csrs.ToArray());
            }

        }

        public void InsertBulk(CrossSectionRecord[] records)
        {

            if (!IsConnectedToDataBase)
                return;

            lock (_lock)
            {
                using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                {
                    dataAdapter.SelectCommand = new SqlCommand("select TOP 1 * from " + _tableName, _connection);

                    DataSet dataSet = new DataSet();

                    dataAdapter.Fill(dataSet);

                    Debug.WriteLine("There are {0} rows in the table", dataSet.Tables[0].Rows.Count);

                    DataRow[] rows = new DataRow[records.Length];

                    for (int i = 0; i < records.Length; i++)
                    {
                        DataRow row = dataSet.Tables[0].NewRow();
                        //row["ID"] = record.ID;
                        row["Panel"] = records[i].Panel;
                        row["Stent"] = records[i].Stent;
                        row["Side"] = records[i].Side;
                        row["Segment_Type"] = records[i].Segment_Type;
                        row["FirstVector"] = records[i].FirstVector;
                        row["SecondVector"] = records[i].SecondVector;
                        row["SurfaceClassification"] = records[i].SurfaceClassification;
                        row["Width"] = records[i].Width;
                        row["NumOfPeaks"] = records[i].NumOfPeaks;
                        row["PositionInSegment"] = records[i].PositionInSegment;
                        row["GlobalPositionX"] = records[i].GlobalPositionX;
                        row["GlobalPositionY"] = records[i].GlobalPositionY;
                        row["FilePath"] = records[i].FilePath;
                        row["AnalyzeDate"] = records[i].AnalyzeDate;

                        rows[i] = row;

                    }


                    SqlTransaction transaction = _connection.BeginTransaction();

                    using (var bulkCopy = new SqlBulkCopy(_connection, SqlBulkCopyOptions.Default, transaction))
                    {
                        bulkCopy.BatchSize = rows.Length;
                        bulkCopy.DestinationTableName = "dbo." + _tableName;
                        try
                        {
                            // define mappings for columns, as property names / generated data table column names
                            // is different from destination table column name
                            //bulkCopy.ColumnMappings.Add("ID", "UserID");
                            //bulkCopy.ColumnMappings.Add("Angle", "Angle");
                            // the other mappings come here

                            bulkCopy.WriteToServer(rows);
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                        }
                    }

                    transaction.Commit();

                }
            }
        }

        public CrossSectionRecord[] GetRecordsPerStent(String Panel, String Stent)
        {
            CrossSectionRecord[] recordList = null;
            
            using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
            {
                string SQL = " SELECT  [ID]" +
                                         " ,[Panel] " +
                                         " ,[Side] " +
                                         "  ,[Stent] " +
                                         " ,[Segment_Type]" +
                                        "  ,[FirstVector]" +
                                         " ,[SecondVector]" +
                                          ",[SurfaceClassification]" +
                                          ",[Width]" +
                                           ",[NumOfPeaks]" +
                                          ",[PositionInSegment]" +
                                          ",[GlobalPositionX]" +
                                          ",[GlobalPositionY]" +
                                          ",[FilePath]" +
                                          ",[AnalyzeDate]" +
                                           ",[ManualSurfaceClassification]" +
                                    "  FROM[AVIMachineLearning].[dbo].[SurfaceData] ";

                dataAdapter.SelectCommand = new SqlCommand(SQL + "WHERE Panel LIKE '%" + Panel + "%' AND STENT LIKE '" + Stent + "'", _connection);
                dataAdapter.SelectCommand.CommandTimeout = 240;
                DataSet dataSet = new DataSet();
                
                dataAdapter.Fill(dataSet);
                RecordsDataSet = dataSet;
                Debug.WriteLine("There are {0} rows in the table", dataSet.Tables[0].Rows.Count);

                if (dataSet.Tables[0].Rows.Count == 0)
                    return recordList;

                recordList = new CrossSectionRecord[dataSet.Tables[0].Rows.Count];
                int i = 0;
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    CrossSectionRecord record = new CrossSectionRecord();
                    record.ID = (long)row["ID"];
                    record.Panel = (string)row["Panel"];
                     record.Stent = (string)row["Stent"];
                    record.Side = (string)row["Side"];
                    record.Segment_Type =(int) row["Segment_Type"];
                    record.FirstVector = (byte[])row["FirstVector"];
                    record.SecondVector = (byte[])row["SecondVector"];
                    record.SurfaceClassification = (int)row["SurfaceClassification"];
                    record.Width = (float)(double)row["Width"];
                    record.NumOfPeaks = (int)row["NumOfPeaks"];
                    record.PositionInSegment = (int)row["PositionInSegment"];
                    record.GlobalPositionX = (int)row["GlobalPositionX"];
                    record.GlobalPositionY = (int)row["GlobalPositionY"];
                    record.FilePath = (string)row["FilePath"];
                    record.AnalyzeDate = (DateTime)row["AnalyzeDate"];

                    recordList[i++] = record;
                }
                Adapter = dataAdapter;
            }
            
                return recordList;
        }

        public void UpdateManualClassification(List<CrossSectionRecord> rlist, TransformPanel Tp)
        {
            int i = rlist.Count();
            foreach (CrossSectionRecord item in rlist)
            {
                
                SqlCommand SqlCmd = new SqlCommand( " UPDATE [AVIMachineLearning].[dbo].[SurfaceData] SET [ManualSurfaceClassification] = 1 WHERE ID =" + item.ID.ToString());
                SqlCmd.Connection = _connection;
                SqlCmd.ExecuteNonQuery();

                Tp.Dispatcher.Invoke(() =>
                {
                    Tp.NumOfSavedRecords = (--i).ToString();
                });

                
                
            }


            
            
        }

        public void InsertSingleCrossSectionRecordNew(CrossSectionRecord record)
        {
            if (!IsConnectedToDataBase)
                return;

            lock (_lock)
            {
                using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                {
                    dataAdapter.SelectCommand = new SqlCommand("select TOP 1 * from " + _tableName, _connection);

                    DataSet dataSet = new DataSet();

                    dataAdapter.Fill(dataSet);

                    Debug.WriteLine("There are {0} rows in the table", dataSet.Tables[0].Rows.Count);

                    DataRow row = dataSet.Tables[0].NewRow();
                    //row["ID"] = record.ID;
                    row["Panel"] = record.Panel;
                    row["Stent"] = record.Stent;
                    row["Side"] = record.Side;
                    row["Segment_Type"] = record.Segment_Type;
                    row["FirstVector"] = record.FirstVector;
                    row["SecondVector"] = record.SecondVector;
                    row["SurfaceClassification"] = record.SurfaceClassification;
                    row["Width"] = record.Width;
                    row["NumOfPeaks"] = record.NumOfPeaks;
                    row["PositionInSegment"] = record.PositionInSegment;
                    row["GlobalPositionX"] = record.GlobalPositionX;
                    row["GlobalPositionY"] = record.GlobalPositionY;
                    row["FilePath"] = record.FilePath;
                    row["AnalyzeDate"] = record.AnalyzeDate;

                    dataSet.Tables[0].Rows.Add(row);

                    dataAdapter.InsertCommand =
                        new SqlCommand(
                            "insert into " + _tableName + @"
                            (
                                Panel,
                                Stent,
                                Side,
                                Segment_Type,
                                FirstVector,
                                SecondVector,
                                SurfaceClassification,
                                Width,
                                NumOfPeaks,
                                PositionInSegment,
                                GlobalPositionX,
                                GlobalPositionY,
                                FilePath,
                                AnalyzeDate                                                                                                                                            
                            )
                            values 
                            (
                                @Panel,
                                @Stent,
                                @Side,
                                @Segment_Type,
                                @FirstVector,
                                @SecondVector,
                                @SurfaceClassification,
                                @Width,
                                @NumOfPeaks,
                                @PositionInSegment,
                                @GlobalPositionX,
                                @GlobalPositionY,
                                @FilePath,
                                @AnalyzeDate        
                            )",
                            _connection);

                    //dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("ID", row["ID"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Panel", row["Panel"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Stent", row["Stent"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Side", row["Side"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Segment_Type", row["Segment_Type"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("FirstVector", row["FirstVector"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("SecondVector", row["SecondVector"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("SurfaceClassification", row["SurfaceClassification"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Width", row["Width"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("NumOfPeaks", row["NumOfPeaks"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("PositionInSegment", row["PositionInSegment"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("GlobalPositionX", row["GlobalPositionX"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("GlobalPositionY", row["GlobalPositionY"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("FilePath", row["FilePath"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("AnalyzeDate", row["AnalyzeDate"]));

                    dataAdapter.Update(dataSet);

                    ////Just to prove we inserted
                    //using (DataSet newDataSet = new DataSet())
                    //{
                    //    dataAdapter.Fill(newDataSet);
                    //    Debug.WriteLine("There are {0} rows in the table", newDataSet.Tables[0].Rows.Count);
                    //}
                }
            }
        }


        public void InsertSingleCrossSectionRecord(CrossSectionRecord record)
        {
            if (!IsConnectedToDataBase)
                return;

            lock (_lock)
            {
                using (SqlDataAdapter dataAdapter = new SqlDataAdapter())
                {
                    dataAdapter.SelectCommand = new SqlCommand("select TOP 1 * from " + _tableName, _connection);

                    DataSet dataSet = new DataSet();

                    dataAdapter.Fill(dataSet);

                    Debug.WriteLine("There are {0} rows in the table", dataSet.Tables[0].Rows.Count);

                    DataRow row = dataSet.Tables[0].NewRow();
                    //row["ID"] = record.ID;
                    row["Panel"] = record.Panel;
                    row["Stent"] = record.Stent;
                    row["Side"] = record.Side;
                    row["Segment_Type"] = record.Segment_Type;
                    row["FirstVector"] = record.FirstVector;
                    row["SecondVector"] = record.SecondVector;
                    row["SurfaceClassification"] = record.SurfaceClassification;
                    row["Width"] = record.Width;
                    row["NumOfPeaks"] = record.NumOfPeaks;
                    row["PositionInSegment"] = record.PositionInSegment;
                    row["GlobalPositionX"] = record.GlobalPositionX;
                    row["GlobalPositionY"] = record.GlobalPositionY;
                    row["FilePath"] = record.FilePath;
                    row["AnalyzeDate"] = record.AnalyzeDate;

                    dataSet.Tables[0].Rows.Add(row);

                    dataAdapter.InsertCommand =
                        new SqlCommand(
                            "insert into " + _tableName + @"
                            (
                                Panel,
                                Stent,
                                Side,
                                Segment_Type,
                                FirstVector,
                                SecondVector,
                                SurfaceClassification,
                                Width,
                                NumOfPeaks,
                                PositionInSegment,
                                GlobalPositionX,
                                GlobalPositionY,
                                FilePath,
                                AnalyzeDate                                                                                                                                            
                            )
                            values 
                            (
                                @Panel,
                                @Stent,
                                @Side,
                                @Segment_Type,
                                @FirstVector,
                                @SecondVector,
                                @SurfaceClassification,
                                @Width,
                                @NumOfPeaks,
                                @PositionInSegment,
                                @GlobalPositionX,
                                @GlobalPositionY,
                                @FilePath,
                                @AnalyzeDate        
                            )",
                            _connection);

                    //dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("ID", row["ID"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Panel", row["Panel"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Stent", row["Stent"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Side", row["Side"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Segment_Type", row["Segment_Type"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("FirstVector", row["FirstVector"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("SecondVector", row["SecondVector"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("SurfaceClassification", row["SurfaceClassification"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("Width", row["Width"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("NumOfPeaks", row["NumOfPeaks"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("PositionInSegment", row["PositionInSegment"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("GlobalPositionX", row["GlobalPositionX"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("GlobalPositionY", row["GlobalPositionY"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("FilePath", row["FilePath"]));
                    dataAdapter.InsertCommand.Parameters.Add(new SqlParameter("AnalyzeDate", row["AnalyzeDate"]));

                    dataAdapter.Update(dataSet);

                    ////Just to prove we inserted
                    //using (DataSet newDataSet = new DataSet())
                    //{
                    //    dataAdapter.Fill(newDataSet);
                    //    Debug.WriteLine("There are {0} rows in the table", newDataSet.Tables[0].Rows.Count);
                    //}
                }
            }
        }



        public bool ConnectToDataBase()
        {
            try
            {
                _connection.Open();
                IsConnectedToDataBase = true;
            }
            catch (InvalidOperationException ioe)
            {
                //already connected
                IsConnectedToDataBase = true;
            }
            catch (SqlException ex)
            {
                IsConnectedToDataBase = false;
            }

            return IsConnectedToDataBase;
        }
    }
}
