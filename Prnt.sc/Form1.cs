using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using @is.gd;

namespace Prnt.sc
{
    public partial class Form1 : Form
    {
        public const string urlRoot = "https://prnt.sc/";
        public Image displayImg;
        public ArrayList linkHistoryList = new ArrayList();
        public ArrayList imageHistoryList = new ArrayList();
        public int picNum = 1;
        public string picChar = "aa";
        public bool printToDbgConsole = false;

        public Form1()
        {
            InitializeComponent();
        }

        public void HistoryListMgmt(Image imgToSave)
        {
            if (linkHistoryList.Count > 10)
            {
                imageHistoryList[(linkHistoryList.Count - 1) % 10] = imgToSave;
            }
            else
            {
                imageHistoryList.Add(imgToSave);
            }
        }

        private async void button1_ClickAsync(object sender, EventArgs e)
        {
            string picUrl = await RandPicURLAsync();
            Console.WriteLine(picUrl);
            OutputImg(picUrl);
        }

        private async void OutputImg(string picUrl)
        {
            //picUrl = await ShortenLinkAsync(picUrl);
            displayImg = await GetImgFromURL(picUrl);

            linkHistoryList.Add(picUrl);
            HistoryListMgmt(displayImg);
            pictureBox1.Image = displayImg;
            FillTextBox(picUrl);
            AddToListBox(picUrl);
            listView1.EnsureVisible(listView1.Items.Count - 1);
        }

        private async void Button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                Clipboard.SetText(textBox1.Text);
                button2.Text = "Copied!";
                await Task.Delay(2000);
                button2.Text = "Copy link";
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            if (displayImg != null)
            {
                Clipboard.SetImage(pictureBox1.Image);
                button3.Text = "Copied!";
                await Task.Delay(2000);
                button3.Text = "Copy embedded";
            }
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            if (displayImg != null)
            {
                string datePic = DateTime.Now.ToString();
                string pattern = @"(\/)";
                string substitution = @"";
                Regex regex = new Regex(pattern);
                datePic = regex.Replace(datePic, substitution);

                pattern = @"(\:)";
                substitution = @"";
                regex = new Regex(pattern);
                datePic = regex.Replace(datePic, substitution);

                substitution = @"_";
                pattern = @"(\s)";
                regex = new Regex(pattern);
                datePic = regex.Replace(datePic, substitution);
                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)))
                {
                    Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\PrntscSave");
                }

                string path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\PrntscSave\\" + datePic + ".jpg";
                Image imgToSave = pictureBox1.Image;
                imgToSave.Save(path, ImageFormat.Jpeg);
                button4.Text = "Saved!";
                await Task.Delay(2000);
                button4.Text = "Save image";
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != "")
            {
                Process.Start(textBox1.Text);
            }
        }

        private void FillTextBox(string url)
        {
            textBox1.Text = url;
        }

        private async void Form1_LoadAsync(object sender, EventArgs e)
        {
            string urlImg;
            string notExistingPic = $"//st.prntscr.com/2019/11/26/0154/img/0_173a7b_211be8ff.png";
            do
            {
                picNum = await GetRand(9999);
                picChar = await GetRandChar();
                urlImg = await PicURLAsync(picNum, picChar);
                Console.WriteLine(urlImg);
            } while (urlImg == notExistingPic || urlImg == "");
            OutputImg(urlImg);
        }

        private async Task<Image> GetImgFromURL(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.CreateHttp(url);
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
            }
            catch (Exception)
            {

                throw;
            }

            using (Stream reader = response.GetResponseStream())
            {
                return Image.FromStream(reader);
            }
        }

        private async Task<string> GetPageAsync(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.CreateHttp(url);
            HttpWebResponse response;
            request.Method = "GET";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4003.0 Safari/537.36 Edg/81.0.381.0"; //okhttp seems to bypass new issues caused by 2k20 :shrug:
            try
            {
                response = (HttpWebResponse)await request.GetResponseAsync();
                string sourcePage;

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return sourcePage = reader.ReadToEnd();
                }
            }
            catch (WebException e)
            {
                HttpWebResponse n = (HttpWebResponse)e.Response;
                if (n.StatusCode == HttpStatusCode.NotFound)
                {
                    return "";
                }
            }
            return "";
        }

        private async Task<int> GetRand(int iMax)
        {
            int randVal;
            string netRandVal = await GetPageAsync($"https://www.random.org/integers/?num=1&min=0000&max={iMax}&col=1&base=10&format=plain&rnd=new");
            randVal = int.Parse(netRandVal);
            return randVal;
        }

        private async Task<string> GetRandChar()
        {
            return (await GetPageAsync($"https://www.random.org/strings/?num=1&len=2&loweralpha=on&format=html&rnd=new&format=plain")).ToString().Trim();
        }

        private async Task<string> RandPicURLAsync()
        {
            string urlImg;
            int numRand;
            string randChar;
            string notExistingPic = $"//st.prntscr.com/2019/11/26/0154/img/0_173a7b_211be8ff.png";
            do
            {
                numRand = await GetRand(9999);
                randChar = await GetRandChar();
                urlImg = await PicURLAsync(numRand, randChar);
            } while (urlImg == notExistingPic);
            picNum = numRand;
            return urlImg;
        }

        private async Task<string> PicURLAsync(int picNum, string picChar)
        {
            Match regexOut;
            string urlImg;
            urlImg = $"{urlRoot}/{picChar}{picNum}";
            string sourcePage = await GetPageAsync(urlImg);
            string pattern = @"<img class=""no-click screenshot-image"" src=""(.*?)(?="")";
            RegexOptions options = RegexOptions.Multiline;
            regexOut = Regex.Match(sourcePage, pattern, options);
            urlImg = regexOut.Groups[1].Value;
            Console.WriteLine(regexOut.Success);
            if (!regexOut.Success)
            {
                urlImg = "";
            }
            return urlImg;
        }

        private void AddToListBox(string strToAdd)
        {
            listView1.Items.Add(linkHistoryList[linkHistoryList.Count - 1].ToString());
        }

        private async Task<string> ShortenLinkAsync(string url)
        {
            string shortenenLink = await Url.GetShortenedUrl(url);
            return shortenenLink;
        }

        private async void ListView1_SelectedIndexChangedAsync(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                int selectedHistoryIndex = listView1.SelectedItems[0].Index;
                Console.WriteLine(selectedHistoryIndex);
                if (selectedHistoryIndex > linkHistoryList.Count - 10)
                {
                    pictureBox1.Image = (Image)imageHistoryList[selectedHistoryIndex % 10];
                }
                else
                {
                    displayImg = await GetImgFromURL((string)linkHistoryList[selectedHistoryIndex]);
                    pictureBox1.Image = displayImg;
                }
                FillTextBox((string)linkHistoryList[selectedHistoryIndex]);
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                checkBox2.Text = "Read mode";
                button1.Enabled = false;
                button1.Visible = false;

                button7.Enabled = true;
                button7.Visible = true;
                button8.Enabled = true;
                button8.Visible = true;
            }
            else
            {
                checkBox2.Text = "Random mode";
                button1.Enabled = true;
                button1.Visible = true;

                button7.Enabled = false;
                button7.Visible = false;
                button8.Enabled = false;
                button8.Visible = false;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            checkBox1.Text = "Auto change image each " + trackBar1.Value.ToString() + " secs";
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e) //debug console
        {
            if (checkBox3.Checked)
            {
                printToDbgConsole = true;
            }
            else
            {
                printToDbgConsole = false;
            }
        }

        private async void button7_ClickAsync(object sender, EventArgs e)
        {
            string urlPic;
            do
            {
                picNum--;
                urlPic = await PicURLAsync(picNum, picChar);
            } while (urlPic == "");
            OutputImg(urlPic);
        }

        private async void button8_ClickAsync(object sender, EventArgs e)
        {
            string urlPic;
            do
            {
                picNum++;
                urlPic = await PicURLAsync(picNum, picChar);
            } while (urlPic == "");
            OutputImg(urlPic);
        }

        private async void checkBox1_CheckedChangedAsync(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                do
                {
                    byte incMe = 0;
                    do
                    {
                        if (checkBox1.Checked)
                        {
                            incMe += 1; //might help to stop
                            await Task.Delay(1000); // *1000 to get millisec :D
                        }
                        else
                        {
                            return;
                        }
                    } while (((trackBar1.Value - incMe) != 0));
                    if (incMe == trackBar1.Value)
                    {
                        OutputImg(await RandPicURLAsync());
                    }
                    else
                    {
                        //we stopped the time. Or maybe just unchecked the box :/
                    }
                } while (checkBox1.Checked);
            }
        }
    }
}