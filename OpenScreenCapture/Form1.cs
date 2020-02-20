using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenScreenCapture
{
    public partial class Form1 : Form
    {
        private string hostname = "hostname";
        private string saveto   = "";
        private int interval = 30; //秒



        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled) return;

            this.Hide();
            this.notifyIcon1.Visible = true;

            timer1.Interval = this.interval * 1000;

            timer1.Start();
            btnStart.Enabled = false;
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            Rectangle tScreenRect = new Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Bitmap tSrcBmp = new Bitmap(tScreenRect.Width, tScreenRect.Height); // 用于屏幕原始图片保存
            Graphics gp = Graphics.FromImage(tSrcBmp);
            gp.CopyFromScreen(0, 0, 0, 0, tScreenRect.Size);
            gp.DrawImage(tSrcBmp, 0, 0, tScreenRect, GraphicsUnit.Pixel);
            tSrcBmp.Save("now.png", ImageFormat.Png);

            string ret = this.upload("http://192.168.2.114:8080/osctaker.php", "now.png", "file", ImageFormat.Png.ToString(), "hostname=demo");

        }


        public string upload(string url, string file, string paramName, string contentType, string args)
        {


            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");


            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Timeout = 10000;

            request.Method = "POST";

            using (Stream reqStream = request.GetRequestStream())
            {
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                string[] av = args.Split('&');

                foreach (string str in av)
                {
                    string[] p = str.Split('=');

                    reqStream.Write(boundarybytes, 0, boundarybytes.Length);
                    string formitem = string.Format(formdataTemplate, p[0], p[1]);
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    reqStream.Write(formitembytes, 0, formitembytes.Length);
                }
                reqStream.Write(boundarybytes, 0, boundarybytes.Length);


                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                string header = string.Format(headerTemplate, paramName, file, contentType);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                reqStream.Write(headerbytes, 0, headerbytes.Length);

                FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[4096];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    reqStream.Write(buffer, 0, bytesRead);
                }
                fileStream.Close();

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                reqStream.Write(trailer, 0, trailer.Length);

            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("GBK"));
            return reader.ReadToEnd();
        }
    }
}
