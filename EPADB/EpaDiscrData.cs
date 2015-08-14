using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EPADB {
    public class EpaDiscrData {
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public EPADB epa { get; set; }
        public List<string> tables { get; set; }
        public Dictionary<string, int> subSystemsByTables { get; set; }
        public Dictionary<int, List<string>> tablesState { get; set; }
        public Dictionary<int, List<string>> SignalsBySubSys { get; set; }
        public ToolStripStatusLabel Status;
        

        public EpaDiscrData(DateTime dateStart, DateTime dateEnd, EPADB epa) {
            DateStart = dateStart;
            DateEnd = dateEnd;
            this.epa = epa;

        }

        public void processSignals() {
            SignalsBySubSys = new Dictionary<int, List<string>>();
            foreach (SignalInfo si in epa.SelectedDiscrSignals) {
                if (si.discr) {
                    if (!SignalsBySubSys.ContainsKey(si.subSys)) {
                        SignalsBySubSys.Add(si.subSys, new List<string>());
                    }
                    SignalsBySubSys[si.subSys].Add(si.KKS);
                }
            }
        }


        public void readTables() {
            List<int> DiscrSubSystems = new List<int>();
            subSystemsByTables = new Dictionary<string, int>();
            foreach (KeyValuePair<int, string> de in epa.SubSystems) {
                if (de.Value.Contains("Дискр"))
                    DiscrSubSystems.Add(de.Key);
                if (epa.SelectedDiscrSignals.Count > 0) {
                    if (!SignalsBySubSys.ContainsKey(de.Key)) {
                        DiscrSubSystems.Remove(de.Key);
                    }
                }
            }
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            tables = new List<string>();
            foreach (int ss in DiscrSubSystems) {
                string tabName = String.Format("data_{0}_tabs", ss);
                SqlCommand com = con.CreateCommand();
                Status.Text = "Чтение списка таблиц с данными " + ss;
                com.CommandText = "Select * from " + tabName;
                SqlDataReader reader = com.ExecuteReader();
                while (reader.Read()) {
                    DateTime ds = EPADB.GetDate(reader.GetInt32(1));
                    DateTime de = EPADB.GetDate(reader.GetInt32(2));
                    string nm = reader.GetString(0);
                    if (nm.ToLower().Contains("tmp") || (de>=DateStart && ds<=DateEnd)
                        /*(ds < DateStart && !(de < DateStart)) || (de > DateEnd && !(ds > DateEnd)) || (ds >= DateStart && de <= DateEnd)*/ ) {
                        string evTab = reader.GetString(6);
                        tables.Add(evTab);
                        subSystemsByTables.Add(evTab, ss);
                        /*string stTab = reader.GetString(0);
                        tables.Add(stTab);*/
                    }
                }
                reader.Close();
            }
            con.Close();
        }

        public void readTablesStates(DateTime date) {
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            tablesState = new Dictionary<int, List<string>>();
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
                    if (nm.ToLower().Contains("tmp") || (date >= ds && date <= de)) {
                        string stTab = reader.GetString(0);
                        if (!tablesState.ContainsKey(ss))
                            tablesState.Add(ss, new List<string>());
                        tablesState[ss].Add(stTab);
                    }
                }
                reader.Close();
            }
            con.Close();
        }

        public void readData(string fn) {
            
            OutputData.writeToOutput(fn, "<table border='1'><tr><th>Дата</th><th>kks_asu</th><th>kks_tech</th><th>name_asu</th><th>name_tech</th><th>state</th><th>source</th><th>subsys</th></tr>");
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            Int64 timeStart = EPADB.GetIntDate(DateStart);
            Int64 timeEnd = EPADB.GetIntDate(DateEnd);
            Dictionary<int, string> colors = new Dictionary<int, string>();
            colors.Add(0, "white");
            colors.Add(1, "PaleGreen");
            colors.Add(2, "LightSkyBlue");
            colors.Add(3, "LightCoral");
            SortedList<DateTime, string> outputData = new SortedList<DateTime, string>();
            foreach (string tab in tables) {
                SqlCommand com = con.CreateCommand();
                Status.Text = "Чтение данных из таблицы " + tab;
                int index = 0;
                try {
                    com.CommandText = String.Format("Select time,mcs,data,kks_id_signal,bsrc from {0} where time>={1} and time<={2}", tab, timeStart, timeEnd);
                    if (epa.SelectedDiscrSignals.Count > 0) {
                        int ss = subSystemsByTables[tab];
                        List<string> kksList = new List<string>();
                        foreach (string kks in SignalsBySubSys[ss]) {
                            kksList.Add("'" + kks + "'");
                        }
                        com.CommandText += String.Format(" and kks_id_signal in ({0})", String.Join(",", kksList));
                    }
                    SqlDataReader reader = com.ExecuteReader();
                    while (reader.Read()) {
                        try {
                            index++;
                            if (index % 1000 == 0)
                                Status.Text += "...|";
                            DateTime dt = EPADB.GetDate(reader.GetInt32(0));
                            string kks = reader.GetString(3);
                            int state = Int32.Parse(reader[2].ToString());
                            int src = Int32.Parse(reader[4].ToString());
                            int mcs = Int32.Parse(reader[1].ToString());
                            dt = dt.AddMilliseconds(mcs / 1000.0);
                            SignalInfo siAsu = epa.FindSignal(epa.ASURoot, kks, null);
                            SignalInfo siTech = null;
                            try {
                                string kksTech = epa.ASUTechDict[kks];
                                siTech = epa.FindSignal(epa.TechRoot, kksTech, null);
                            }
                            catch { }
                            string color = "white";
                            try {
                                color = colors[state];
                            }
                            catch { }
                            string outStr = String.Format("<tr bgColor='{0}'><th>{1}</th><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td><td>{6}</td><td>{7}</td><td>{8}</td></tr>", color,
                                dt.ToString("dd.MM.yyyy HH:mm:ss,fff"), kks, siTech == null ? "-" : siTech.KKS, siAsu.ShortName, siTech == null ? "-" : siTech.ShortName,
                                EPADB.getSignalState(state, siAsu), EPADB.getSignalSrc(src), siAsu.subSys);
                            if (!outputData.ContainsKey(dt)) {
                                outputData.Add(dt, "");
                            }
                            outputData[dt] += "\r\n" + outStr;

                        }
                        catch (Exception e) {
                            Logger.Info(e.ToString());
                        }
                    }
                    reader.Close();
                }
                catch { }
            }
            con.Close();
            foreach (string str in outputData.Values) {
                OutputData.writeToOutput(fn, str);
            }
            OutputData.writeToOutput(fn, "</table>");
            Status.Text = "Отчет сформирован";
        }

        public void readState(DateTime date, DataGridView grid) {
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            Int64 time = EPADB.GetIntDate(date);
            grid.Rows.Clear();
            Dictionary<string, int> gridRows = new Dictionary<string, int>();
            Dictionary<string, SignalInfo> signals = new Dictionary<string, SignalInfo>();

            foreach (SignalInfo siAsu in epa.SelectedDiscrSignals) {
                try {
                    int i = grid.Rows.Add();
                    SignalInfo siTech = null;
                    try {
                        string kksTech = epa.ASUTechDict[siAsu.KKS];
                        siTech = epa.FindSignal(epa.TechRoot, kksTech, null);
                    }
                    catch { }

                    grid.Rows[i].Cells[0].Value = siAsu.ShortName;
                    grid.Rows[i].Cells[1].Value = siTech != null ? siTech.ShortName : "-";
                    grid.Rows[i].Cells[2].Value = "-";
                    grid.Rows[i].Cells[3].Value = "-";
                    grid.Rows[i].Cells[4].Value = "-";

                    gridRows.Add(siAsu.KKS, i);
                    signals.Add(siAsu.KKS, siAsu);
                }
                catch { }
            }

            foreach (int subSys in tablesState.Keys) {
                Status.Text = "Чтение данных подсистемы " + subSys;
                foreach (string table in tablesState[subSys]) {
                    try {
                        SqlCommand com = con.CreateCommand();
                        com.CommandText = String.Format("Select top 1 time_page from {0} where time_page>={1}", table.Replace("state", "time"), time);
                        Object timeObj = com.ExecuteScalar();
                        if ((Int32)timeObj <=0)
                            continue;
                        List<string> kksQList = new List<string>();
                        foreach (string kks in SignalsBySubSys[subSys]) {
                            try {                                
                                kksQList.Add("'" + kks + "'");
                                if (kksQList.Count < 10 && kks != SignalsBySubSys[subSys].Last())
                                    continue;
                                string kksQ = string.Join(",", kksQList);
                                kksQList.Clear();

                                Status.Text = Status.Text+"...|";
                                com.CommandText = String.Format("Select  time_page,time,kks_id_signal,mcs,data,bsrc from {0} where time_page={1} and kks_id_signal in ({2})", table, timeObj, kksQ);
                                SqlDataReader reader = com.ExecuteReader();
                                while (reader.Read()) {

                                    DateTime dt = EPADB.GetDate(reader.GetInt32(0));
                                    DateTime dt_prev = EPADB.GetDate(reader.GetInt32(1));
                                    string kksAsu = reader.GetString(2);
                                    int state = Int32.Parse(reader[4].ToString());
                                    int src = Int32.Parse(reader[5].ToString());
                                    int mcs = Int32.Parse(reader[3].ToString());
                                    dt = dt.AddMilliseconds(mcs / 1000.0);

                                    int index = gridRows[kksAsu];
                                    grid.Rows[index].Cells[2].Value = dt_prev.ToString("dd.MM.yyyy HH:mm:ss,fff");
                                    grid.Rows[index].Cells[3].Value = EPADB.getSignalState(state, signals[kksAsu]);
                                    grid.Rows[index].Cells[4].Value = EPADB.getSignalSrc(src);

                                }
                                reader.Close();
                            }
                            catch (Exception e) { Logger.Info(e.ToString()); }
                        }
                    }
                    catch (Exception e) { Logger.Info(e.ToString()); }
                }
            }
            Status.Text = "Готово ";
        }
    }
}
