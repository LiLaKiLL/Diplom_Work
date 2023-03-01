using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceControl
{
    public partial class Form5 : Form
    {
        public Form5()
        {
            InitializeComponent();
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            // TODO: данная строка кода позволяет загрузить данные в таблицу "peopleDataSet.student". При необходимости она может быть перемещена или удалена.
            this.studentTableAdapter.Fill(this.peopleDataSet.student);
            // TODO: данная строка кода позволяет загрузить данные в таблицу "peopleDataSet.staff". При необходимости она может быть перемещена или удалена.
            this.staffTableAdapter.Fill(this.peopleDataSet.staff);

        }
    }
}
