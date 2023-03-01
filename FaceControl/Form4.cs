using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using System.IO;

namespace FaceControl
{
    public partial class Form4 : Form
    {
        OleDbConnection con = new OleDbConnection($@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={Directory.GetCurrentDirectory()}\people.mdb; Persist Security Info=False");
        public Form4()
        {
            InitializeComponent();
        }

        private void Form4_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            label2.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            con.Open();
            OleDbCommand select = new OleDbCommand();
            select.Connection = con;
            select.CommandText = $"Select * From staff Where pass_st='{textBox1.Text}'";
            OleDbDataReader reader = select.ExecuteReader();
            if (reader.HasRows)
            {
                Form1 form1 = new Form1();
                this.Hide();
                form1.Show();
            }
            else
            {
                MessageBox.Show("Неверный пароль");
            }
            con.Close();
             
        }
    }
}
