using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AviDefMark
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected string MACHINE_LEARNING_DB_CONNECTION_STRING = @"Data Source=tfs;Initial Catalog=AVIMachineLearning;Persist Security Info=True;User ID=sa;Password=Manager1";
        protected MachineLearningDispatcher _mlDispatcher;
        protected CrossSectionRecord[] rList;
        protected CrossSectionRecord csr = new CrossSectionRecord();
        private string mPAnel, mStent;

        public MainWindow()
        {
            _mlDispatcher = new MachineLearningDispatcher(MACHINE_LEARNING_DB_CONNECTION_STRING);
            InitializeComponent();                               
        }

        private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dg = new OpenFileDialog();
            dg.ShowDialog();
            Tpanel.DisplayImage = dg.FileName;
            if (dg.FileName == "")
                return;
            string[] PathSplit = dg.FileName.Split( '\\');
            Tpanel.Stent = PathSplit.Last().Substring(0, 1) + PathSplit.Last().Substring(2,1 );
            Tpanel.Panel = PathSplit[PathSplit.Count() - 2];
            rList = null;
            Tpanel.NumOfRecords = "";
            border.ClearClassifications();
            btnSaveClassification.IsEnabled = false;
            mPAnel = Tpanel.Panel;
            mStent = Tpanel.Stent;

            GetRecords();            
        }

        private async void SaveClassifications(object sender, RoutedEventArgs e)
        {
           await SaveClassThreadProc();
        }

        private async Task SaveClassThreadProc()
        {
            btnClearClassification.IsEnabled = false;
            btnLoadImage.IsEnabled = false;
            List<Rectangle> RectList = new List<Rectangle>();
            TransformPanel c = border.child as TransformPanel;

            foreach (UIElement item in c.Children)
            {
                
                Rectangle t = item as Rectangle;

                if (t != null)
                {
                   Rectangle r = new Rectangle();
                    r.Margin = t.Margin;
                    r.Width = t.Width;
                    r.Height = t.Height;
                    RectList.Add(r);
                }
            }

            await Task.Run(() =>
            {
               

                    _mlDispatcher.UpdateManualClassification(GetClassifications(rList, RectList),Tpanel);

               
            });

           

            string msg = "Records Have Been Updated.";
            if (Application.Current.Dispatcher.CheckAccess())
            {
                MessageBox.Show(Application.Current.MainWindow, msg);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                    MessageBox.Show(Application.Current.MainWindow, msg);
                }));
            }

            btnClearClassification.IsEnabled = true;
            btnLoadImage.IsEnabled = true;
        }

        private async Task ThreadProc()
        {
            await Task.Run(() =>
            {
              
                    rList = _mlDispatcher.GetRecordsPerStent(mPAnel, mStent);
                        

            });

            Dispatcher.Invoke(() =>
            {
                Tpanel.NumOfRecords = rList.Length.ToString();
                btnSaveClassification.IsEnabled = true;
            });
        }

        private async void GetRecords()
        {
            await ThreadProc();
        }

        private void ClearClassifications(object sender, RoutedEventArgs e)
        {
            border.ClearClassifications();


        }

        public List<CrossSectionRecord> GetClassifications(CrossSectionRecord[] RecordsArray, List<System.Windows.Shapes.Rectangle> RectList)
        {
            List<CrossSectionRecord> RecordsToUpdate = null;

            foreach (System.Windows.Shapes.Rectangle item in RectList)
            {
               
                {
                    if (RecordsToUpdate == null)
                        RecordsToUpdate = new List<CrossSectionRecord>();


                    item.Dispatcher.Invoke(() =>
                    {
                        RecordsToUpdate.AddRange(GetRecordsInRect(item.Margin.Left, item.Margin.Top, item.Width, item.Height, RecordsArray));
                    });
                    
                }
            }

            return RecordsToUpdate;
        }

        private List<CrossSectionRecord> GetRecordsInRect(double lft,double top, double width, double height, CrossSectionRecord[] RecordsArray)
        {
            List<CrossSectionRecord> resultList = new List<CrossSectionRecord>();

            var recInRect = from a in RecordsArray
                            where a.GlobalPositionX > lft
                            where a.GlobalPositionX < lft+width
                            where a.GlobalPositionY > top
                            where a.GlobalPositionY < top + height
                            select a;

            foreach (CrossSectionRecord item in recInRect)
            {
                item.ManualSurfaceClassification = 1;
            }

            resultList.AddRange(recInRect);

            return resultList;
        }
    }

}
