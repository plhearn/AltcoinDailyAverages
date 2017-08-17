using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarketDaily
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();

        public List<float> points = new List<float>();
        private Timer timer1;
        public int interval = 15000;
        int ticks = 0;
        int numUpdates = 0;


        protected struct coinData
        {
            public string name;
            public double marketCap;
            public double price;
            public double volume;
            public double fiveMin;
            public double hr;
            public double day;
            public double week;
            public int posTicks;
            public int negTicks;
            public List<int> posTicks10;
            public List<int> negTicks10;
            public double upRatio;
            public double upRatio10;
        }

        protected struct chartPoint
        {
            public double price;
            public double sma;
            public double lrPoint;
        }

        protected struct chartData
        {
            public string symbol;
            public List<chartPoint> points;
        }

        protected struct snapShot
        {
            public DateTime timeStamp;
            public List<coinData> coins;
        }


        List<snapShot> snapShots = new List<snapShot>();
        List<chartData> charts = new List<chartData>();


        public Form1()
        {
            InitializeComponent();

            DataGridViewCheckBoxColumn checkColumn = new DataGridViewCheckBoxColumn();
            checkColumn.Name = "Visible";
            checkColumn.HeaderText = "Visible";
            checkColumn.Width = 50;
            checkColumn.ReadOnly = false;
            checkColumn.FillWeight = 10;
            checkColumn.TrueValue = true;
            checkColumn.FalseValue = false;
            dataGridView1.Columns.Add(checkColumn);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (timer1 == null)
            {
                //InitTimer();
                button1.Text = "Stop";
                getHttpBittrex();
                //timer1_Tick(new object(), new EventArgs());
            }
            else
            {
                //timer1.Stop();
                timer1 = null;
                button1.Text = "Start";
            }
        }

        public void InitTimer()
        {
            timer1 = new Timer();
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Interval = interval; // in miliseconds
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            ticks++;

            getHttpBittrex();
        }
        
        public async void getHttpCC(string symbol)
        {
            label1.Text = "Updating " + symbol;

            var responseString = "";

            try
            {
                responseString = await client.GetStringAsync("https://min-api.cryptocompare.com/data/histoday?fsym=" + symbol + "&tsym=BTC&limit=60&aggregate=3&e=CCCAGG");
            }
            catch (TaskCanceledException e1)
            {
                //txtStatus.Text = e1.Message + "\n" + txtStatus.Text;
                return;
            }
            catch (HttpRequestException e1)
            {
                //txtStatus.Text = e1.Message + "\n" + txtStatus.Text;
                return;
            }
            

            string phrase = "{\"Response\":\"Success\",\"Type\":100,\"Aggregated\":true,\"Data\":";
            int startIdx = responseString.IndexOf(phrase) + phrase.Length;
            phrase = "]";
            int endIdx = responseString.IndexOf(phrase);
            responseString = responseString.Substring(startIdx, endIdx - startIdx);

            //Debug.Print(responseString);


            chartData s;

            s.symbol = symbol;
            s.points = new List<chartPoint>();


            while (responseString.Contains("{\"time\":"))
            {
                chartPoint c;

                phrase = ",\"close\":";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = ",\"high\":";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strClose = responseString.Substring(startIdx, endIdx - startIdx);
                double close = 0;
                double.TryParse(strClose, out close);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                
                c.price = close;
                c.sma = 0;
                c.lrPoint = 0;

                s.points.Add(c);

            }

            charts.Add(s);


            label1.Text = "";

            if(charts.Count == snapShots[0].coins.Count)
                updateGraph();
        }


        public async void getHttpBittrex()
        {
            label1.Text = "Updating";

            var responseString = "";

            try
            {
                responseString = await client.GetStringAsync("https://bittrex.com/api/v1.1/public/getmarketsummaries");
            }
            catch (TaskCanceledException e1)
            {
                //txtStatus.Text = e1.Message + "\n" + txtStatus.Text;
                return;
            }
            catch (HttpRequestException e1)
            {
                //txtStatus.Text = e1.Message + "\n" + txtStatus.Text;
                return;
            }

            if (snapShots.Count > 0)
                numUpdates++;

            List<string> checkedCoins = new List<string>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[2];

                if (chk.Value == chk.TrueValue)
                    checkedCoins.Add(row.Cells[0].Value.ToString());
            }

            dataGridView1.Rows.Clear();

            //Debug.Print(responseString);

            string phrase = "{\"success\":true,\"message\":\"\",\"result\":[";
            int startIdx = responseString.IndexOf(phrase) + phrase.Length;
            phrase = "]}";
            int endIdx = responseString.IndexOf(phrase);
            responseString = responseString.Substring(startIdx, endIdx - startIdx);

            //Debug.Print(responseString);

            snapShot s;

            s.timeStamp = DateTime.Now;
            s.coins = new List<coinData>();


            while (responseString.Contains("{\"MarketName\""))
            {
                coinData c;
                c.posTicks10 = new List<int>();
                c.negTicks10 = new List<int>();

                phrase = "{\"MarketName\":\"";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = "\",";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string coinName = responseString.Substring(startIdx, endIdx - startIdx);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "\"High\":";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = ",";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strHigh = responseString.Substring(startIdx, endIdx - startIdx);
                double high = 0;
                double.TryParse(strHigh, out high);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "\"Low\":";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = ",";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strLow = responseString.Substring(startIdx, endIdx - startIdx);
                double low = 0;
                double.TryParse(strLow, out low);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "\"Volume\":";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = ",";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strVolume = responseString.Substring(startIdx, endIdx - startIdx);
                double volume = 0;
                double.TryParse(strVolume, out volume);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "\"Last\":";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = ",";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strLast = responseString.Substring(startIdx, endIdx - startIdx);
                double last = 0;
                double.TryParse(strLast, out last);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "\"BaseVolume\":";
                startIdx = responseString.IndexOf(phrase) + phrase.Length;
                phrase = ",";
                endIdx = responseString.IndexOf(phrase);

                if (endIdx < 0)
                    endIdx = 10;
                string strBaseVol = responseString.Substring(startIdx, endIdx - startIdx);
                double baseVol = 0;
                double.TryParse(strBaseVol, out baseVol);
                responseString = responseString.Substring(endIdx + phrase.Length, responseString.Length - (endIdx + phrase.Length));

                phrase = "}";
                startIdx = responseString.IndexOf(phrase);
                responseString = responseString.Substring(startIdx + phrase.Length, responseString.Length - (startIdx + phrase.Length));


                c.name = coinName.Replace("BTC-", "");
                c.marketCap = baseVol;
                c.price = last;
                c.volume = baseVol;
                c.hr = 0;
                c.day = 0;
                c.week = 0;

                c.fiveMin = 0;
                c.posTicks = 0;
                c.negTicks = 0;
                c.upRatio = 0;
                c.upRatio10 = 0;

                if (snapShots.Count > 0)
                {
                    foreach (coinData d in snapShots[snapShots.Count - 1].coins)
                        if (d.name == c.name)
                        {
                            c.fiveMin = d.fiveMin + ((last - d.price) / last);

                            c.posTicks = d.posTicks;
                            c.negTicks = d.negTicks;
                            c.posTicks10 = d.posTicks10;
                            c.negTicks10 = d.negTicks10;

                            if (c.fiveMin > 0)
                            {
                                c.posTicks++;
                                c.posTicks10.Add(1);
                                c.negTicks10.Add(0);
                            }
                            else if (c.fiveMin < 0)
                            {
                                c.negTicks++;
                                c.posTicks10.Add(0);
                                c.negTicks10.Add(1);
                            }
                            else
                            {
                                c.posTicks10.Add(0);
                                c.negTicks10.Add(0);
                            }

                            if (c.posTicks10.Count > 10)
                                c.posTicks10.RemoveAt(0);

                            if (c.negTicks10.Count > 10)
                                c.negTicks10.RemoveAt(0);

                            c.upRatio = (c.posTicks - c.negTicks) / (double)numUpdates;

                            int pos10sum = 0;
                            int neg10sum = 0;

                            foreach (int i in c.posTicks10)
                                pos10sum += i;

                            foreach (int i in c.negTicks10)
                                neg10sum += i;

                            c.upRatio10 = (pos10sum - neg10sum) / Math.Min((double)c.posTicks10.Count, 10.0);
                        }
                }


                if (c.volume > 300 &&
                    !c.name.Contains("ETH-") &&
                    !c.name.Contains("USDT-") &&
                    !c.name.Contains("CNY-"))
                {
                    s.coins.Add(c);

                    getHttpCC(c.name);

                    dataGridView1.Rows.Add(c.name, c.fiveMin);
                    

                }



            }

            //dataGridView1.Sort(dataGridView1.Columns[1], ListSortDirection.Descending);


            snapShots.Add(s);

            if (snapShots.Count > 400)
                snapShots.RemoveAt(0);






            if (ticks == 1)
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[2];
                    chk.Value = chk.TrueValue;
                }
            else
                foreach (string str in checkedCoins)
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        if (row.Cells[0].Value.ToString() == str)
                        {
                            DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[2];
                            chk.Value = chk.TrueValue;
                        }
                    }
                }
            

            label1.Text = "";
        }
        private void updateGraph()
        {
            ChartAreas();
            ChartSeries();

            chart1.Invalidate();
        }


        private void ChartAreas()
        {
            if (charts.Count == 0)
                return;

            var axisX = new System.Windows.Forms.DataVisualization.Charting.Axis
            {
                Interval = 1,
            };

            double min = double.MaxValue;
            double max = 0;

            if (points.Count > 0)
            {
                //min = (int)points.Min();
                //max = (int)points.Max();
            }

            min = 0;
            max = 0.00000001;


            foreach (chartData s in charts)
            {

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[2];

                    if (row.Cells[0].Value.ToString() ==s.symbol && chk.Value == chk.TrueValue)
                        foreach (chartPoint c in s.points)
                        {
                            if (c.price > max)
                                max = Math.Round((double)c.price, 8);

                            if (c.price < min && c.price > 0)
                                min = Math.Round((double)c.price, 8);
                        }

                }

            }

            //min = -1;
            //max = 1;

            var axisY = new System.Windows.Forms.DataVisualization.Charting.Axis
            {
                Minimum = min,
                Maximum = max,
                Title = "price in BTC",
            };

            var chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea
            {
                AxisX = axisX,
                AxisY = axisY,
            };

            chartArea1.AxisX.LabelStyle.Format = "dd/MMM\nhh:mm";
            //chartArea1.AxisX.LabelStyle.Format = "hh:mm";


            chartArea1.AxisX.MajorGrid.Enabled = false;
            chartArea1.AxisY.MajorGrid.Enabled = false;
            chartArea1.AxisX.LabelStyle.Enabled = false;

            this.chart1.ChartAreas.Clear();
            this.chart1.ChartAreas.Add(chartArea1);
        }


        private void ChartSeries()
        {
            chart1.Series.Clear();

            for (int i = 0; i < charts.Count; i++)
            {
                if (charts[i].symbol == "BAT")
                    i = i;

                var series1 = new System.Windows.Forms.DataVisualization.Charting.Series
                {
                    Name = charts[i].symbol,
                    Color = Color.FromArgb(255, (i * 17) % 255, (i * 31) % 255, (i * 61) % 255),
                    BorderWidth = 1,
                    IsXValueIndexed = false,
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line,
                };

                for (int j = 0; j < charts[i].points.Count; j++)
                {
                    series1.Points.AddXY(j, charts[i].points[j].price);

                }
                
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    DataGridViewCheckBoxCell chk = (DataGridViewCheckBoxCell)row.Cells[2];

                    if (row.Cells[0].Value.ToString() == series1.Name &&
                        chk.Value == chk.TrueValue)
                        chart1.Series.Add(series1);
                }
            }
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            updateGraph();
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
    }
}
