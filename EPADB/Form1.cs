using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EPADB {



    public partial class Form1 : Form {
        public EPADB epa;
        public Form1() {
            InitializeComponent();
            Logger.InitFileLogger("log", "epaLog");
            epa = new EPADB();
            epa.Status = statusLabel;
            epa.Status.TextChanged += Status_TextChanged;
            txtDate1.Text = DateTime.Now.AddHours(-2).ToString("dd.MM.yyyy HH:00");
            txtDate2.Text = DateTime.Now.ToString("dd.MM.yyyy HH:00");
            txtDate3.Text = DateTime.Now.AddHours(-2).ToString("dd.MM.yyyy HH:00");
            txtDate4.Text = DateTime.Now.ToString("dd.MM.yyyy HH:00");
            txtDate.Text = DateTime.Now.ToString("dd.MM.yyyy HH:00");
            textBox1.Text = "300";
        }

        void Status_TextChanged(object sender, EventArgs e) {
            statusLabel.Invalidate();
            Application.DoEvents();

        }

        private void button2_Click(object sender, EventArgs e) {

            List<SignalInfo> resAsu = new List<SignalInfo>();
            epa.findText(epa.ASURoot, txtSearch.Text.ToUpper(), resAsu);            
            if (resAsu.Count > 0) {
                treeAsu.CollapseAll();
                epa.selectTree(treeAsu.Nodes[0], resAsu);
            }

            List<SignalInfo> resTech = new List<SignalInfo>();
            epa.findText(epa.TechRoot, txtSearch.Text.ToUpper(), resTech);
            if (resTech.Count > 0) {
                treeTech.CollapseAll();
                epa.selectTree(treeTech.Nodes[0], resTech);
            }
        }

        private void txtSearch_TextChanged(object sender, EventArgs e) {

        }

        private void treeTech_DoubleClick(object sender, EventArgs e) {
            try {
                string kks = treeTech.SelectedNode.Name;
                string kksA = epa.TechASUDict[kks];
                string[] kksL = kksA.Split(new char[] { ';' });
                foreach (string kksAsu in kksL) {
                    SignalInfo siAsu = epa.FindSignal(epa.ASURoot, kksAsu, null);
                    treeAsu.SelectedNode = siAsu.treeNode;
                    treeAsu.SelectedNode.EnsureVisible();
                }
                statusLabel.Text = String.Format("Найдено {0} сигналов", kksL.Length);
            }
            catch { }
        }

        private void treeAsu_DoubleClick(object sender, EventArgs e) {
            try {
                string kks = treeAsu.SelectedNode.Name;
                string kksT = epa.ASUTechDict[kks];
                string[] kksL = kksT.Split(new char[] { ';' });
                foreach (string kksTech in kksL) {
                    SignalInfo siTech = epa.FindSignal(epa.TechRoot, kksTech, null);
                    treeTech.SelectedNode = siTech.treeNode;
                    treeTech.SelectedNode.EnsureVisible();
                }
                statusLabel.Text = String.Format("Найдено {0} сигналов", kksL.Length);
            }
            catch { }
        }


        private void treeTech_AfterSelect(object sender, TreeViewEventArgs e) {
            richTextBox1.Text = "";
            richTextBox2.Text = "";
            try {
                string kks = treeTech.SelectedNode.Name;
                SignalInfo si = epa.FindSignal(epa.TechRoot, kks, null);
                richTextBox1.Text = String.Format("FullName: {0} \n ShortName:{1} \n KKS:{2}\n", si.FullName, si.ShortName, si.KKS);
                try {                    
                    string kksAsuStr = epa.TechASUDict[kks];
                    richTextBox2.Text += String.Format("link_KKS:{0}\n", kksAsuStr);
                    string[] kksAsuL = kksAsuStr.Split(new char[] { ';' });
                    foreach (string kksAsu in kksAsuL) {
                        try {
                            SignalInfo siAsu = epa.FindSignal(epa.ASURoot, kksAsu, null);
                            richTextBox2.Text += String.Format("FullName: {0} \n ShortName:{1} \n KKS:{2}\nSubSys:{3}\n numSign:{4}\n", siAsu.FullName, siAsu.ShortName, siAsu.KKS, epa.getSubSysName(siAsu),siAsu.numSign);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            catch { }
        }

        private void treeAsu_AfterSelect(object sender, TreeViewEventArgs e) {
            richTextBox1.Text = "";
            richTextBox2.Text = "";
            try {
                string kks = treeAsu.SelectedNode.Name;
                SignalInfo si = epa.FindSignal(epa.ASURoot, kks, null);
                richTextBox2.Text = String.Format("FullName: {0} \n ShortName:{1} \n KKS:{2}\nSubSys:{3}\n numSign:{4}\n", si.FullName, si.ShortName, si.KKS, epa.getSubSysName(si),si.numSign);
                try {
                    string kksTechStr = epa.ASUTechDict[kks];
                    richTextBox1.Text += String.Format("link_KKS:{0}\n", kksTechStr);
                    string[] kksTechL = kksTechStr.Split(new char[] { ';' });
                    foreach (string kksTech in kksTechL) {
                        try {
                            SignalInfo siTech = epa.FindSignal(epa.TechRoot, kksTech, null);
                            richTextBox1.Text += String.Format("FullName: {0} \n ShortName:{1} \n KKS:{2}\n", siTech.FullName, siTech.ShortName, siTech.KKS);
                        }
                        catch { }
                    }
                }
                catch { }
            }
            catch { }
        }

        private void Form1_Shown(object sender, EventArgs e) {
            epa.readkksDev();
            epa.readSubSystems();
            epa.readASU();
            epa.readTech();
            epa.linkSignals();
            epa.readAlgoritms();            
            statusLabel.Text = "Чтение завершено";
            TreeNode nodeASU = epa.displayTree(epa.ASURoot);
            TreeNode nodeTECH = epa.displayTree(epa.TechRoot);

            treeAsu.Nodes.Add(nodeASU);
            treeTech.Nodes.Add(nodeTECH);
        }


        private void treeTech_AfterCheck(object sender, TreeViewEventArgs e) {
            if (epa.processingSelect)
                return;
            epa.processingSelect = true;
            try {
                string kks = e.Node.Name;
                SignalInfo si = epa.FindSignal(epa.TechRoot, kks, null);
                if (si.allChildrenCount > 500)
                    e.Node.Checked = !e.Node.Checked;
                else
                    epa.selectTechSignal(si, e.Node.Checked);
            }
            catch { }
            epa.processingSelect = false;
        }

        private void treeAsu_AfterCheck(object sender, TreeViewEventArgs e) {
            if (epa.processingSelect)
                return;
            epa.processingSelect = true;
            try {
                string kks = e.Node.Name;
                SignalInfo si = epa.FindSignal(epa.ASURoot, kks, null);
                if (si.allChildrenCount > 500)
                    e.Node.Checked = !e.Node.Checked;
                else
                    epa.selectAsuSignal(si, e.Node.Checked, true);
            }
            catch { }
            epa.processingSelect = false;
        }

        private void btnCollapse_Click(object sender, EventArgs e) {
            epa.SelectedDiscrSignals.Clear();
            epa.SelectedAnalogSignals.Clear();
            epa.processingSelect = true;
            treeAsu.CollapseAll();
            treeTech.CollapseAll();
            epa.cleartTree(epa.ASURoot.treeNode);
            epa.cleartTree(epa.TechRoot.treeNode);
            epa.processingSelect = false;

        }

        private void btnGetDiscr_Click(object sender, EventArgs e) {
            DateTime ds = DateTime.Parse(txtDate1.Text);
            DateTime de = DateTime.Parse(txtDate2.Text);

            EpaDiscrData data = new EpaDiscrData(ds, de, epa);
            data.Status = statusLabel;
            data.processSignals();
            data.readTables();
            string fn = String.Format("OUT_Discr_{0}.html", DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss"));
            data.readData(fn);
            try {
                Process.Start(fn);
            }
            catch { }
        }

        private void tabPage2_Enter(object sender, EventArgs e) {
            listBox1.Items.Clear();
            foreach (SignalInfo si in epa.SelectedDiscrSignals) {
                string name = si.ShortName;
                string techName = "";
                try {
                    string kksT = epa.ASUTechDict[si.KKS];
                    string[] kksL = kksT.Split(new char[] { ';' });

                    foreach (string kksTech in kksL) {
                        SignalInfo siTech = epa.FindSignal(epa.TechRoot, kksTech, null);
                        techName = techName + siTech.ShortName;
                    }
                }
                catch { }
                if (techName.Length > 0) {
                    name = name + " (" + techName + ")";
                }
                listBox1.Items.Add(name);
            }
        }

        private void btnState_Click(object sender, EventArgs e) {
            DateTime dt = DateTime.Parse(txtDate.Text);
            DataGridView grid = new DataGridView();
            grid.Columns.Add("Signal", "Сигнал"); grid.Columns[0].Width = 200; grid.Columns[0].ReadOnly = true;
            grid.Columns.Add("SignalTech", "Тех"); grid.Columns[1].Width = 200; grid.Columns[1].ReadOnly = true;
            grid.Columns.Add("Date", "Дата"); grid.Columns[2].Width = 130; grid.Columns[2].ReadOnly = true;
            grid.Columns.Add("State", "Сост"); grid.Columns[3].Width = 70; grid.Columns[3].ReadOnly = true;
            grid.Columns.Add("Src", "Ист"); grid.Columns[3].Width = 70; grid.Columns[3].ReadOnly = true;
            grid.AllowUserToAddRows = false;
            tabControl2.TabPages.Add(dt.ToString());
            TabPage page = tabControl2.TabPages[tabControl2.TabPages.Count - 1];
            page.Controls.Add(grid);
            page.Select();
            grid.Dock = DockStyle.Fill;
            EpaDiscrData data = new EpaDiscrData(dt, dt, epa);
            data.Status = statusLabel;
            data.processSignals();
            data.readTablesStates(dt);
            data.readState(dt, grid);
        }

        private void btnAnalog_Click(object sender, EventArgs e) {
            DateTime ds = DateTime.Parse(txtDate3.Text);
            DateTime de = DateTime.Parse(txtDate4.Text);

            EpaAnalogData data = new EpaAnalogData(ds, de, epa, Int32.Parse(textBox1.Text));
            data.Status = statusLabel;
            data.processSignals();
            data.readTables();
            string fn = String.Format("OUT_An_{0}.html", DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss"));
            Form2 frm = new Form2();
            data.readData(fn, frm.graph.GraphPane);
            frm.graph.GraphPane.XAxis.ScaleFormatEvent += XAxis_ScaleFormatEvent;
            frm.graph.Invalidate();

            frm.Show();

            try {
                Process.Start(fn);
            }
            catch { }

        }

        string XAxis_ScaleFormatEvent(ZedGraph.GraphPane pane, ZedGraph.Axis axis, double val, int index) {
            try {
                int num = Convert.ToInt32(val);
                return EPADB.GetDate(num).ToString();
            }
            catch {
                return "";
            }
        }

        private void tabPage3_Enter(object sender, EventArgs e) {
            listBox2.Items.Clear();
            foreach (SignalInfo si in epa.SelectedAnalogSignals) {
                string name = si.ShortName;
                string techName = "";
                try {
                    string kksT = epa.ASUTechDict[si.KKS];
                    string[] kksL = kksT.Split(new char[] { ';' });

                    foreach (string kksTech in kksL) {
                        SignalInfo siTech = epa.FindSignal(epa.TechRoot, kksTech, null);
                        techName = techName + siTech.ShortName;
                    }
                }
                catch { }
                if (techName.Length > 0) {
                    name = name + " (" + techName + ")";
                }
                listBox2.Items.Add(name);
            }
        }

        private void chkNumSign_CheckedChanged(object sender, EventArgs e) {
            epa.UseNumSignals = chkNumSign.Checked;
        }







    }
}
