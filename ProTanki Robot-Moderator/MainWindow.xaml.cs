﻿using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Net;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace ProTanki_Robot_Moderator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private JObject obj;


        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("settings.data"))
            {
                obj = JObject.Parse(File.ReadAllText("settings.data"));
                tbToken.Text = JsonGet("access_token");
            }
            else
                JsonSet("access_token", "");
        }

        public void Authorization()
        {
            try
            {

                Process.Start(Properties.Resources.OAuth +
                "?client_id=" + Properties.Resources.AppID +
                    "&redirect_uri=" + Properties.Resources.ApiRedirect +
                    "&display=page" +
                    "&response_type=token");
            }
            catch (Exception) { }
        }

        private string POST(string Url, string Data)
        {
            try
            {
                string Out = "";

                WebRequest req = WebRequest.Create(Url);
                req.Method = "POST";
                req.Timeout = 100000;
                req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                byte[] sentData = Encoding.GetEncoding("Utf-8").GetBytes(Data);
                req.ContentLength = sentData.Length;
                Stream sendStream = req.GetRequestStream();
                sendStream.Write(sentData, 0, sentData.Length);
                sendStream.Close();
                WebResponse res = req.GetResponse();
                Stream ReceiveStream = res.GetResponseStream();
                StreamReader sr = new StreamReader(ReceiveStream, Encoding.UTF8);
                Char[] read = new Char[256];
                int count = sr.Read(read, 0, 256);
                while (count > 0)
                {
                    String str = new String(read, 0, count);
                    Out += str;
                    count = sr.Read(read, 0, 256);
                }

                return Out;
            }
            catch (WebException ex) { return ex.Message; }
            catch (Exception ex) { return ex.Message; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Authorization();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                JsonSet("access_token", tbToken.Text);
                File.WriteAllText("settings.data", obj.ToString());
            }
            catch (Exception ex) { File.WriteAllText("errors.log", ex.StackTrace); }
        }

        private void JsonSet(string path, string key)
        {
            try
            {
                if (obj != null)
                {
                    if (obj.SelectToken(path) == null)
                    {
                        JObject jo = (JObject)obj[path];
                        jo.Add(new JProperty(path, key));
                    }
                    else
                        obj[path] = key;
                }
                else
                {
                    obj = new JObject();
                    obj.Add(new JProperty(path, key));
                }
            }
            catch (Exception ex) { File.WriteAllText(path + "_" + key + ".txt", ex.Message); }
        }

        private string JsonGet(string path)
        {
            try { return (string)obj.SelectToken(path); }
            catch (Exception) { }
            return null;
        }

        private JObject WallGet()
        {
            try
            {
                string Data = "access_token=" + JsonGet("access_token") +
                    "&owner_id=" + Properties.Resources.ID +
                    "&offset=0" +
                    "&count=" + Properties.Resources.Count +
                    "&filter=all";

                string result = POST(Properties.Resources.API + "wall.get", Data);

                if (result != null)
                {



                    JToken res = JObject.Parse(result).SelectToken("response");

                    JObject wall = new JObject();
                    JArray ja = new JArray();

                    for (int i = 1; i < res.Count(); i++)
                        if ((int)res[i]["comments"]["count"] > 0)
                            ja.Add(res[i]["id"]);

                    wall["response"] = ja;
                }
            }
            catch (Exception ex) { File.WriteAllText("err.txt", ex.Message); }

            return null;
        }

        private JToken WallGetComments(string postId)
        {
            try
            {
                string Data = "access_token=" + JsonGet("access_token") +
                    "&owner_id=" + Properties.Resources.ID +
                    "&post_id=" + postId +
                    "&offset=0" +
                    "&count=20" +
                    "&need_likes=1" +
                    "&sort=desc" +
                    "&preview_length=0";

                string result = POST(Properties.Resources.API + "wall.getComments", Data);


                if (result != null)
                {
                    JToken res = JObject.Parse(result).SelectToken("response");

                    JObject wall = new JObject();
                    JArray ja = new JArray();

                    for (int i = 1; i < res.Count(); i++)
                        if ((int)res[i].SelectToken("likes.count") < 20)
                            ja.Add(res[i]["cid"]);

                    wall["response"] = ja;

                    return (JObject)wall;
                }
            }
            catch (Exception ex) { File.WriteAllText("err.txt", ex.Message); }

            return null;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //tbLog.Text = WallGet().ToString();
            tbLog.Text = WallGetComments("220626").ToString();
        }
    }
}
