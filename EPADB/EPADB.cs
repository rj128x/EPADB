using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EPADB {
    public class SignalInfo {
        public string KKS { get; set; }
        public string FullName { get; set; }
        public string ShortName { get; set; }
        public string ShortNameUp { get; set; }
        public string FullNameUp { get; set; }
        public List<SignalInfo> Children { get; set; }
        public TreeNode treeNode { get; set; }
        public int subSys { get; set; }
        public bool discr { get; set; }
        public string valueStep { get; set; }
        public int allChildrenCount { get; set; }
        public int idLevel { get; set; }
        public string kksDev { get; set; }
        public bool isLastTreeNode { get; set; }
        public int numSign { get; set; }
    }

    public class SignalGroup {
        public string KKSDev { get; set; }
        public int level { get; set; }
        public int parent { get; set; }
        public string name { get; set; }
    }

    public class EPADB {

        public List<SignalInfo> SelectedDiscrSignals;
        public List<SignalInfo> SelectedAnalogSignals;
        public Dictionary<string, List<SignalGroup>> Signals;
        public bool processingSelect;

        public ToolStripStatusLabel Status;
        public EPADB() {
            DBSettings.init();
            SelectedDiscrSignals = new List<SignalInfo>();
            SelectedAnalogSignals = new List<SignalInfo>();
        }

        public SignalInfo ASURoot { get; set; }
        public SignalInfo TechRoot { get; set; }
        public Dictionary<string, string> ASUTechDict;
        public Dictionary<string, string> TechASUDict;
        public Dictionary<string, bool> DictAlgo;
        public Dictionary<int, string> SubSystems;
        public Dictionary<int, string> SubSysTables;
        public Dictionary<string, int> NumSignals;
        public bool UseNumSignals;

        public void readASU() {
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            List<SignalInfo> asufull = new List<SignalInfo>();
            SqlCommand com = con.CreateCommand();
            Status.Text = "Чтение АСУ сигналов из БД";
            com.CommandText = "Select kks_id_signal, full_name,short_name,sub_sys,value_step,kks_id_dev,id_level from asu_signals_full";
            SqlDataReader reader = com.ExecuteReader();
            while (reader.Read()) {
                SignalInfo si = new SignalInfo();
                si.KKS = reader.GetString(0);
                si.FullName = reader.GetString(1);
                si.ShortName = reader.GetString(2);
                si.FullNameUp = si.FullName.ToUpper();
                si.ShortNameUp = si.ShortName.ToUpper();
                si.subSys = reader.GetInt32(3);
                si.valueStep = reader.GetString(4);
                si.kksDev = reader.GetString(5);
                si.idLevel = reader.GetInt32(6);
                try {
                    si.numSign = NumSignals[si.KKS];
                }
                catch { }
                asufull.Add(si);
            }
            reader.Close();
            NumSignals.Clear();
            SubSysTables.Clear();


            Status.Text = "Чтение АСУ дерева из БД";
            Dictionary<string, string> asu_tree = new Dictionary<string, string>();
            com.CommandText = "Select * from asu_tree";
            reader = com.ExecuteReader();
            while (reader.Read()) {
                string kks = reader.GetString(3);
                string name = reader.GetString(2);
                asu_tree.Add(kks, name);

            }
            reader.Close();
            con.Close();

            SignalInfo root = new SignalInfo();
            root.KKS = asu_tree.Keys.ToArray()[0];
            root.FullName = asu_tree.Values.ToArray()[0];
            createTree(root, asu_tree);
            Status.Text = "Формирование АСУ дерева";
            fillTree(root, asufull);
            Status.Text = "Формирование АСУ групп";
            fillSubTree(root);
            ASURoot = root.Children[0];
        }


        public void readTech() {
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            List<SignalInfo> techfull = new List<SignalInfo>();
            SqlCommand com = con.CreateCommand();
            com.CommandText = "Select * from tech_parameters_full";
            Status.Text = "Чтение Тех сигналов из БД";
            SqlDataReader reader = com.ExecuteReader();
            while (reader.Read()) {
                SignalInfo si = new SignalInfo();
                si.KKS = reader.GetString(0);
                si.FullName = reader.GetString(1);
                si.ShortName = reader.GetString(2);
                si.FullNameUp = si.FullName.ToUpper();
                si.ShortNameUp = si.ShortName.ToUpper();
                techfull.Add(si);
            }
            reader.Close();

            Status.Text = "Чтение Тех дерева из БД";
            Dictionary<string, string> tech_tree = new Dictionary<string, string>();
            com.CommandText = "Select * from tech_tree";
            reader = com.ExecuteReader();
            while (reader.Read()) {
                string kks = reader.GetString(3);
                string name = reader.GetString(2);
                tech_tree.Add(kks, name);
            }
            reader.Close();
            con.Close();

            SignalInfo root = new SignalInfo();
            root.KKS = tech_tree.Keys.ToArray()[0];
            root.FullName = tech_tree.Values.ToArray()[0];
            Status.Text = "Формирование Тех дерева";
            createTree(root, tech_tree);
            fillTree(root, techfull);
            TechRoot = root.Children[0];
        }

        public string getSubSysName(SignalInfo si) {
            if (si.subSys > 0) {
                try {
                    return SubSystems[si.subSys];
                }
                catch { }
            }
            return "";
        }

        public void linkSignals() {
            ASUTechDict = new Dictionary<string, string>();
            TechASUDict = new Dictionary<string, string>();
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            SqlCommand com = con.CreateCommand();
            Status.Text = "Чтение связанных сигналов";
            com.CommandText = "Select * from link_signals where kks_id_param is not null and kks_id_signal is not null";
            SqlDataReader reader = com.ExecuteReader();
            while (reader.Read()) {
                string kksAsu = reader.GetString(2);
                string kksTech = reader.GetString(1);
                try {
                    ASUTechDict.Add(kksAsu, kksTech);
                }
                catch {
                    ASUTechDict[kksAsu] += ";" + kksTech;
                }
                try {
                    TechASUDict.Add(kksTech, kksAsu);
                }
                catch {
                    TechASUDict[kksTech] += ";" + kksAsu;
                }

            }
            reader.Close();
            con.Close();
        }

        public void readAlgoritms() {
            DictAlgo = new Dictionary<string, bool>();
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            SqlCommand com = con.CreateCommand();
            Status.Text = "Чтение алгоритмов";
            com.CommandText = "Select * from algorithm_collection_binary";
            SqlDataReader reader = com.ExecuteReader();
            while (reader.Read()) {
                string kks = reader.GetString(1);
                try {
                    DictAlgo.Add(kks, true);
                }
                catch { }
            }
            reader.Close();
            con.Close();
        }

        public void readSubSystems() {
            SubSystems = new Dictionary<int, string>();
            SubSysTables = new Dictionary<int, string>();
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            SqlCommand com = con.CreateCommand();
            Status.Text = "Чтение подсистем";
            com.CommandText = "Select * from Subsys_collection";
            SqlDataReader reader = com.ExecuteReader();
            while (reader.Read()) {
                int subSys = reader.GetInt32(0);
                string name = reader.GetString(3);
                string tab = reader.GetString(2);
                try {
                    SubSystems.Add(subSys, name);
                    SubSysTables.Add(subSys, tab);
                }
                catch { }
            }
            reader.Close();

            NumSignals = new Dictionary<string, int>();
            foreach (string tabName in SubSysTables.Values) {
                Status.Text = "Чтение подсистем "+tabName;
                try {
                    com.CommandText = String.Format("Select * from {0}", tabName);
                    reader = com.ExecuteReader();
                    while (reader.Read()) {
                        int num = reader.GetInt32(1);
                        string kks = reader.GetString(0);
                        NumSignals.Add(kks, num);
                    }
                }
                finally {
                    reader.Close();
                }
            }
            
            con.Close();


        }

        public void readkksDev() {
            SubSystems = new Dictionary<int, string>();
            SqlConnection con = DBSettings.getConnection();
            con.Open();
            SqlCommand com = con.CreateCommand();
            Status.Text = "Чтение групп сигналов";
            com.CommandText = "Select * from ASU_signal_group";
            SqlDataReader reader = com.ExecuteReader();
            Signals = new Dictionary<string, List<SignalGroup>>();
            while (reader.Read()) {
                SignalGroup gr = new SignalGroup();
                gr.KKSDev = reader.GetString(0);
                gr.name = reader.GetString(3);
                try {
                    gr.level = reader.GetInt32(1);
                }
                catch {
                    gr.level = -1;
                }
                try {
                    gr.parent = reader.GetInt32(2);
                }
                catch {
                    gr.parent = -1;
                }
                if (!Signals.ContainsKey(gr.KKSDev)) {
                    Signals.Add(gr.KKSDev, new List<SignalGroup>());
                }
                Signals[gr.KKSDev].Add(gr);
            }
            reader.Close();
            con.Close();
        }


        public SignalInfo FindSignal(SignalInfo root, string kks, SignalInfo found) {
            if (root.Children != null) {
                foreach (SignalInfo ch in root.Children) {
                    if (ch.KKS == kks) {
                        found = ch;
                        break;
                    }
                    if (found == null)
                        found = FindSignal(ch, kks, found);
                }
            }
            return found;
        }


        public void createTree(SignalInfo root, Dictionary<string, string> tree) {
            foreach (KeyValuePair<string, string> de in tree) {
                SignalInfo ch = new SignalInfo();
                ch.KKS = de.Key;
                ch.FullName = de.Value;
                ch.ShortName = de.Value;
                ch.FullNameUp = ch.FullName.ToUpper();
                ch.ShortNameUp = ch.ShortName.ToUpper();
                if (isChild(root.KKS, ch.KKS, tree)) {
                    if (root.Children == null)
                        root.Children = new List<SignalInfo>();
                    root.Children.Add(ch);
                    createTree(ch, tree);
                }
            }
            if (root.Children == null)
                root.isLastTreeNode = true;
        }

        public void fillTree(SignalInfo root, List<SignalInfo> full) {
            if (root.Children != null) {
                foreach (SignalInfo ch in root.Children) {
                    fillTree(ch, full);
                    root.allChildrenCount += ch.allChildrenCount;
                }

            }
            else {
                string msk = root.KKS.Replace("_", "");
                foreach (SignalInfo si in full) {
                    if (si.KKS.Contains(msk)) {
                        if (root.Children == null) {
                            root.Children = new List<SignalInfo>();
                        }
                        root.Children.Add(si);
                        root.allChildrenCount++;
                    }
                }
            }
        }

        public void fillSubTree(SignalInfo root) {
            if (!root.isLastTreeNode) {
                foreach (SignalInfo ch in root.Children) {
                    fillSubTree(ch);
                }
            }
            else if (root.Children!=null) {
                SignalInfo subRoot = new SignalInfo();
                subRoot.Children = new List<SignalInfo>();
                List<SignalInfo> forDel = new List<SignalInfo>();
                foreach (SignalInfo si in root.Children) {
                    if (!Signals.ContainsKey(si.kksDev))
                        continue;
                    List<SignalGroup> groups = Signals[si.kksDev];
                    List<SignalGroup> subTree = new List<SignalGroup>();
                    try {
                        int level = si.idLevel;
                        int index = 0;
                        while (index < groups.Count) {
                            SignalGroup gr = groups[index];
                            if (gr.level == level) {
                                subTree.Add(gr);
                                level = gr.parent;
                                index = -1;
                            }
                            index++;
                        }
                    }
                    catch { }

                    try {
                        if (subTree.Count > 0) {
                            SignalInfo siTemp = subRoot;
                            for (int i = subTree.Count - 1; i >= 0; i--) {
                                SignalGroup gr = subTree[i];
                                siTemp = fillSubRoot(siTemp, gr);
                            }
                            if (siTemp.Children == null)
                                siTemp.Children = new List<SignalInfo>();
                            siTemp.Children.Add(si);
                            forDel.Add(si);
                        }
                    }
                    catch { }
                }
                foreach (SignalInfo del in forDel) {
                    root.Children.Remove(del);
                }
                foreach (SignalInfo add in subRoot.Children) {
                    bool found = false;
                    foreach (SignalInfo s in root.Children)
                        if (s.KKS == add.KKS) {
                            found = true;
                            break;
                        }
                    if (!found) {
                        root.Children.Add(add);
                    }
                }
            }
        }

        public SignalInfo fillSubRoot(SignalInfo root, SignalGroup group) {
            string kks = group.KKSDev + "-" + group.level;
            if (root.Children == null)
                root.Children = new List<SignalInfo>();
            foreach (SignalInfo ch in root.Children) {
                if (ch.KKS == kks)
                    return ch;
            }
            SignalInfo si = new SignalInfo();
            si.KKS = kks;
            si.ShortName = group.name;
            si.FullName = group.name;
            si.FullNameUp = si.FullName.ToUpper();
            si.ShortNameUp = si.ShortName.ToUpper();
            root.Children.Add(si);
            return si;
        }


        public bool isChild(string kks, string kksCh, Dictionary<string, string> tree) {
            if (kks == kksCh)
                return false;
            string msk = kks.Replace("_", "");
            string mskCh = kksCh.Replace("_", "");
            if (!kksCh.Contains(msk))
                return false;
            foreach (KeyValuePair<string, string> de in tree) {
                if (de.Key == kks || de.Key == kksCh)
                    continue;
                if (de.Key.Contains(msk) && kksCh.Contains(de.Key.Replace("_", "")))
                    return false;
            }
            return true;
        }

        public TreeNode displayTree(SignalInfo si) {
            TreeNode node = new TreeNode();
            node.Text = si.ShortName;
            node.ToolTipText = si.FullName;
            node.Name = si.KKS;
            si.discr = getSubSysName(si).Contains("Дискр");
            if (TechASUDict.ContainsKey(si.KKS) || ASUTechDict.ContainsKey(si.KKS)) {
                node.ForeColor = Color.Blue;
            }

            if (DictAlgo.ContainsKey(si.KKS)) {
                node.Text += " [*]";
            }
            si.treeNode = node;
            if (si.Children != null) {
                foreach (SignalInfo ch in si.Children) {
                    TreeNode nodeCh = displayTree(ch);
                    node.Nodes.Add(nodeCh);
                }
            }
            return node;
        }

        public void findText(SignalInfo root, string text, List<SignalInfo> res) {
            if (root.Children != null) {
                foreach (SignalInfo ch in root.Children) {
                    if (ch.KKS.Contains(text) || ch.FullNameUp.Contains(text) || (ch.ShortNameUp.Contains(text))) {
                        res.Add(ch);
                        Status.Text = String.Format("Найдено {0} сигналов", res.Count);
                    }
                    findText(ch, text, res);
                }
            }
        }

        public void selectTree(TreeNode node, List<SignalInfo> res) {
            node.BackColor = Color.White;
            foreach (SignalInfo si in res) {
                if (node.Name == si.KKS) {
                    node.EnsureVisible();
                    node.BackColor = Color.DarkGray;
                }
            }
            foreach (TreeNode ch in node.Nodes) {
                selectTree(ch, res);
            }
        }

        public void cleartTree(TreeNode node) {
            node.BackColor = Color.White;
            foreach (TreeNode ch in node.Nodes) {
                cleartTree(ch);
            }
            node.Checked = false;
        }

        public static DateTime GetDate(Int64 time) {
            return new DateTime(1970, 1, 1).AddSeconds(time).AddHours(5);
        }

        public static Int64 GetIntDate(DateTime date) {
            return (date.AddHours(-5).Ticks - (new DateTime(1970, 1, 1)).Ticks) / 10000000;
        }

        public static string getSignalState(int state, SignalInfo si) {
            try {
                string[] vals = si.valueStep.Split(new char[] { ';' });
                if (vals.Length > 1) {
                    return vals[state];
                }
            }
            catch { }
            switch (state) {
                case 0:
                    return "Неопр";
                case 1:
                    return "Откл";
                case 2:
                    return "Вкл";
                case 3:
                    return "Ошибка";
            }
            return "-";
        }

        public static string getSignalSrc(int src) {
            switch (src) {
                case 0:
                    return "Устройство";
                case 1:
                    return "ФК";
                case 2:
                    return "Сервер";
                case 3:
                    return "Модель";
                case 4:
                    return "Логика";
                case 5:
                    return "Логика ФК";
                case 8:
                    return "Устройство в ремонт";
                case 16:
                    return "АРМ";

            }
            return "-";
        }

        public void selectAsuSignal(SignalInfo si, bool check, bool selectLink) {
            si.treeNode.Checked = check;
            if (si.Children != null) {
                foreach (SignalInfo ch in si.Children) {
                    selectAsuSignal(ch, check, selectLink);
                }
            }
            else {
                if (check) {
                    if (si.discr)
                        SelectedDiscrSignals.Add(si);
                    else
                        SelectedAnalogSignals.Add(si);
                }
                else {
                    if (si.discr)
                        SelectedDiscrSignals.Remove(si);
                    else
                        SelectedAnalogSignals.Remove(si);
                }

                if (selectLink) {
                    try {
                        string kksT = ASUTechDict[si.KKS];
                        string[] kksL = kksT.Split(new char[] { ';' });
                        foreach (string KKSTech in kksL) {
                            SignalInfo siTech = FindSignal(TechRoot, KKSTech, null);
                            siTech.treeNode.Checked = check;
                        }
                    }
                    catch { }
                }
            }
        }

        public void selectTechSignal(SignalInfo si, bool check) {
            si.treeNode.Checked = check;
            if (si.Children != null) {
                foreach (SignalInfo ch in si.Children) {
                    selectTechSignal(ch, check);
                }
            }
            try {
                string kksA = TechASUDict[si.KKS];
                string[] kksL = kksA.Split(new char[] { ';' });
                foreach (string KKSAsu in kksL) {
                    SignalInfo siAsu = FindSignal(ASURoot, KKSAsu, null);
                    siAsu.treeNode.Checked = check;
                    if (check) {
                        if (siAsu.discr)
                            SelectedDiscrSignals.Add(siAsu);
                        else
                            SelectedAnalogSignals.Add(siAsu);
                    }
                    else {
                        if (siAsu.discr)
                            SelectedDiscrSignals.Remove(siAsu);
                        else
                            SelectedAnalogSignals.Remove(siAsu);
                    }
                }
            }
            catch { }
        }
    }
}