using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MVSPages.Pages
{
    public class HostSettings
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string HostHttpUrl { get; set; }
    }

    public class SubmitJobModel : PageModel
    {
        private IWebHostEnvironment _environment;

        private static HostSettings? hostSettings = null;

        private string InvalidateId { get; set; }

        public SubmitJobModel(IWebHostEnvironment environment)
        {
            _environment = environment;
            //_httpContextAccessor = httpContextAccessor;
            if (hostSettings == null)
            {
                JclText = string.Empty;
                var file = Path.Combine(_environment.ContentRootPath, "", "hostsettings.json");

                string json = System.IO.File.ReadAllText(file);
                hostSettings =
                    JsonSerializer.Deserialize<HostSettings>(json);

            }


            Host = hostSettings.Host;
            Port = hostSettings.Port;
            //HostHttpUrl = hostSettings.HostHttpUrl;
        }

        [ViewData]
        public string JclText
        {
            get; set;
        }

        private void SetInvalidateId()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("InvalidateId")))
            {
                HttpContext.Session.SetString("InvalidateId", "1");
                HostHttpUrl = hostSettings.HostHttpUrl + "?InvalidateId=1";
            }
            else
            {
                int iVal = int.Parse(HttpContext.Session.GetString("InvalidateId"));
                iVal++;
                HostHttpUrl = hostSettings.HostHttpUrl + "?InvalidateId=" + iVal.ToString();

            }

        }
        public void OnGet()
        {
            SetInvalidateId();
        }

        public string HostHttpUrl { get; set; }
        [BindProperty]
        public IFormFile Upload { get; set; }
        public async Task OnPostUploadFileAsync()
        {
            var file = Path.Combine(_environment.ContentRootPath, "upload", Upload.FileName);
            using (var fileStream = new FileStream(file, FileMode.Create))
            {
                await Upload.CopyToAsync(fileStream);
            }

            JclText = System.IO.File.ReadAllText(file).Trim();
            HttpContext.Session.SetString("Jcl", JclText);

            SetInvalidateId();

        }

        [BindProperty]
        public string Host { get; set; }
        [BindProperty]
        public string Port { get; set; }

        [BindProperty]
        public string TextValue { get; set; }

        public void OnPostSubmitJcl()
        {
            using TcpClient tcpClient = new TcpClient();
            tcpClient.Connect(Host, int.Parse(Port));

            using NetworkStream netStream = tcpClient.GetStream();

            // Send some data to the peer.
            JclText = TextValue;
            if (JclText != null && JclText != string.Empty)
            {
                byte[] sendBuffer = Encoding.ASCII.GetBytes(JclText);
                netStream.Write(sendBuffer);

            }

            SetInvalidateId();
        }
    }
}
