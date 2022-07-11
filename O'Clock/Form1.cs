using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Npgsql;

namespace O_Clock
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            List<string> text = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.txt")).ToList();
            foreach (string d in text)
            {
                string[] items = d.Split(new char[] { ',' },
                       StringSplitOptions.RemoveEmptyEntries);
                ListViewItem item = listView1.Items.Add(items[0].ToString());
                item.SubItems.Add(items[2].ToString());
                item.SubItems.Add(items[1].ToString());
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label1.Text = DateTime.Now.ToLongDateString();
            label2.Text = DateTime.Now.ToLongTimeString();

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.SubItems[0].Text == label1.Text && item.SubItems[2].Text == label2.Text)
                {
                    SoundPlayer player = new SoundPlayer();
                    player.SoundLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "alarm.wav");
                    player.PlayLooping();
                    DialogResult result = MessageBox.Show(item.SubItems[1].Text);
                    if (result == DialogResult.OK || result == DialogResult.Cancel)
                    {
                        player.Stop();
                    }
                }
            }
        }

        // Tombol Hapus
        private void button2_Click(object sender, EventArgs e)
        {
            List<string> text = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.txt")).ToList();
            List<string> data = new List<string>();
            foreach (string d in text)
            {
                string[] items = d.Split(new char[] { ',' });
                if (listView1.SelectedItems[0].Text != items[0] && listView1.SelectedItems[0].SubItems[1].Text != items[1] && listView1.SelectedItems[0].SubItems[2].Text != items[2])
                {
                    data.Add(items[0] + ',' + items[1] + ',' + items[2] + Environment.NewLine);
                }
            }
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.txt"), String.Empty);
            foreach (string item in data)
            {
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.txt"), item);
            }

            if (listView1.SelectedItems.Count > 0)
            {
                listView1.SelectedItems[0].Remove();
            }
        }

        // Tombol Tambah
        private void button1_Click(object sender, EventArgs e)
        {
            string tanggal = Interaction.InputBox("Masukkan Tanggal Alarm Yang Diinginkan", "Tanggal Alarm");
            string waktu = Interaction.InputBox("Masukkan Waktu Alarm Yang Diinginkan", "Waktu Alarm");
            string nama = Interaction.InputBox("Masukkan Nama Alarm Yang Diinginkan", "Nama Alarm");

            string data = tanggal + "," + waktu + "," + nama;
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.txt"), data + Environment.NewLine); 

            ListViewItem item = listView1.Items.Add(tanggal);
            item.SubItems.Add(nama);
            item.SubItems.Add(waktu);
        }

        // Tombol Bagikan
        private void button3_Click(object sender, EventArgs e)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[6];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var kode = new string(stringChars);

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection("Server=localhost;Port=5432;Database=test;User Id=postgres;Password=qwerty;"))
                {
                    connection.Open();
                    NpgsqlCommand cmd = new NpgsqlCommand();
                    cmd.Connection = connection;
                    cmd.CommandText = "Insert into alarm (kode) values (@kode)";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(new NpgsqlParameter("@kode", kode));
                    cmd.ExecuteNonQuery();

                    using (cmd = new NpgsqlCommand("Insert into detail_alarm (kode, tanggal, waktu, nama) values((Select id from alarm where kode = '" + kode + "'), @tanggal, @waktu, @nama)", connection))
                    {
                        var tanggal = new NpgsqlParameter("tanggal", DbType.String);
                        var waktu = new NpgsqlParameter("waktu", DbType.String);
                        var nama = new NpgsqlParameter("nama", DbType.String);
                        cmd.Parameters.Add(tanggal);
                        cmd.Parameters.Add(waktu);
                        cmd.Parameters.Add(nama);
                        cmd.Prepare();   
                        List<string> text = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.txt")).ToList();
                        foreach (string d in text)
                        {
                            string[] items = d.Split(new char[] { ',' });
                            tanggal.Value = items[0].ToString();
                            waktu.Value = items[1].ToString();
                            nama.Value = items[2].ToString();
                            cmd.ExecuteNonQuery();
                        }
                    }
                    cmd.Dispose();
                    connection.Close();
                    MessageBox.Show("Kode untuk dibagikan : " + kode);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Tombol Masukkan Kode
        private void button4_Click(object sender, EventArgs e)
        {
            string kode = Interaction.InputBox("Masukkan Kode Yang Didapatkan", "Kode Alarm");

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection("Server=localhost;Port=5432;Database=test;User Id=postgres;Password=qwerty;"))
                {
                    connection.Open();
                    NpgsqlCommand cmd = new NpgsqlCommand();
                    cmd.Connection = connection;
                    using (cmd = new NpgsqlCommand("Select * from detail_alarm where kode = (Select id from alarm where kode = '" + kode + "')", connection))
                    {
                        string tanggal;
                        string waktu;
                        string nama;
                        NpgsqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            tanggal = reader[2].ToString();
                            waktu = reader[3].ToString();
                            nama = reader[4].ToString();

                            string data = tanggal + "," + waktu + "," + nama;
                            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data.txt"), data + Environment.NewLine);

                            ListViewItem item = listView1.Items.Add(tanggal);
                            item.SubItems.Add(nama);
                            item.SubItems.Add(waktu);
                        }
                    }
                    cmd.Dispose();
                    connection.Close();
                    MessageBox.Show("Jadwal Baru Berhasil Ditambahkan!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
