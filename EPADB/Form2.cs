using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZedGraph;

namespace EPADB {
    public partial class Form2 : Form {
        public ZedGraphControl graph;
        public Form2() {
            InitializeComponent();
            graph = zedGraphControl1;            
        }
    }
}
