using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace TaijiTaskClient
{
    public partial class Form1 : Form
    {
        TaijiTaskServiceReference.TaskServiceClient client;
        Thread thread;
        public Form1()
        {
            InitializeComponent();
            client = new TaijiTaskServiceReference.TaskServiceClient("BasicHttpBinding_ITaskService");
            thread = new Thread(new ThreadStart(Bind));
            thread.IsBackground = true;
            btnStartAll.Enabled = btnStopAll.Enabled = false;
        }


        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                tbStatus.Text = "连接中..";
                var datas = await client.GetTaskListAsync();
                listView1.Items.Clear();
                foreach (var data in datas)
                {
                    var item = new ListViewItem(data.id.ToString());
                    item.SubItems.Add(data.name);
                    item.SubItems.Add(data.taskStatus);
                    item.SubItems.Add(data.nextProcessTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-");
                    item.SubItems.Add(data.timerType);
                    item.SubItems.Add(data.interval.ToString());
                    item.SubItems.Add(data.runOnStart ? "是" : "否");
                    item.SubItems.Add(data.startTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-");
                    item.Tag = data;
                    listView1.Items.Add(item);
                }
                btnStartAll.Enabled = btnStopAll.Enabled = true;
                thread.Start();
                tbStatus.Text = "已就绪";
            }
            catch (EndpointNotFoundException ex)
            {
                Disconnect();
                MessageBox.Show(string.Format("未能连接服务器：{0}，错误原因：{1}.", client.Endpoint.Address.Uri, ex.Message), "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Bind()
        {
            try
            {
                while (true)
                {
                    Invoke(new Action<Dictionary<Guid, TaijiTaskServiceReference.TaskResult>>(dic =>
                    {
                        foreach (ListViewItem item in listView1.Items)
                        {
                            var data = dic[((TaijiTaskServiceReference.TaskResult)item.Tag).id];
                            item.SubItems[1].Text = data.name;
                            item.SubItems[2].Text = data.taskStatus;
                            item.SubItems[3].Text = data.nextProcessTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
                            item.SubItems[4].Text = data.timerType;
                            item.SubItems[5].Text = data.interval.ToString();
                            item.SubItems[6].Text = data.runOnStart ? "是" : "否";
                            item.SubItems[7].Text = data.startTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-";
                            item.Tag = data;
                        }
                    }), client.GetTaskList().ToDictionary(p => p.id));
                    Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    Disconnect();
                    MessageBox.Show(string.Format("未能连接服务器：{0}，错误原因：{1}.", client.Endpoint.Address.Uri, ex.Message), "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private void Disconnect()
        {
            btnStartAll.Enabled = btnStopAll.Enabled = false;
            btnStart.Enabled = btnStop.Enabled = false;
            thread.Abort();
            tbStatus.Text = "连接已中断";
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            client.Close();
        }

        private void btnStartAll_Click(object sender, EventArgs e)
        {
            client.StartAll();
        }

        private void btnStopAll_Click(object sender, EventArgs e)
        {
            client.StopAll();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView1.SelectedItems.Count != 1)
                e.Cancel = true;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            var data = listView1.SelectedItems[0].Tag as TaijiTaskServiceReference.TaskResult;
            client.Start(data.id);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            var data = listView1.SelectedItems[0].Tag as TaijiTaskServiceReference.TaskResult;
            client.Stop(data.id);
        }

        private void btnRunAtOnce_Click(object sender, EventArgs e)
        {
            var data = listView1.SelectedItems[0].Tag as TaijiTaskServiceReference.TaskResult;
            client.RunImmediately(data.id);
        }
    }
}
