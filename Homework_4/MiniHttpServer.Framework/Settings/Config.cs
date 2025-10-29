using MiniHttpServer.shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer.Settings
{
    public class Config
    {
        private static Config? _instance;

        public SettingsModel Settings { get; private set; }

        private Config(SettingsModel settings)
        {
            Settings = settings;
        }

        public static void Initialize(SettingsModel settings)
        {
            if (_instance == null)
            {
                _instance = new Config(settings);
            }
        }

        public static Config Instance
        {
            get
            {
                if (_instance == null)
                {
                    Initialize(new SettingsModel());
                }

                return _instance!;
            }
        }
    }
}
