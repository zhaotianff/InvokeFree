﻿using CSharpCrawler.Model;
using CSharpCrawler.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

namespace CSharpCrawler.Views
{
    /// <summary>
    /// FetchImage.xaml 的交互逻辑
    /// </summary>
    public partial class FetchImage : Page
    {
        GlobalDataUtil globalData = GlobalDataUtil.GetInstance();
        object obj = new object();
        ObservableCollection<UrlStruct> imageCollection = new ObservableCollection<UrlStruct>();
        List<UrlStruct> ToVisitList = new List<UrlStruct>();
        List<UrlStruct> VisitedList = new List<UrlStruct>();

        int globalIndex = 1;

        public FetchImage()
        {
            InitializeComponent();
            this.listview_Image.ItemsSource = imageCollection;
        }

        private void btn_Surfing_Click(object sender, RoutedEventArgs e)
        {
            string url = this.tbox_Url.Text;
            bool isStartWithHttp = false;

            if (string.IsNullOrEmpty(url))
            {
                ShowStatusText("请输入Url");
                return;
            }

            //判断Url
            if (globalData.CrawlerConfig.ImageConfig.IgnoreUrlCheck == false)
            {
                if (RegexUtil.IsUrl(url, out isStartWithHttp) == false)
                {
                    ShowStatusText("网址输入有误");
                    return;
                }

                if (isStartWithHttp == false)
                {
                    url = "http://" + url;
                }
            }

            Reset();
            Surfing(url);
        }

        public void ShowStatusText(string content)
        {
            this.Dispatcher.Invoke(() => {
                this.lbl_Status.Content = content;
            });
        }

        public void Surfing(string url)
        {
            if(globalData.CrawlerConfig.ImageConfig.DynamicGrab == true)
            {
                SurfingByCEF(url);
            }
            else
            {
                SurfingByFCL(url);
            }
        }

        private void SurfingByCEF(string url)
        {
            globalData.Browser.GetHtmlSourceDynamic(url);
            
        }

        private async void SurfingByFCL(string url)
        {
            try
            {
                string html = await WebUtil.GetHtmlSource(url);

                Thread extractThread = new Thread(new ParameterizedThreadStart(ExtractImage));
                extractThread.IsBackground = true;
                extractThread.Start(html);
            }
            catch (Exception ex)
            {
                //TODO
                ShowStatusText(ex.Message);
            }
        }

        private void ExtractImage(object html)
        {
            string value = "";          

            MatchCollection mc = RegexUtil.Match(html.ToString(), RegexPattern.TagImgPattern);
            foreach (Match item in mc)
            {
                value = item.Groups["image"].Value;
                AddToCollection(new UrlStruct() {Id = globalIndex,Status = "",Title = "",Url = value });
                IncrementCount();
            }


        }


        public void AddToCollection(UrlStruct urlStruct)
        {
            lock (obj)
            {
                var query = imageCollection.Where(x => x.Url == urlStruct.Url).FirstOrDefault();
                if (query != null)
                    return;
                Dispatcher.Invoke(() => {
                    imageCollection.Add(urlStruct);
                    ToVisitList.Add(urlStruct);
                });
            }
        }

        public void ClearCollection()
        {
            Dispatcher.Invoke(()=> {
                imageCollection.Clear();
            }); 
        }

        public void Reset()
        {
            ClearCollection();
            globalIndex = 1;
            ShowStatusText("");
        }

        private void IncrementCount()
        {
            System.Threading.Interlocked.Increment(ref globalIndex);
        }

        private void listview_Image_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = this.listview_Image.SelectedIndex;
            if(index != -1)
            {
                this.imgage_Thumbnail.Source = new BitmapImage(new Uri(imageCollection[index].Url));
            }
        }
    }
}
