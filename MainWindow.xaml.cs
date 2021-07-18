using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CsharpJson;

namespace 抖音采集
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string api_url = "https://www.iesdouyin.com/aweme/v1/web/aweme/detail/?aweme_id=${aid}";
        public MainWindow()
        {
            InitializeComponent();
            info("开源地址: https://github.com/MrXiaoM/DouYinVideo");
        }

        private ImageSource GetImageFromURL(string url)
        {
            try
            {
                return new BitmapImage(new Uri(url));
            } catch
            {
                BitmapImage biImg = new BitmapImage();
                try
                {
                    using (MemoryStream ms = new MemoryStream(Properties.Resources.douyin))
                    {
                        biImg.BeginInit();
                        biImg.StreamSource = ms;
                        biImg.CacheOption = BitmapCacheOption.OnLoad;
                        biImg.EndInit();
                        biImg.Freeze();
                    }
                }
                catch { }
                return biImg;
            }
        }

        private void info(string i)
        {
            log("[INFO] " + i);
        }
        private void warn(string i)
        {
            log("[WARN] " + i);
        }
        private void error(string i)
        {
            log("[ERROR] " + i);
        }
        private void log(string i)
        {
            string log = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + i;
            LogCat.Text = LogCat.Text + (LogCat.Text.EndsWith("\n") || LogCat.Text == string.Empty ? "" : "\n") + log;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string[] ids = IdsTextBox.Text.Contains("\n") ? IdsTextBox.Text.Split('\n') : new string[] { IdsTextBox.Text };
            info("正在获取各作品信息…");
            foreach(string id in ids)
            {
                WebClient wc = new WebClient();
                wc.Headers.Add(HttpRequestHeader.Referer, "https://www.douyin.com/video/" + id);
                wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.70");
                string result = new WebClient().DownloadString(api_url.Replace("${aid}", id));
                if(result.Equals("Need Verifying"))
                {
                    warn(id + " 提示需要验证，跳过");
                    continue;
                }
                JsonDocument jd = JsonDocument.FromString(result);
                if (jd.IsObject())
                {
                    try
                    {
                        JsonObject json = jd.Object["aweme_detail"].ToObject();
                        string desc = json["desc"].ToString();
                        string authorNickname = json["author"].ToObject()["nickname"].ToString();
                        JsonObject statistics = json["statistics"].ToObject();
                        int like = statistics["digg_count"].ToInt();
                        int comments = statistics["comment_count"].ToInt();
                        string coverUrl = json["video"].ToObject()["cover"].ToObject()["url_list"].ToArray()[0].ToString();
                        string downloadUrl = json["video"].ToObject()["play_addr"].ToObject()["url_list"].ToArray()[0].ToString();

                        this.listView.Items.Add(new ListViewItem(GetImageFromURL(coverUrl), id, desc, like.ToString(), authorNickname, downloadUrl));
                    }
                    catch (Exception ex)
                    {
                        error("在解析 " + id + " 时出现一个错误: " + ex.Message);
                    }
                }
                else
                {
                    warn("无法解析 " + id + " 返回的 json");
                }
            }
            update();
            info("各作品信息获取完毕");
        }

        private void textBox_keyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = (sender as TextBox);
            if (e.Key == Key.Enter)
            {
                int sel = tb.SelectionStart;
                tb.Text = tb.Text.Insert(sel, "\n");
                tb.SelectionStart = sel + 1;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("作品ID是一串数字，当你在浏览器打开抖音视频分享链接时，\n" +
                "你会发现/符号和?符号之间有一串数字，没有参数时就是最后一个/到最后的数字\n" +
                "举个例子，分享视频的链接是 https://v.douyin.com/etUbpwn/ ，访问会转跳到\n" +
                "https://www.douyin.com/video/6951596640251809061?previous_page=app_code_link\n" +
                "那作品 ID 就是 6951596640251809061 了");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            WebBrowser wb = new WebBrowser();
            wb.Navigating += Wb_Navigating;
            wb.Navigate(AddressTextBox.Text);
        }

        private void Wb_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            info("正在尝试获取分享链接的作品ID");
            FieldInfo fi = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi != null)
            {
                object browser = fi.GetValue(sender);
                if (browser != null)
                    browser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, browser, new object[] { true });
            }
            string url = e.Uri.AbsoluteUri;
            if (url.Contains("?"))
            {
                url = url.Substring(0, url.IndexOf("?"));
            }
            if (url.Contains("/"))
            {
                url = url.Substring(url.LastIndexOf("/") + 1);
            }
            if (url.Length > 5)
            {
                info("获取到了作品ID，等待用户回应");
                if (MessageBox.Show("获取到的作品 ID 为 " + url + "，是否填入列表?", "抖音视频下载", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    info("将获取到的 " + url + " 填入列表");
                    IdsTextBox.Text += (IdsTextBox.Text.EndsWith("\n") || IdsTextBox.Text == string.Empty ? "" : "\n") + url;
                }
                (sender as WebBrowser).Dispose();
            }
        }

        private void Button_Click_Copy(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var c = btn.DataContext as ListViewItem;
            if (c != null && c is ListViewItem)
            {
                info("已复制 " + c.Id + " 的下载地址");
                Clipboard.SetDataObject(c.DownloadLink, true);
                MessageBox.Show("已复制下载地址到你的剪贴板");
            }
        }
        private void Button_Click_Remove(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var c = btn.DataContext as ListViewItem;
            if (c != null && c is ListViewItem)
            {
                info("已从列表移除 " + c.Id);
                listView.Items.Remove(c);
            }
            update();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            info("已清空列表");
            listView.Items.Clear();
            update();
        }

        private void update()
        {
            this.listCount.Text = "列表共有 " + listView.Items.Count + " 项";
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            string links = string.Empty;
            foreach (ListViewItem item in listView.Items)
            {
                links += (links == string.Empty ? "" :"\n") + item.DownloadLink;
            }
            info("已复制列表中所有下载地址");
            Clipboard.SetDataObject(links, true);
            MessageBox.Show("已复制下载地址到你的剪贴板");
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("开源地址: https://github.com/MrXiaoM/DouYinVideo\n" +
                "软件作者: MrXiaoM\n" +
                "仅供测试学习与参考，请勿用于商业目的\n" +
                "本软件与字节跳动及其旗下部门、产品没有从属关系\n" +
                "\n" +
                "依赖库: CsharpJson");
        }
    }
    class ListViewItem : INotifyPropertyChanged
    {
        private string _id;
        private string _desc;
        private string _like;
        private string _author;
        private string _downloadLink;
        private ImageSource _coverImage;
        public event PropertyChangedEventHandler PropertyChanged;
        public ListViewItem() { }
        public ListViewItem(ImageSource coverImage, string id, string desc, string like, string author, string downloadLink)
        {
            this._coverImage = coverImage;
            this._id = id;
            this._desc = desc;
            this._like = like;
            this._author = author;
            this._downloadLink = downloadLink;
        }

        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Id"));
                }
            }
        }
        public string Desc
        {
            get
            {
                return _desc;
            }
            set
            {
                _desc = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Desc"));
                }
            }
        }
        public string Like
        {
            get
            {
                return _like;
            }
            set
            {
                _like = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Like"));
                }
            }
        }
        public string Author
        {
            get
            {
                return _author;
            }
            set
            {
                _author = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("Author"));
                }
            }
        }
        public string DownloadLink
        {
            get
            {
                return _downloadLink;
            }
            set
            {
                _downloadLink = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DownloadLink"));
                }
            }
        }

        public ImageSource CoverImage
        {
            get
            {
                return _coverImage;
            }
            set
            {
                _coverImage = value;
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CoverImage"));
                }
            }
        }
    }
}
