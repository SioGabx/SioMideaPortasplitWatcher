using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.ApplicationModel.DataTransfer;

namespace SioMideaPortasplitWatcher
{
    public class BalloonNotifier
    {
        private readonly string _title;
        private readonly string _message;
        private readonly string _url;
        private readonly string _shopName;

        public BalloonNotifier(string title, string message, string url = null, string shopName = null)
        {
            _title = title;
            _message = message;
            _url = url;
            _shopName = shopName;
        }

        public static void Initialize()
        {
            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                var args = ToastArguments.Parse(toastArgs.Argument);

                if (args.Contains("action") && args["action"] == "open" && args.Contains("url") && args.Contains("shopname"))
                {

                    string shopName = args["shopname"];
                    string url = args["url"];

                    // Copie dans le presse-papiers de manière asynchrone (API Windows 10/11)
                    var package = new DataPackage();
                    package.SetText(shopName);
                    

                    Thread t = new Thread(() => {
                        Clipboard.SetContent(package);
                        Clipboard.Flush();
                    });
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            };
        }
        public void Show()
        {
            var builder = new ToastContentBuilder()
                .AddText(_title)
                .AddText(_message);

            if (!string.IsNullOrEmpty(_url))
            {
                builder.AddButton(new ToastButton()
                    .SetContent("Ouvrir")
                    .AddArgument("action", "open")
                    .AddArgument("url", _url))
                    .AddArgument("shopname", _shopName);
            }

            builder.Show(toast =>
            {
                toast.ExpiresOnReboot = true;
                toast.Priority = Windows.UI.Notifications.ToastNotificationPriority.High;
                toast.ExpirationTime = DateTime.Now.AddSeconds(60);
            });
        }
    }
}
