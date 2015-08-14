using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace EPADB {
    public class EpaAnalogData {
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public EPADB epa { get; set; }
        public List<string> tables { get; set; }
        public Dictionary<int, List<string>> SignalsBySubSys { get; set; }
        public ToolStripStatusLabel Status;
        public Dictionary<string, int> subSystemsByTables { get; set; }
        public int Step { get; set; }
        public Dictionary<int, Color> Colors;

        public EpaAnalogData(DateTime dateStart, DateTime dateEnd, EPADB epa, int step) {
            DateStart = dateStart;
            DateEnd = dateEnd;
            Step = step;
            this.epa = epa;
            Colors = new Dictionary<int, Color>();
            Colors.Add(0, Color.Black);
            Colors.Add(1, Color.Red);
            Colors.Add(2, Color.Blue);
            Colors.Add(3, Color.Green);
            Colors.Add(4, Color.Orange);
            Colors.Add(5, Color.DarkGray);
            Colors.Add(6, Color.DarkViolet);
            Colors.Add(7, Color.Brown);
        }

        public void processSignals() {
            SignalsBySubSys = new Dictionary<int, List<string>>();
            foreach (SignalInfo si in epa.SelectedAnalogSignals) {
                if (!si.discr) {
                    if (!SignalsBySubSys.ContainsKey(si.subSys)) {
                        SignalsBySubSys.Add(si.subSys, new List<string>());
                    }
                    SignalsBySubSys[si.subSys].Add(si.KKS);
                }
            }
        }

        public void readTables() {
            subSystemsByTables = new Dictionary<string, int>();
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            tables = new List<string>();
            foreach (int ss in SignalsBySubSys.Keys) {
                string tabName = String.Format("data_{0}_tabs", ss);
                SqlCommand com = con.CreateCommand();
                Status.Text = "Чтение списка таблиц с данными " + ss;
                com.CommandText = "Select * from " + tabName;
                SqlDataReader reader = com.ExecuteReader();
                while (reader.Read()) {
                    DateTime ds = EPADB.GetDate(reader.GetInt32(1));
                    DateTime de = EPADB.GetDate(reader.GetInt32(2));
                    string nm = reader.GetString(0);
                    if (nm.ToLower().Contains("tmp") || (de >= DateStart && ds <= DateEnd)
                        /*(ds < DateStart && !(de < DateStart)) || (de > DateEnd && !(ds > DateEnd)) || (ds >= DateStart && de <= DateEnd)*/ ) {
                        string stTab = reader.GetString(0);
                        tables.Add(stTab);
                        subSystemsByTables.Add(stTab, ss);
                    }
                }
                reader.Close();
            }
            con.Close();
        }

        public void readData(string fn,GraphPane graph) {
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            Int64 timeStart = EPADB.GetIntDate(DateStart);
            Int64 timeEnd = EPADB.GetIntDate(DateEnd);
            Dictionary<Int64, Dictionary<string, double>> Data = new Dictionary<long, Dictionary<string, double>>();
            
            Int64 time = timeStart;
            while (time <= timeEnd) {
                Data.Add(time, new Dictionary<string, double>());
                foreach (SignalInfo si in epa.SelectedAnalogSignals) {
                    Data[time].Add(si.KKS, double.NaN);
                }
                time += Step;
            }

            List<string> kksQList = new List<string>();
            List<int> timesQList = new List<int>();
            Dictionary<int, int> timesDict = new Dictionary<int, int>();
            List<int> timesList = new List<int>();
            foreach (int ss in SignalsBySubSys.Keys) {
                foreach (string table in tables) {
                    if (subSystemsByTables[table] != ss)
                        continue;
                    Status.Text = "Обработка таблицы " + table;
                    SqlCommand com = con.CreateCommand();
                    com.CommandText = String.Format("Select * from {0} where time_page>={1} and time_page<={2}", table.Replace("state", "time"), timeStart, timeEnd);
                    List<Int32> times = new List<int>();

                    try {
                        Status.Text += "---|";
                        SqlDataReader reader = com.ExecuteReader();
                        while (reader.Read()) {
                            try {
                                int tm = reader.GetInt32(0);
                                times.Add(tm);
                            }
                            catch { }
                        }
                        reader.Close();                        

                        foreach (string kks in SignalsBySubSys[ss]) {
                            kksQList.Add("'" + kks + "'");
                            if (kksQList.Count() <= 10 && kks != SignalsBySubSys[ss].Last())
                                continue;
                            string kksQ=String.Join(",", kksQList);
                            kksQList.Clear();
                            timesDict.Clear();
                            timesQList.Clear();

                            foreach (int t in Data.Keys) {
                                if (t >= times.First() && t <= times.Last()) {
                                    int valT = times.First(tempT => { return tempT >= t; });
                                    if (valT - t < Step) {
                                        timesQList.Add(valT);
                                        timesDict.Add(valT, t);
                                    }
                                }                                
                                if (timesQList.Count < 100 && t != Data.Keys.Last())
                                    continue;                     
                                string timesQ=String.Join(",", timesQList);
                                timesQList.Clear();

                                try {
                                    com = con.CreateCommand();
                                    com.CommandText = String.Format("Select kks_id_signal,time_page,data from {0} where time_page in ({2}) and kks_id_signal in ({1})", table, kksQ, timesQ);
                                    //com.CommandText = String.Format("Select kks_id_signal,time_page,data from {0} where time_page={2} and kks_id_signal = '{1}'", table, kks, valT);

                                    Status.Text += "---|";
                                    reader = com.ExecuteReader();
                                    while (reader.Read()) {
                                        try {
                                            int timeRes = reader.GetInt32(1);
                                            string kksAsu = reader.GetString(0);
                                            double val = reader.GetFloat(2);

                                            long resultTime = timesDict[timeRes];
                                            Data[resultTime][kksAsu] = val;
                                        }
                                        catch (Exception e) {Logger.Info(e.ToString()); }
                                    }
                                    reader.Close();
                                }
                                catch (Exception e) {Logger.Info(e.ToString()); }
                            }
                        }
                    }
                    catch (Exception e) { Logger.Info(e.ToString()); }
                }                
            }
            con.Close();

            Status.Text = "Чтение завершено";
            List<string> thAsuList = new List<string>();
            List<string> thTechList = new List<string>();            

            foreach (SignalInfo si in epa.SelectedAnalogSignals) {
                thAsuList.Add(String.Format("<th>{0}</th>", si.ShortName));
                try {
                    string kksTech = epa.ASUTechDict[si.KKS];
                    SignalInfo tech = epa.FindSignal(epa.TechRoot, kksTech, null);
                    thTechList.Add(String.Format("<th>{0}</th>", tech.ShortName));
                }
                catch {
                    thTechList.Add("<th>-</th>");
                }
            }
            OutputData.writeToOutput(fn, String.Format("<table border='1'><tr><th rowspan='2'>Дата</th>{0}</tr><tr>{1}</tr>",string.Join(" ",thAsuList),string.Join(" ",thTechList)));
            foreach (int tm in Data.Keys) {
                OutputData.writeToOutput(fn, String.Format("<tr><th>{0}</th><td>{1}</td></tr>", EPADB.GetDate(tm), String.Join("</td><td>", Data[tm].Values)));
            }
            OutputData.writeToOutput(fn,"</table>");

            graph.CurveList.Clear();
            graph.XAxis.Scale.Min = Data.Keys.First();
            graph.XAxis.Scale.Max = Data.Keys.Last();
            graph.XAxis.Scale.MinAuto = false;
            graph.XAxis.Scale.MaxAuto = false;
            graph.XAxis.Title.IsVisible = false;
            graph.YAxis.IsVisible = false;
            graph.YAxis.Title.IsVisible = false;
            graph.Legend.FontSpec.Size = 6;
            graph.Legend.Location.X = 0;
            graph.Legend.Location.Y = 0;
            graph.Title.IsVisible = false;
            graph.YAxis.Scale.FontSpec.Size = 6;
            graph.YAxis.IsVisible = false;
            graph.XAxis.Scale.FontSpec.Size = 6;
            int index = 0;
            foreach (SignalInfo si in epa.SelectedAnalogSignals) {
                try {
                    string name = si.ShortName;
                        int axInd=graph.AddYAxis("");
                        graph.YAxisList[axInd].Color = Colors[index % 8];
                        graph.YAxisList[axInd].Scale.FontSpec.Size = 6;
                        graph.YAxisList[axInd].Scale.FontSpec.FontColor = Colors[index % 8];

                    try {
                        string kksTech = epa.ASUTechDict[si.KKS];
                        SignalInfo tech = epa.FindSignal(epa.TechRoot, kksTech, null);
                        name = name + " (" + tech.ShortName + ")";
                    }
                    catch { }
                    PointPairList list = new PointPairList();
                    foreach (int tm in Data.Keys) {
                        list.Add(new PointPair(tm, Data[tm][si.KKS]));
                    }
                    graph.AddCurve(name, list, Colors[index % 8], SymbolType.None);
                    graph.CurveList[index].YAxisIndex = axInd;
                    
                }
                catch (Exception e) {
                    Logger.Info(e.ToString());
                }
                graph.AxisChange();
                index++;
            }
        }
    }
}

