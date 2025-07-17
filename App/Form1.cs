using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace WindowsFormsSharePointApp2
{
    // https://docs.microsoft.com/en-us/previous-versions/office/developer/sharepoint-2010/hh147177(v=office.14)?redirectedfrom=MSDN
    // https://stackoverflow.com/questions/15049877/getting-webbrowser-cookies-to-log-in
    // https://stackoverflow.com/questions/3382498/is-it-possible-to-transfer-authentication-from-webbrowser-to-webrequest
    // https://stackoverflow.com/questions/3062925/c-sharp-get-httponly-cookie
    // https://stackoverflow.com/questions/25388696/federated-authentication-in-sharepoint-2013-getting-rtfa-and-fedauth-cookies

    public partial class Form1 : Form
    {
        string sharePointSiteUrl = "https://xx.sharepoint.com/sites/TestSite/Shared%20Documents/Forms/AllItems.aspx";
        string sharePointImageUrl = "https://xx.sharepoint.com/sites/TestSite/Images1/test.jpg";

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await webView21.EnsureCoreWebView2Async();
            webView21.NavigationCompleted += webView21_NavigationCompleted;
            webView21.Source = new Uri(sharePointSiteUrl);
        }

        private IDictionary<string, string> ParseCookieData(string cookieData)
        {
            var cookieDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (string.IsNullOrEmpty(cookieData))
                    return cookieDictionary;

                var values = cookieData.TrimEnd(';').Split(';');
                foreach (var parts in values.Select(c => c.Split(new[] { '=' }, 2)))
                {
                    var cookieName = parts[0].Trim();
                    string cookieValue;

                    if (parts.Length == 1)
                        cookieValue = string.Empty;
                    else
                        cookieValue = parts[1];

                    cookieDictionary[cookieName] = cookieValue;
                }
            }
            catch (Exception)
            {
            }

            return cookieDictionary;
        }

        private void buttonDownloadImage_Click(object sender, EventArgs e)
        {
            try
            {
                var url = sharePointImageUrl;

                var handler = new HttpClientHandler();
                handler.CookieContainer = new System.Net.CookieContainer();

                var cc = new CookieCollection();
                cc.Add(new Cookie("FedAuth", textBoxFedAuth.Text));
                cc.Add(new Cookie("rtFa", textBoxrtFa.Text));

                handler.CookieContainer.Add(new Uri(url), cc);

                HttpClient httpClient = new HttpClient(handler);
                var resp = httpClient.GetAsync(url).Result;
                var byteData = resp.Content.ReadAsByteArrayAsync().Result;

                if (resp.IsSuccessStatusCode)
                {
                    pictureBox1.Image = byteArrayToImage(byteData);
                }
            }
            catch (Exception)
            {

            }
        }

        public Image byteArrayToImage(byte[] bytesArr)
        {
            using (MemoryStream memstr = new MemoryStream(bytesArr))
            {
                Image img = Image.FromStream(memstr);
                return img;
            }
        }

        private async void webView21_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                var uri = webView21.Source?.AbsoluteUri;
                if (string.IsNullOrEmpty(uri) || uri == "about:blank")
                    return;

                var cookieManager = webView21.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync(uri);

                var cookieData = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                textBoxCookie.Text = cookieData;

                var dict = ParseCookieData(cookieData);
                textBoxFedAuth.Text = dict.ContainsKey("FedAuth") ? dict["FedAuth"] : "";
                textBoxrtFa.Text = dict.ContainsKey("rtFa") ? dict["rtFa"] : "";
            }
            catch (Exception)
            {
                // Handle exceptions as needed
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            int rightMargin = 20;
            textBoxCookie.Left = this.ClientSize.Width - textBoxCookie.Width - rightMargin;
            textBoxFedAuth.Left = this.ClientSize.Width - textBoxFedAuth.Width - rightMargin;
            textBoxrtFa.Left = this.ClientSize.Width - textBoxrtFa.Width - rightMargin;
        }
    }
}
