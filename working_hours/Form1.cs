using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;

namespace working_hours
{
    public partial class Form1 : Form
    {
        Timer timer;
        private Dictionary<int, TimeSpan> elapsedTimes = new Dictionary<int, TimeSpan>(); // 儲存每個行的已過時間
        private Dictionary<int, DateTime> startTimes = new Dictionary<int, DateTime>(); // 儲存每個行的開始時間

        public Form1()
        {
            InitializeComponent();

            // 初始化 Timer
            timer = new Timer();
            timer.Interval = 1000; // 1 秒
            timer.Tick += timer1_Tick;
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            // 連線到 SQLite 資料庫
            string connectionString = "Data Source=C:\\Users\\User\\source\\repos\\working_hours\\mydatabase.db";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // 填入機台名稱選單
                string query = "SELECT MachineID, MachineName FROM Machines";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbMachines.ValueMember = "MachineID";
                    cmbMachines.DisplayMember = "MachineName";
                    cmbMachines.DataSource = table;
                }

                // 填入機台單元選單
                query = "SELECT UnitID, UnitName FROM MachineUnits";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbUnits.ValueMember = "UnitID";
                    cmbUnits.DisplayMember = "UnitName";
                    cmbUnits.DataSource = table;
                }

                // 填入操作項目選單
                query = "SELECT ItemID, ItemName FROM OperationItems";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbItems.ValueMember = "ItemID";
                    cmbItems.DisplayMember = "ItemName";
                    cmbItems.DataSource = table;
                }

                // 填入員工名稱選單
                query = "SELECT EmployeeID, EmployeeName FROM Employees";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbEmployees.ValueMember = "EmployeeID";
                    cmbEmployees.DisplayMember = "EmployeeName";
                    cmbEmployees.DataSource = table;
                }

                //開啟派工資料表
                query = "SELECT * FROM WorkAssignments";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvAssignments.DataSource = table;

                    dgvAssignments.Columns.Remove("AssignmentID");


                    // 初始化已過時間及開始時間
                    foreach (DataGridViewRow row in dgvAssignments.Rows)
                    {
                        int rowIndex = row.Index + 1;
                        elapsedTimes.Add(rowIndex, TimeSpan.Zero);

                        // 檢查 "StartTime" 欄位是否為 null
                        if (row.Cells["StartTime"].Value != null)
                        {
                            startTimes.Add(rowIndex, DateTime.Parse(row.Cells["StartTime"].Value.ToString()));

                            // 如果狀態為"正在進行中",則啟動該行的計時
                            if (row.Cells["State"].Value != null && row.Cells["State"].Value.ToString() == "正在進行中")
                            {
                                startTimes[rowIndex] = DateTime.Parse(row.Cells["StartTime"].Value.ToString());
                            }
                        }
                        else
                        {
                            // 如果為 null,則使用當前時間作為開始時間
                            startTimes.Add(rowIndex, DateTime.Now);
                        }
                    }
                }
            }
            // 啟動計時器
            timer.Start();
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvAssignments.Rows)
            {
                int rowIndex = row.Index + 1;
                if (row.Cells["State"].Value != null && row.Cells["State"].Value.ToString() == "正在進行中")
                {
                    elapsedTimes[rowIndex] = DateTime.Now - startTimes[rowIndex];
                    row.Cells["TotalTime"].Value = elapsedTimes[rowIndex].ToString(@"hh\:mm\:ss");
                }
            }
        }

        private void btnAssign_Click(object sender, EventArgs e)
        {
            // 獲取選擇的值
            string machineName = cmbMachines.Text;
            string unitName = cmbUnits.Text;
            string itemName = cmbItems.Text;
            string employeeName = cmbEmployees.Text;
            DateTime startTime = DateTime.Now;

            // 連線到 SQLite 資料庫
            string connectionString = "Data Source=C:\\Users\\User\\source\\repos\\working_hours\\mydatabase.db";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // 插入派工記錄
                string query = "INSERT INTO WorkAssignments (MachineName, UnitName, ItemName, EmployeeName, State, TotalTime, StartTime)" +
                    " VALUES (@MachineName, @UnitName, @ItemName, @EmployeeName, @State, @TotalTime, @StartTime)";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@MachineName", machineName);
                    command.Parameters.AddWithValue("@UnitName", unitName);
                    command.Parameters.AddWithValue("@ItemName", itemName);
                    command.Parameters.AddWithValue("@EmployeeName", employeeName);
                    command.Parameters.AddWithValue("@State", "正在進行中");
                    command.Parameters.AddWithValue("@StartTime", startTime.ToString("yyyy-MM-dd HH:mm:ss")); // 加入開始時間
                    command.Parameters.AddWithValue("@TotalTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.ExecuteNonQuery();
                }

                // 更新 DataGridView 中的內容
                query = "SELECT * FROM WorkAssignments";
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvAssignments.DataSource = null;
                    dgvAssignments.Rows.Clear();
                    dgvAssignments.DataSource = table;
                    dgvAssignments.Columns.Remove("AssignmentID");

                    // 初始化新行的已過時間
                    int lastIndex = dgvAssignments.Rows.Count - 1;
                    elapsedTimes.Add(lastIndex + 1, TimeSpan.Zero);
                }
            }

            // 如果尚未啟動計時器,則啟動
            if (!timer.Enabled)
            {
                timer.Start();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dgvAssignments.SelectedRows.Count > 0) // 確保至少有一行被選中
            {
                DataGridViewRow selectedRow = dgvAssignments.SelectedRows[0];
                int rowIndex = selectedRow.Index + 1;

                if (comboBox1.Text == "暫停" || comboBox1.Text == "已完成")
                {
                    // 停止選中行的計時
                    elapsedTimes[rowIndex] = DateTime.Now - startTimes[rowIndex];
                    selectedRow.Cells["TotalTime"].Value = elapsedTimes[rowIndex].ToString(@"hh\:mm\:ss");
                    selectedRow.Cells["State"].Value = comboBox1.Text;
                }
                else
                {
                    // 啟動選中行的計時
                    startTimes[rowIndex] = DateTime.Now;
                    selectedRow.Cells["State"].Value = "正在進行中";
                }
            }
        }
    }
}