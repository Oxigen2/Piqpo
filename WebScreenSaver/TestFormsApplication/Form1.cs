using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TestFormsApplication
{
    public partial class Form1 : Form
    {
        public Form1(bool parent)
        {
            InitializeComponent();

            if (parent)
            {
                m_child = new Form1(false);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (m_child != null)
            {
                m_child.Show();
            }
        }

        private Form1 m_child;
    }
}
